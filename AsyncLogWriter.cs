using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace GPSTrackerListeners.AtlantaNew
{
    internal static class AsyncLogWriter
    {
        private struct LogItem
        {
            public string FullPath;
            public string Text;
            public bool Append;
        }

        private static readonly ConcurrentQueue<LogItem> _queue = new ConcurrentQueue<LogItem>();
        private static readonly AutoResetEvent _signal = new AutoResetEvent(false);
        private static Thread _worker;
        private static volatile bool _started;
        private static volatile bool _stop;

        public static void EnsureStarted()
        {
            if (_started) return;
            _started = true;

            _worker = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "AsyncLogWriter"
            };
            _worker.Start();
        }

        public static void Enqueue(string fullPath, string text, bool append)
        {
            // start lazy
            if (!_started) EnsureStarted();

            _queue.Enqueue(new LogItem
            {
                FullPath = fullPath,
                Text = text,
                Append = append
            });

            _signal.Set();
        }

        public static void Stop()
        {
            _stop = true;
            _signal.Set();
        }

        private static void WorkerLoop()
        {
            while (!_stop)
            {
                // Wait until something comes
                _signal.WaitOne(200);

                while (_queue.TryDequeue(out var item))
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(item.FullPath);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        using (var sw = new StreamWriter(item.FullPath, item.Append))
                        {
                            sw.WriteLine(item.Text);
                        }
                    }
                    catch
                    {
                        // never crash worker
                    }
                }
            }

            // Flush remaining items on stop
            while (_queue.TryDequeue(out var item2))
            {
                try
                {
                    var dir2 = Path.GetDirectoryName(item2.FullPath);
                    if (!Directory.Exists(dir2))
                        Directory.CreateDirectory(dir2);

                    using (var sw = new StreamWriter(item2.FullPath, item2.Append))
                    {
                        sw.WriteLine(item2.Text);
                    }
                }
                catch { }
            }
        }
    }
}
