using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GPSTrackerListeners.ORSAC
{
    /// <summary>
    /// Settings for the raw traffic mirror. Defaults are used when the matching
    /// appSettings key is absent from App.config, so no config change is required.
    /// </summary>
    internal static class RawMirrorConfig
    {
        public static bool Enabled { get; private set; }
        public static string Host { get; private set; }
        public static int Port { get; private set; }
        public static int MaxQueuedPackets { get; private set; }
        public static int ConnectTimeoutMs { get; private set; }
        public static int ReconnectDelayMs { get; private set; }
        public static int DrainTimeoutMs { get; private set; }

        static RawMirrorConfig()
        {
            Enabled = ReadBool("RawMirrorEnabled", true);
            Host = ReadString("RawMirrorHost", "192.168.23.134");
            //Host = ReadString("RawMirrorHost", "103.114.154.160");
            Port = ReadInt("RawMirrorPort", 5000);
            MaxQueuedPackets = ReadInt("RawMirrorMaxQueuedPackets", 2000);
            ConnectTimeoutMs = ReadInt("RawMirrorConnectTimeoutMs", 10000);
            ReconnectDelayMs = ReadInt("RawMirrorReconnectDelayMs", 5000);
            DrainTimeoutMs = ReadInt("RawMirrorDrainTimeoutMs", 5000);
        }

        private static string ReadString(string key, string fallback)
        {
            try
            {
                string v = ConfigurationManager.AppSettings[key];
                return string.IsNullOrWhiteSpace(v) ? fallback : v.Trim();
            }
            catch { return fallback; }
        }

        private static int ReadInt(string key, int fallback)
        {
            int v;
            return int.TryParse(ReadString(key, null), out v) && v > 0 ? v : fallback;
        }

        private static bool ReadBool(string key, bool fallback)
        {
            bool v;
            return bool.TryParse(ReadString(key, null), out v) ? v : fallback;
        }
    }

    /// <summary>
    /// Relays a copy of one device session's inbound bytes to the mirror endpoint over
    /// its own TCP connection, so the remote server sees each device as a separate client.
    ///
    /// Everything here is fire and forget: Enqueue only copies bytes into a bounded queue
    /// and returns, so the listener's own processing is never blocked, slowed or failed
    /// by the mirror being slow, down or unreachable.
    /// </summary>
    internal sealed class RawDataMirror
    {
        private readonly string m_Label;
        private readonly ConcurrentQueue<byte[]> m_Queue = new ConcurrentQueue<byte[]>();
        private readonly SemaphoreSlim m_Signal = new SemaphoreSlim(0);
        private readonly CancellationTokenSource m_Cts = new CancellationTokenSource();

        private int m_QueuedCount;
        private int m_DroppedCount;
        private int m_Closing;
        private volatile bool m_LoggedFailure;

        public RawDataMirror(string label)
        {
            m_Label = string.IsNullOrEmpty(label) ? "unknown" : label;
            Task.Run(() => PumpAsync());
        }

        /// <summary>Queue a copy of the given bytes. Never throws, never blocks.</summary>
        public void Enqueue(byte[] buffer, int offset, int length)
        {
            try
            {
                if (buffer == null || length <= 0) return;
                if (Thread.VolatileRead(ref m_Closing) != 0) return;

                if (Thread.VolatileRead(ref m_QueuedCount) >= RawMirrorConfig.MaxQueuedPackets)
                {
                    // Mirror is down or too slow: drop rather than grow without bound.
                    int dropped = Interlocked.Increment(ref m_DroppedCount);
                    if (dropped == 1 || dropped % 1000 == 0)
                        Log("dropped " + dropped + " packet(s), mirror queue full");
                    return;
                }

                byte[] copy = new byte[length];
                Buffer.BlockCopy(buffer, offset, copy, 0, length);

                m_Queue.Enqueue(copy);
                Interlocked.Increment(ref m_QueuedCount);
                m_Signal.Release();
            }
            catch
            {
                // The mirror must never affect the listener.
            }
        }

        /// <summary>
        /// Stop mirroring for this session. Queued bytes get a short window to drain,
        /// then the mirror connection is closed.
        /// </summary>
        public void Close()
        {
            if (Interlocked.Exchange(ref m_Closing, 1) != 0) return;

            try
            {
                m_Signal.Release();                                  // wake the pump so it notices the close
                m_Cts.CancelAfter(RawMirrorConfig.DrainTimeoutMs);   // hard stop if the drain stalls
            }
            catch { }
        }

        private async Task PumpAsync()
        {
            CancellationToken token = m_Cts.Token;
            byte[] pending = null;   // dequeued but not yet confirmed sent

            while (!token.IsCancellationRequested)
            {
                TcpClient client = null;

                try
                {
                    client = new TcpClient { NoDelay = true };
                    await ConnectAsync(client, token).ConfigureAwait(false);

                    m_LoggedFailure = false;
                    Log("connected to " + RawMirrorConfig.Host + ":" + RawMirrorConfig.Port);

                    NetworkStream stream = client.GetStream();

                    while (!token.IsCancellationRequested)
                    {
                        if (pending == null)
                        {
                            if (!m_Queue.TryDequeue(out pending))
                            {
                                // Nothing buffered: if the session is closing, we are done.
                                if (Thread.VolatileRead(ref m_Closing) != 0) return;

                                await m_Signal.WaitAsync(token).ConfigureAwait(false);
                                continue;
                            }

                            Interlocked.Decrement(ref m_QueuedCount);
                        }

                        await stream.WriteAsync(pending, 0, pending.Length, token).ConfigureAwait(false);
                        await stream.FlushAsync(token).ConfigureAwait(false);
                        pending = null;   // sent; only now let it go
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Log the first failure of a run only, so a long outage cannot flood the log.
                    if (!m_LoggedFailure)
                    {
                        m_LoggedFailure = true;
                        Log("error: " + ex.Message);
                    }
                }
                finally
                {
                    try { if (client != null) client.Close(); } catch { }
                }

                if (token.IsCancellationRequested) return;

                // A closing session does not wait around for a dead mirror.
                if (Thread.VolatileRead(ref m_Closing) != 0 && pending == null && Thread.VolatileRead(ref m_QueuedCount) == 0)
                    return;

                try { await Task.Delay(RawMirrorConfig.ReconnectDelayMs, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }
        }

        private static async Task ConnectAsync(TcpClient client, CancellationToken token)
        {
            Task connect = client.ConnectAsync(RawMirrorConfig.Host, RawMirrorConfig.Port);
            Task timeout = Task.Delay(RawMirrorConfig.ConnectTimeoutMs, token);

            if (await Task.WhenAny(connect, timeout).ConfigureAwait(false) != connect)
            {
                token.ThrowIfCancellationRequested();
                throw new TimeoutException("connect to " + RawMirrorConfig.Host + ":" + RawMirrorConfig.Port + " timed out");
            }

            await connect.ConfigureAwait(false);   // surface a connect failure
        }

        private void Log(string message)
        {
            try
            {
                General.WriteToLogFile(m_Label + " : " + message, GlobalVariable.m_folderpath, "RawMirror.txt");
            }
            catch { }
        }
    }
}
