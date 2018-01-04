using SynkServer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SynkServer.Analytics
{
    public enum AnalyticsDataType
    {
        Invalid,
        None,
        String,
        SByte,
        Short,
        Int,
        Long,
        Byte,
        UShort,
        UInt,
        ULong,
        Float,
        Double,
        Decimal,
        Bool,
    }


    public class AnalyticsCollection
    {
        public SortedDictionary<long, object> values = new SortedDictionary<long, object>();

        // counters
        public Dictionary<long, int> dayAggregate = new Dictionary<long, int>();
        public Dictionary<long, int> monthAggregate = new Dictionary<long, int>();
        public Dictionary<long, int> yearAggregate = new Dictionary<long, int>();

        public string name { get; private set; }
        public AnalyticsDataType dataType { get; private set; }

        public AnalyticsCollection(string name, AnalyticsDataType dataType)
        {
            this.name = name;
            this.dataType = dataType;
        }

        public void Add(long timestamp, object obj)
        {
            var val = timestamp.ToDateTime();

            values[timestamp] = obj;

            var dayDate = val.FixedDay();
            Increase(dayAggregate, dayDate);

            var monthDate = val.FixedMonth();
            Increase(monthAggregate, monthDate);

            var yearDate = val.FixedYear();
            Increase(yearAggregate, yearDate);
        }

        private void Increase(Dictionary<long, int> dic, DateTime val)
        {
            var timestamp = val.ToTimestamp();

            if (dic.ContainsKey(timestamp))
            {
                dic[timestamp]++;
            }
            else
            {
                dic[timestamp] = 1;
            }
        }

        public void Load(BinaryReader reader)
        {
            var eventCount = reader.ReadInt64();
            while (eventCount > 0)
            {
                long timestamp = reader.ReadInt64();

                object obj;

                switch (dataType)
                {

                    case AnalyticsDataType.None: obj = null;  break;
                    case AnalyticsDataType.String: obj = reader.ReadString(); break;
                    case AnalyticsDataType.SByte: obj = reader.ReadSByte(); break;
                    case AnalyticsDataType.Short: obj = reader.ReadInt16(); break;
                    case AnalyticsDataType.Int: obj = reader.ReadInt32(); break;
                    case AnalyticsDataType.Long: obj = reader.ReadInt64(); break;
                    case AnalyticsDataType.Byte: obj = reader.ReadByte(); break;
                    case AnalyticsDataType.UShort: obj = reader.ReadUInt16(); break;
                    case AnalyticsDataType.UInt: obj = obj = reader.ReadUInt32(); break;
                    case AnalyticsDataType.ULong: obj = reader.ReadUInt64(); break;
                    case AnalyticsDataType.Float: obj = reader.ReadSingle(); break;
                    case AnalyticsDataType.Double: obj = reader.ReadDouble(); break;
                    case AnalyticsDataType.Decimal: obj = reader.ReadDecimal(); break;
                    case AnalyticsDataType.Bool: obj = reader.ReadBoolean(); break;
                    default:
                        {
                            throw new Exception("Invalid analytics type");
                        }
                }

                Add(timestamp, obj);
                eventCount--;
            }
        }

        public void Save(BinaryWriter writer)
        {
            long eventCount = this.values.Count;
            writer.Write(eventCount);
            foreach (var entry in this.values)
            {
                var timestamp = entry.Key;
                var obj = entry.Value;
                writer.Write(timestamp);

                switch (this.dataType)
                {

                    case AnalyticsDataType.None: break;
                    case AnalyticsDataType.String: writer.Write((string)obj); break;
                    case AnalyticsDataType.SByte: writer.Write((sbyte)obj); break;
                    case AnalyticsDataType.Short: writer.Write((short)obj); break;
                    case AnalyticsDataType.Int: writer.Write((int)obj); break;
                    case AnalyticsDataType.Long: writer.Write((long)obj); break;
                    case AnalyticsDataType.Byte: writer.Write((byte)obj); break;
                    case AnalyticsDataType.UShort: writer.Write((ushort)obj); break;
                    case AnalyticsDataType.UInt: writer.Write((uint)obj); break;
                    case AnalyticsDataType.ULong: writer.Write((ulong)obj); break;
                    case AnalyticsDataType.Float: writer.Write((float)obj); break;
                    case AnalyticsDataType.Double: writer.Write((double)obj); break;
                    case AnalyticsDataType.Decimal: writer.Write((decimal)obj); break;
                    case AnalyticsDataType.Bool: writer.Write((bool)obj); break;
                    default:
                        {
                            throw new Exception("Invalid analytics type");
                        }
                }
            }
        }

    }

}
