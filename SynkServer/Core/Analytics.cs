using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace SynkServer.Core
{
    public class Analytics
    {
        public const string FileName = "analytics.bin";

        private Dictionary<string, List<DateTime>> _events = new Dictionary<string, List<DateTime>>();

        private Site site;

        private bool changed = false;

        private static Thread saveThread = null;

        public Analytics(Site site)
        {
            this.site = site;

            LoadAnalyticsData();
        }

        public void RegisterEvent(Enum val, DateTime date)
        {
            RegisterEvent(val.ToString().ToLowerInvariant(), date);
        }

        public void RegisterEvent(string key, DateTime date)
        {
            lock (this)
            {
                List<DateTime> table = null;

                if (_events.ContainsKey(key))
                {
                    table = _events[key];
                }
                
                if (table == null)
                {
                    table = new List<DateTime>();
                    _events[key] = table;
                }

                table.Add(date);

                changed = true;

                RequestBackgroundThread();
            }

        }

        private void RequestBackgroundThread()
        {
            if (saveThread == null)
            {
                saveThread = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    do
                    {
                        Thread.Sleep(10000);
                        if (changed)
                        {
                            SaveAnalyticsData();
                        }
                    } while (true);
                });

                saveThread.Start();
            }
        }

        private void LoadAnalyticsData()
        {
            if (File.Exists(FileName))
            {
                using (var stream = new FileStream(FileName, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        var keyCount = reader.ReadInt32();
                        for (int i=0; i<keyCount; i++)
                        {
                            var key = reader.ReadString();
                            var eventCount = reader.ReadInt64();

                            while (eventCount>0)
                            {
                                var timestamp = reader.ReadInt64();
                                var date = timestamp.ToDateTime();

                                RegisterEvent(key, date);
                                eventCount--;
                            }
                        }
                    }
                }
            }
        }

        private void SaveAnalyticsData()
        {
            lock (this)
            {
                using (var stream = new FileStream(FileName, FileMode.Create))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        int keyCount = _events.Count;
                        writer.Write(keyCount);

                        foreach (var entry in _events)
                        {
                            writer.Write(entry.Key);

                            var table = entry.Value;

                            long eventCount = table.Count;
                            writer.Write(eventCount);

                            foreach (var item in table)
                            {
                                var timestamp = item.ToTimestamp();
                                writer.Write(timestamp);
                            }
                        }
                    }
                }

                changed = false;
            }

        }
    }
}
