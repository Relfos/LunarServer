using SynkServer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SynkServer.Analytics
{
    public enum AnalyticsFrequency
    {
        Daily,
        Monthly,
        Yearly
    }

    public class AnalyticsPlugin
    {
        public string FileName { get; private set; }

        private Dictionary<string, AnalyticsCollection> _collections = new Dictionary<string, AnalyticsCollection>();

        private Site site;

        private bool changed = false;

        private static Thread saveThread = null;

        public AnalyticsPlugin(Site site, string fileName)
        {
            this.site = site;
            this.FileName = fileName;

            LoadAnalyticsData();

            RequestBackgroundThread();
        }

        public void RegisterEvent(Enum val, long timestamp)
        {
            RegisterEvent(val.ToString().ToLowerInvariant(), timestamp);
        }

        public void RegisterEvent<T>(Enum val, long timestamp, object obj)
        {
            RegisterEvent<T>(val.ToString().ToLowerInvariant(), timestamp, obj);
        }

        public void RegisterEvent(string key, long timestamp)
        {
            RegisterEvent(key, timestamp, null, typeof(object));
        }

        public void RegisterEvent<T>(string key, long timestamp, object obj)
        {
            var type = typeof(T);
            RegisterEvent(key, timestamp, obj, type);
        }

        private void RegisterEvent(string key, long timestamp, object obj, Type type)
        {
            var dataType = FromType(type);
            var table = FindTable(key, dataType);

            lock (table)
            {
                table.Add(timestamp,  obj);

                changed = true;
            }

        }

        private AnalyticsCollection FindTable(string key, AnalyticsDataType dataType)
        {
            lock (_collections)
            {
                AnalyticsCollection table = null;

                if (_collections.ContainsKey(key))
                {
                    table = _collections[key];
                }

                if (table == null)
                {
                    table = new AnalyticsCollection(key, dataType);
                    _collections[key] = table;
                }

                return table;
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

        private Type FromDataType(AnalyticsDataType dataType)
        {
            switch (dataType)
            {
                case AnalyticsDataType.None: return typeof(object);
                case AnalyticsDataType.String: return typeof(string);
                case AnalyticsDataType.SByte: return typeof(sbyte);
                case AnalyticsDataType.Short: return typeof(short);
                case AnalyticsDataType.Int: return typeof(int);
                case AnalyticsDataType.Long: return typeof(long);
                case AnalyticsDataType.Byte: return typeof(byte);
                case AnalyticsDataType.UShort: return typeof(ushort);
                case AnalyticsDataType.UInt: return typeof(uint);
                case AnalyticsDataType.ULong: return typeof(ulong);
                case AnalyticsDataType.Float: return typeof(float);
                case AnalyticsDataType.Double: return typeof(double);
                case AnalyticsDataType.Decimal: return typeof(decimal);
                case AnalyticsDataType.Bool: return typeof(bool);
                default: return null;
            }
        }

        private AnalyticsDataType FromType(Type type)
        {
            if (type == typeof(string))
            {
                return AnalyticsDataType.String;
            }

            if (type == typeof(sbyte))
            {
                return AnalyticsDataType.SByte;
            }
            
            if (type == typeof(short))
            {
                return AnalyticsDataType.Short;
            }
            
            if (type == typeof(int))
            {
                return AnalyticsDataType.Int;
            }
            
            if (type == typeof(long))
            {
                return AnalyticsDataType.Long;
            }
            
            if (type == typeof(byte))
            {
                return AnalyticsDataType.Byte;
            }
            
            if (type == typeof(ushort))
            {
                return AnalyticsDataType.UShort;
            }
            
            if (type == typeof(uint))
            {
                return AnalyticsDataType.UInt;
            }
            
            if (type == typeof(ulong))
            {
                return AnalyticsDataType.ULong;
            }

            if (type == typeof(float))
            {
                return AnalyticsDataType.Float;
            }
            
            if (type == typeof(double))
            {
                return AnalyticsDataType.Double;
            }
            
            if (type == typeof(decimal))
            {
                return AnalyticsDataType.Decimal;
            }
            
            if (type == typeof(bool))
            {
                return AnalyticsDataType.Bool;
            }

            if (type == typeof(object))
            {
                return AnalyticsDataType.None;
            }

            return AnalyticsDataType.Invalid;
        }

        private void LoadAnalyticsData()
        {
            if (File.Exists(FileName))
            {
                using (var stream = new FileStream(FileName, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        try
                        {
                            var keyCount = reader.ReadInt32();
                            for (int i = 0; i < keyCount; i++)
                            {
                                var key = reader.ReadString();
                                var dataType = (AnalyticsDataType)reader.ReadByte();

                                var type = FromDataType(dataType);
                                var table = FindTable(key, dataType);
                                table.Load(reader);
                            }
                        }
                        catch
                        {
                            return;
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
                        int keyCount = _collections.Count;
                        writer.Write(keyCount);

                        foreach (var colEntry in _collections)
                        {
                            writer.Write(colEntry.Key);
                            var table = colEntry.Value;
                            writer.Write((byte)table.dataType);

                            table.Save(writer);
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
            if (_collections.ContainsKey(key))
            {
                return _collections[key].values.Count;
            }

            return 0;
        }

        public int GetAmmount(Enum val, AnalyticsFrequency frequency, DateTime date)
        {
            return GetAmmount(val.ToString().ToLowerInvariant(), frequency, date);
        }

        public int GetAmmount(string key, AnalyticsFrequency frequency, DateTime date)
        {
            if (_collections.ContainsKey(key))
            {
                var evt = _collections[key];

                switch (frequency)
                {
                    case AnalyticsFrequency.Daily:
                        {
                            date = date.FixedDay();
                            var timestamp = date.ToTimestamp();

                            if (evt.dayAggregate.ContainsKey(timestamp))
                            {
                                return evt.dayAggregate[timestamp];
                            }

                            return 0;
                        }

                    case AnalyticsFrequency.Monthly:
                        {
                            date = date.FixedMonth();
                            var timestamp = date.ToTimestamp();

                            if (evt.monthAggregate.ContainsKey(timestamp))
                            {
                                return evt.monthAggregate[timestamp];
                            }

                            return 0;
                        }

                    case AnalyticsFrequency.Yearly:
                        {
                            date = date.FixedYear();
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
