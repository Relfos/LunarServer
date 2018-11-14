using LunarLabs.WebServer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LunarLabs.WebServer.Analytics
{
    public enum AnalyticsFrequency
    {
        Daily,
        Monthly,
        Yearly
    }

    public class AnalyticsPlugin
    {
        private Dictionary<string, AnalyticsCollection> _collections = new Dictionary<string, AnalyticsCollection>();

        private Site site;

        private bool changed = false;

        private static Thread saveThread = null;

        public string FileName
        {
            get;
            private set;
        }

        public AnalyticsPlugin(Site site, string fileName)
        {
            this.site = site;
            this.FileName = fileName;
            this.LoadAnalyticsData();
            this.RequestBackgroundThread();
        }

        public void RegisterEvent(Enum val, long timestamp)
        {
            this.RegisterEvent(val.ToString().ToLowerInvariant(), timestamp);
        }

        public void RegisterEvent<T>(Enum val, long timestamp, object obj)
        {
            this.RegisterEvent<T>(val.ToString().ToLowerInvariant(), timestamp, obj);
        }

        public void RegisterEvent(string key, long timestamp)
        {
            this.RegisterEvent(key, timestamp, null, typeof(object));
        }

        public void RegisterEvent<T>(string key, long timestamp, object obj)
        {
            Type typeFromHandle = typeof(T);
            this.RegisterEvent(key, timestamp, obj, typeFromHandle);
        }

        private void RegisterEvent(string key, long timestamp, object obj, Type type)
        {
            AnalyticsDataType dataType = this.FromType(type);
            AnalyticsCollection analyticsCollection = this.FindTable(key, dataType, true);
            lock (analyticsCollection)
            {
                analyticsCollection.Add(timestamp, obj);
                this.changed = true;
            }
        }

        private AnalyticsCollection FindTable(string key, AnalyticsDataType dataType, bool canCreate)
        {
            lock (this._collections)
            {
                AnalyticsCollection analyticsCollection = null;
                if (this._collections.ContainsKey(key))
                {
                    analyticsCollection = this._collections[key];
                }
                if (analyticsCollection == null & canCreate)
                {
                    analyticsCollection = new AnalyticsCollection(key, dataType);
                    this._collections[key] = analyticsCollection;
                }
                return analyticsCollection;
            }
        }

        private void RequestBackgroundThread()
        {
            this.site.Logger.Info("Running analytics thread");
            AnalyticsPlugin.saveThread = new Thread((ThreadStart)delegate
            {
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    Thread.Sleep(10000);
                    if (this.changed)
                    {
                        this.SaveAnalyticsData();
                    }
                }
            });
            AnalyticsPlugin.saveThread.Start();
        }

        private Type FromDataType(AnalyticsDataType dataType)
        {
            switch (dataType)
            {
                case AnalyticsDataType.None:
                    return typeof(object);
                case AnalyticsDataType.String:
                    return typeof(string);
                case AnalyticsDataType.SByte:
                    return typeof(sbyte);
                case AnalyticsDataType.Short:
                    return typeof(short);
                case AnalyticsDataType.Int:
                    return typeof(int);
                case AnalyticsDataType.Long:
                    return typeof(long);
                case AnalyticsDataType.Byte:
                    return typeof(byte);
                case AnalyticsDataType.UShort:
                    return typeof(ushort);
                case AnalyticsDataType.UInt:
                    return typeof(uint);
                case AnalyticsDataType.ULong:
                    return typeof(ulong);
                case AnalyticsDataType.Float:
                    return typeof(float);
                case AnalyticsDataType.Double:
                    return typeof(double);
                case AnalyticsDataType.Decimal:
                    return typeof(decimal);
                case AnalyticsDataType.Bool:
                    return typeof(bool);
                default:
                    return null;
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
            if (File.Exists(this.FileName))
            {
                using (FileStream input = new FileStream(this.FileName, FileMode.Open))
                {
                    using (BinaryReader binaryReader = new BinaryReader(input))
                    {
                        try
                        {
                            int num = binaryReader.ReadInt32();
                            for (int i = 0; i < num; i++)
                            {
                                string key = binaryReader.ReadString();
                                AnalyticsDataType dataType = (AnalyticsDataType)binaryReader.ReadByte();
                                Type type = this.FromDataType(dataType);
                                AnalyticsCollection analyticsCollection = this.FindTable(key, dataType, true);
                                analyticsCollection.Load(binaryReader);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private void SaveAnalyticsData()
        {
            lock (this)
            {
                using (FileStream output = new FileStream(this.FileName, FileMode.Create))
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter(output))
                    {
                        int count = this._collections.Count;
                        binaryWriter.Write(count);
                        foreach (KeyValuePair<string, AnalyticsCollection> collection in this._collections)
                        {
                            binaryWriter.Write(collection.Key);
                            AnalyticsCollection value = collection.Value;
                            binaryWriter.Write((byte)value.dataType);
                            value.Save(binaryWriter);
                        }
                    }
                }
                this.changed = false;
            }
        }

        public int GetTotalAmmount(Enum val)
        {
            return this.GetTotalAmmount(val.ToString().ToLowerInvariant());
        }

        public int GetTotalAmmount(string key)
        {
            if (this._collections.ContainsKey(key))
            {
                return this._collections[key].values.Count;
            }
            return 0;
        }

        public void IterateEntries<T>(Enum key, Action<DateTime, T> visitor)
        {
            this.IterateEntries(key.ToString().ToLowerInvariant(), visitor);
        }

        public void IterateEntries<T>(string key, Action<DateTime, T> visitor)
        {
            Type typeFromHandle = typeof(T);
            AnalyticsDataType dataType = this.FromType(typeFromHandle);
            AnalyticsCollection analyticsCollection = this.FindTable(key, dataType, false);
            if (analyticsCollection != null)
            {
                lock (analyticsCollection)
                {
                    foreach (KeyValuePair<long, object> value in analyticsCollection.values)
                    {
                        visitor(value.Key.ToDateTime(), (T)value.Value);
                    }
                }
            }
        }

        public int GetAmmount(Enum key, AnalyticsFrequency frequency, DateTime date)
        {
            return this.GetAmmount(key.ToString().ToLowerInvariant(), frequency, date);
        }

        public int GetAmmount(string key, AnalyticsFrequency frequency, DateTime date)
        {
            if (this._collections.ContainsKey(key))
            {
                AnalyticsCollection analyticsCollection = this._collections[key];
                switch (frequency)
                {
                    case AnalyticsFrequency.Daily:
                        {
                            date = date.FixedDay();
                            long key3 = date.ToTimestamp();
                            if (analyticsCollection.dayAggregate.ContainsKey(key3))
                            {
                                return analyticsCollection.dayAggregate[key3];
                            }
                            return 0;
                        }
                    case AnalyticsFrequency.Monthly:
                        {
                            date = date.FixedMonth();
                            long key4 = date.ToTimestamp();
                            if (analyticsCollection.monthAggregate.ContainsKey(key4))
                            {
                                return analyticsCollection.monthAggregate[key4];
                            }
                            return 0;
                        }
                    case AnalyticsFrequency.Yearly:
                        {
                            date = date.FixedYear();
                            long key2 = date.ToTimestamp();
                            if (analyticsCollection.yearAggregate.ContainsKey(key2))
                            {
                                return analyticsCollection.yearAggregate[key2];
                            }
                            return 0;
                        }
                }
            }
            return 0;
        }
    }
}
