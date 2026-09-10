using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private static readonly object _startLock = new object();

        public static void EnsureStarted()
        {
            if (_started) return;
            lock (_startLock)
            {
                if (_started) return;

                _worker = new Thread(WorkerLoop)
                {
                    IsBackground = true,
                    Name = "AsyncLogWriter"
                };
                _worker.Start();
                _started = true;
            }
        }

        public static void Enqueue(string fullPath, string text, bool append)
        {
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
                _signal.WaitOne(200);
                ProcessQueue();
            }

            // Flush remaining items on stop
            ProcessQueue();
        }

        private static void ProcessQueue()
        {
            if (_queue.IsEmpty) return;

            // Group queued items by FullPath to minimize file handle creation
            var batch = new Dictionary<string, List<LogItem>>(StringComparer.OrdinalIgnoreCase);

            while (_queue.TryDequeue(out var item))
            {
                if (!batch.TryGetValue(item.FullPath, out var list))
                {
                    list = new List<LogItem>();
                    batch[item.FullPath] = list;
                }
                list.Add(item);
            }

            // Write all lines grouped by file in a single file-open session
            foreach (var kvp in batch)
            {
                var filePath = kvp.Key;
                var items = kvp.Value;

                try
                {
                    var dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    using (var sw = new StreamWriter(filePath, items[0].Append))
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            sw.WriteLine(items[i].Text);
                        }
                    }
                }
                catch
                {
                    // Fail gracefully without interrupting worker loop
                }
            }
        }
    }
}












//using System;
//using System.Collections.Concurrent;
//using System.IO;
//using System.Threading;

//namespace GPSTrackerListeners.AtlantaNew
//{
//    internal static class AsyncLogWriter
//    {
//        private struct LogItem
//        {
//            public string FullPath;
//            public string Text;
//            public bool Append;
//        }

//        private static readonly ConcurrentQueue<LogItem> _queue = new ConcurrentQueue<LogItem>();
//        private static readonly AutoResetEvent _signal = new AutoResetEvent(false);
//        private static Thread _worker;
//        private static volatile bool _started;
//        private static volatile bool _stop;

//        public static void EnsureStarted()
//        {
//            if (_started) return;
//            _started = true;

//            _worker = new Thread(WorkerLoop)
//            {
//                IsBackground = true,
//                Name = "AsyncLogWriter"
//            };
//            _worker.Start();
//        }

//        public static void Enqueue(string fullPath, string text, bool append)
//        {
//            // start lazy
//            if (!_started) EnsureStarted();

//            _queue.Enqueue(new LogItem
//            {
//                FullPath = fullPath,
//                Text = text,
//                Append = append
//            });

//            _signal.Set();
//        }

//        public static void Stop()
//        {
//            _stop = true;
//            _signal.Set();
//        }

//        private static void WorkerLoop()
//        {
//            while (!_stop)
//            {
//                // Wait until something comes
//                _signal.WaitOne(200);

//                while (_queue.TryDequeue(out var item))
//                {
//                    try
//                    {
//                        var dir = Path.GetDirectoryName(item.FullPath);
//                        if (!Directory.Exists(dir))
//                            Directory.CreateDirectory(dir);

//                        using (var sw = new StreamWriter(item.FullPath, item.Append))
//                        {
//                            sw.WriteLine(item.Text);
//                        }
//                    }
//                    catch
//                    {
//                        // never crash worker
//                    }
//                }
//            }

//            // Flush remaining items on stop
//            while (_queue.TryDequeue(out var item2))
//            {
//                try
//                {
//                    var dir2 = Path.GetDirectoryName(item2.FullPath);
//                    if (!Directory.Exists(dir2))
//                        Directory.CreateDirectory(dir2);

//                    using (var sw = new StreamWriter(item2.FullPath, item2.Append))
//                    {
//                        sw.WriteLine(item2.Text);
//                    }
//                }
//                catch { }
//            }
//        }
//    }
//}
