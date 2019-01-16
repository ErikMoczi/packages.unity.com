using UnityEngine;
using UnityEditor;
using System;

namespace Unity.MemoryProfiler.Editor
{
    internal struct Range
    {
        public long first;
        public long last;
        public static Range Empty()
        {
            return new Range(0, 0);
        }

        public static Range FirstLast(long first, long last)
        {
            return new Range(first, last);
        }

        public static Range FirstLength(long first, long length)
        {
            return new Range(first, first + length);
        }

        public long length
        {
            get
            {
                return last - first;
            }
        }
        public long this[long i]
        {
            get
            {
                return first + i;
            }
        }
        private Range(long first, long last)
        {
            this.first = first;
            this.last = last;
        }

        public static Range operator+(Range r, long x)
        {
            return new Range(r.first + x, r.last + x);
        }

        public static Range operator-(Range r, long x)
        {
            return new Range(r.first - x, r.last - x);
        }
    }

    internal struct ArrayRange
    {
        public static ArrayRange FirstLast(long first, long last)
        {
            return new ArrayRange(first, last);
        }

        public static ArrayRange FirstLength(long first, long length)
        {
            return new ArrayRange(first, first + length);
        }

        public static ArrayRange IndexArray(long[] array)
        {
            return new ArrayRange(array);
        }

        public static ArrayRange FirstLastIndexArray(long[] array, long first, long last)
        {
            return new ArrayRange(array, first, last);
        }

        public ArrayRange(long indexFirst, long indexLast)
        {
            this.array = null;
            this.indexFirst = indexFirst;
            this.indexLast = indexLast;
        }

        public ArrayRange(long[] array, long indexFirst, long indexLast)
        {
            this.array = array;
            this.indexFirst = indexFirst;
            this.indexLast = indexLast;
        }

        public ArrayRange(Range range)
        {
            this.array = null;
            this.indexFirst = range.first;
            this.indexLast = range.last;
        }

        public ArrayRange(long[] array, Range range)
        {
            this.array = array;
            this.indexFirst = range.first;
            this.indexLast = range.last;
        }

        public ArrayRange(long[] array)
        {
            this.array = array;
            this.indexFirst = 0;
            this.indexLast = array.Length;
        }

        public long[] ToArray()
        {
            long[] r = new long[indexCount];
            if (array == null)
            {
                for (int i = 0; i < r.Length; ++i)
                {
                    r[i] = indexFirst + i;
                }
            }
            else
            {
                for (int i = 0; i < r.Length; ++i)
                {
                    r[i] = array[indexFirst + i];
                }
            }
            return r;
        }

        public long[] array;//when null, use indexFirst to indexLast index directly
        private long indexFirst;
        private long indexLast;

        public long directIndexFirst
        {
            get
            {
                return indexFirst;
            }
        }
        public long directIndexLast
        {
            get
            {
                return indexLast;
            }
        }
        public long indexCount
        {
            get
            {
                return indexLast - indexFirst;
            }
        }
        public long this[long i]
        {
            get
            {
                if (array != null)
                {
                    return array[indexFirst + i];
                }
                return indexFirst + i;
            }
        }
    }
}
