using LunarLabs.WebServer.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace LunarLabs.WebServer.Analytics
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
        Bool
    }

    public class AnalyticsCollection
    {
        public SortedDictionary<long, object> values = new SortedDictionary<long, object>();

        public Dictionary<long, int> dayAggregate = new Dictionary<long, int>();

        public Dictionary<long, int> monthAggregate = new Dictionary<long, int>();

        public Dictionary<long, int> yearAggregate = new Dictionary<long, int>();

        public string name
        {
            get;
            private set;
        }

        public AnalyticsDataType dataType
        {
            get;
            private set;
        }

        public AnalyticsCollection(string name, AnalyticsDataType dataType)
        {
            this.name = name;
            this.dataType = dataType;
        }

        public void Add(long timestamp, object obj)
        {
            DateTime val = timestamp.ToDateTime();
            this.values[timestamp] = obj;
            DateTime val2 = val.FixedDay();
            this.Increase(this.dayAggregate, val2);
            DateTime val3 = val.FixedMonth();
            this.Increase(this.monthAggregate, val3);
            DateTime val4 = val.FixedYear();
            this.Increase(this.yearAggregate, val4);
        }

        private void Increase(Dictionary<long, int> dic, DateTime val)
        {
            long num = val.ToTimestamp();
            if (dic.ContainsKey(num))
            {
                long key = num;
                dic[key]++;
            }
            else
            {
                dic[num] = 1;
            }
        }

        public void Load(BinaryReader reader)
        {
            for (long num = reader.ReadInt64(); num > 0; num--)
            {
                long timestamp = reader.ReadInt64();
                object obj;
                switch (this.dataType)
                {
                    case AnalyticsDataType.None:
                        obj = null;
                        break;
                    case AnalyticsDataType.String:
                        obj = reader.ReadString();
                        break;
                    case AnalyticsDataType.SByte:
                        obj = reader.ReadSByte();
                        break;
                    case AnalyticsDataType.Short:
                        obj = reader.ReadInt16();
                        break;
                    case AnalyticsDataType.Int:
                        obj = reader.ReadInt32();
                        break;
                    case AnalyticsDataType.Long:
                        obj = reader.ReadInt64();
                        break;
                    case AnalyticsDataType.Byte:
                        obj = reader.ReadByte();
                        break;
                    case AnalyticsDataType.UShort:
                        obj = reader.ReadUInt16();
                        break;
                    case AnalyticsDataType.UInt:
                        obj = (obj = reader.ReadUInt32());
                        break;
                    case AnalyticsDataType.ULong:
                        obj = reader.ReadUInt64();
                        break;
                    case AnalyticsDataType.Float:
                        obj = reader.ReadSingle();
                        break;
                    case AnalyticsDataType.Double:
                        obj = reader.ReadDouble();
                        break;
                    case AnalyticsDataType.Decimal:
                        obj = reader.ReadDecimal();
                        break;
                    case AnalyticsDataType.Bool:
                        obj = reader.ReadBoolean();
                        break;
                    default:
                        throw new Exception("Invalid analytics type");
                }
                this.Add(timestamp, obj);
            }
        }

        public void Save(BinaryWriter writer)
        {
            long value = this.values.Count;
            writer.Write(value);
            foreach (KeyValuePair<long, object> value3 in this.values)
            {
                long key = value3.Key;
                object value2 = value3.Value;
                writer.Write(key);
                switch (this.dataType)
                {
                    case AnalyticsDataType.String:
                        writer.Write((string)value2);
                        break;
                    case AnalyticsDataType.SByte:
                        writer.Write((sbyte)value2);
                        break;
                    case AnalyticsDataType.Short:
                        writer.Write((short)value2);
                        break;
                    case AnalyticsDataType.Int:
                        writer.Write((int)value2);
                        break;
                    case AnalyticsDataType.Long:
                        writer.Write((long)value2);
                        break;
                    case AnalyticsDataType.Byte:
                        writer.Write((byte)value2);
                        break;
                    case AnalyticsDataType.UShort:
                        writer.Write((ushort)value2);
                        break;
                    case AnalyticsDataType.UInt:
                        writer.Write((uint)value2);
                        break;
                    case AnalyticsDataType.ULong:
                        writer.Write((ulong)value2);
                        break;
                    case AnalyticsDataType.Float:
                        writer.Write((float)value2);
                        break;
                    case AnalyticsDataType.Double:
                        writer.Write((double)value2);
                        break;
                    case AnalyticsDataType.Decimal:
                        writer.Write((decimal)value2);
                        break;
                    case AnalyticsDataType.Bool:
                        writer.Write((bool)value2);
                        break;
                    default:
                        throw new Exception("Invalid analytics type");
                    case AnalyticsDataType.None:
                        break;
                }
            }
        }
    }

}
