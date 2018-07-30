namespace LunarLabs.WebServer.Utils
{
    public struct ArrayPointer<T>
    {
        T[] array;
        long address;

        public ArrayPointer(T[] array)
        {
            this.array = array;
            this.address = 0;
        }

        public int Address
        {
            get
            {
                return (int)address;
            }

            set
            {
                address = value;
            }
        }

        public long LongAddress
        {
            get
            {
                return address;
            }

            set
            {
                address = value;
            }
        }

        public T Value
        {
            get
            {
                return this.array[this.address];
            }

            set
            {
                this.array[this.address] = value;
            }
        }

        public int ArrayLength
        {
            get
            {
                return array.Length;
            }
        }

        public long ArrayLongLength
        {
            get
            {
                return array.LongLength;
            }
        }

        public T[] SourceArray
        {
            get
            {
                return this.array;
            }
        }

        public T this[int index]
        {
            get
            {
                return this.array[this.address + index];
            }

            set
            {
                this.array[this.address + index] = value;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is ArrayPointer<T> && this == (ArrayPointer<T>)obj;
        }

        public override int GetHashCode()
        {
            return (int)address;
        }

        public static ArrayPointer<T> operator +(ArrayPointer<T> ap, int offset)
        {
            ArrayPointer<T> temp = new ArrayPointer<T>(ap.array);
            temp.address = ap.address + offset;
            return temp;
        }

        public static ArrayPointer<T> operator +(ArrayPointer<T> ap, long offset)
        {
            ArrayPointer<T> temp = new ArrayPointer<T>(ap.array);
            temp.address = ap.address + offset;
            return temp;
        }

        public static ArrayPointer<T> operator +(int offset, ArrayPointer<T> ap)
        {
            ArrayPointer<T> temp = new ArrayPointer<T>(ap.array);
            temp.address = ap.address + offset;
            return temp;
        }

        public static ArrayPointer<T> operator +(long offset, ArrayPointer<T> ap)
        {
            ArrayPointer<T> temp = new ArrayPointer<T>(ap.array);
            temp.address = ap.address + offset;
            return temp;
        }

        public static ArrayPointer<T> operator ++(ArrayPointer<T> ap)
        {
            ArrayPointer<T> temp = new ArrayPointer<T>(ap.array);
            temp.address = ap.address + 1;
            return temp;
        }

        public static ArrayPointer<T> operator --(ArrayPointer<T> ap)
        {
            ArrayPointer<T> temp = new ArrayPointer<T>(ap.array);
            temp.address = ap.address - 1;
            return temp;
        }

        public static bool operator ==(ArrayPointer<T> ap1, ArrayPointer<T> ap2)
        {
            return ap1.array == ap2.array && ap1.address == ap2.address;
        }

        public static bool operator !=(ArrayPointer<T> ap1, ArrayPointer<T> ap2)
        {
            return ap1.array != ap2.array || ap1.address != ap2.address;
        }
    }
}


