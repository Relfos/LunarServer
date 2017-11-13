using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace SynkServer.Core
{
    public enum AnalyticsFrequency
    {
        Daily,
        Monthly,
        Yearly
    }

    public class AnalyticsEntry
    {
        public List<long> values = new List<long>();

        public Dictionary<long, int> dayAggregate = new Dictionary<long, int>();
        public Dictionary<long, int> monthAggregate = new Dictionary<long, int>();
        public Dictionary<long, int> yearAggregate = new Dictionary<long, int>();

        public static DateTime FixedDay(DateTime val)
        {
            return new DateTime(val.Year, val.Month, val.Day);
        }

        public static DateTime FixedMonth(DateTime val)
        {
            return new DateTime(val.Year, val.Month, 1);
        }

        public static DateTime FixedYear(DateTime val)
        {
            return new DateTime(val.Year, 1, 1);
        }

        public void Add(long timestamp)
        {
            var val = timestamp.ToDateTime();

            values.Add(timestamp);

            var dayDate = FixedDay(val);
            Increase(dayAggregate, dayDate);

            var monthDate = FixedMonth(val);
            Increase(monthAggregate, monthDate);

            var yearDate = FixedYear(val);
            Increase(yearAggregate, yearDate);
        }

        private void Increase(Dictionary<long, int> dic, DateTime val)
        {
            var timestamp = val.ToTimestamp();

            if (dic.ContainsKey(timestamp))  {
                dic[timestamp]++;
            }
            else {
                dic[timestamp] = 1;
            }
        }
    }

    public class Analytics
    {
        public const string FileName = "analytics.bin";

        private Dictionary<string, AnalyticsEntry> _events = new Dictionary<string, AnalyticsEntry>();

        private Site site;

        private bool changed = false;

        private static Thread saveThread = null;

        public Analytics(Site site)
        {
            this.site = site;

            LoadAnalyticsData();

            RequestBackgroundThread();
        }

        public void RegisterEvent(Enum val, DateTime date)
        {
            RegisterEvent(val.ToString().ToLowerInvariant(), date);
        }

        public void RegisterEvent(Enum val, long timestamp)
        {
            RegisterEvent(val.ToString().ToLowerInvariant(), timestamp);
        }

        public void RegisterEvent(string key, DateTime date)
        {
            RegisterEvent(key, date.ToTimestamp());
        }

        public void RegisterEvent(string key, long timestamp)
        {
            lock (this)
            {
                AnalyticsEntry table = null;

                if (_events.ContainsKey(key))
                {
                    table = _events[key];
                }
                
                if (table == null)
                {
                    table = new AnalyticsEntry();
                    _events[key] = table;
                }

                table.Add(timestamp);

                changed = true;
            }

        }

        private void RequestBackgroundThread()
        {
            this.site.log.Info("Running analytics thread");

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
                                long timestamp = reader.ReadInt64();
                                RegisterEvent(key, timestamp);
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

                            long eventCount = table.values.Count;
                            writer.Write(eventCount);

                            foreach (long timestamp in table.values)
                            {
                                writer.Write(timestamp);
                            }
                        }
                    }
                }

                changed = false;
            }

        }

        public int GetTotalAmmount(Enum val)
        {
            return GetTotalAmmount(val.ToString().ToLowerInvariant());
        }

        public int GetTotalAmmount(string key)
        {
            if (_events.ContainsKey(key))
            {
                return _events[key].values.Count;
            }

            return 0;
        }

        public int GetAmmount(Enum val, AnalyticsFrequency frequency, DateTime date)
        {
            return GetAmmount(val.ToString().ToLowerInvariant(), frequency, date);
        }

        public int GetAmmount(string key, AnalyticsFrequency frequency, DateTime date)
        {
            if (_events.ContainsKey(key))
            {
                var evt = _events[key];

                switch (frequency)
                {
                    case AnalyticsFrequency.Daily:
                        {
                            date = AnalyticsEntry.FixedDay(date);
                            var timestamp = date.ToTimestamp();

                            if (evt.dayAggregate.ContainsKey(timestamp))
                            {
                                return evt.dayAggregate[timestamp];
                            }

                            return 0;
                        }

                    case AnalyticsFrequency.Monthly:
                        {
                            date = AnalyticsEntry.FixedMonth(date);
                            var timestamp = date.ToTimestamp();

                            if (evt.monthAggregate.ContainsKey(timestamp))
                            {
                                return evt.monthAggregate[timestamp];
                            }

                            return 0;
                        }

                    case AnalyticsFrequency.Yearly:
                        {
                            date = AnalyticsEntry.FixedYear(date);
                            var timestamp = date.ToTimestamp();

                            if (evt.yearAggregate.ContainsKey(timestamp))
                            {
                                return evt.yearAggregate[timestamp];
                            }

                            return 0;
                        }
                }
            }

            return 0;
        }

    }
}
