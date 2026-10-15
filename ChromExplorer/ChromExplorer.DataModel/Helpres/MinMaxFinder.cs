using System;
using System.Collections.Generic;

namespace ChromExplorer.DataModel.Helpres
{
    public class MinMax<T>
    {
        public T Min;
        public T Max;
        public int MinIndex;
        public int MaxIndex;

        public MinMax(T min, T max)
        {
            Min = min;
            Max = max;
            MinIndex = default(int);
            MaxIndex = default(int);
        }

        public MinMax(T min, T max, int minIndex, int maxIndex)
        {
            Min = min;
            Max = max;
            MinIndex = minIndex;
            MaxIndex = maxIndex;
        }


        public MinMax()
        {
            Min = default(T);
            Max = default(T);
            MinIndex = default(int);
            MaxIndex = default(int);

        }
    }

    public class MinMaxFinder<T> where T : IComparable
    {
        public static MinMax<T> MinMax(T[] values)
        {
            MinMax<T> retval = null;
            if (values == null || values.Length == 0)
                return retval;

            retval = new MinMax<T>();
            retval.Min = values[0];
            retval.Max = values[0];

            for (int i = 0; i < values.Length; i++)
            {
                if (retval.Min.CompareTo(values[i]) > 0)
                {
                    retval.Min = values[i];
                    retval.MinIndex = i;
                }

                if (retval.Max.CompareTo(values[i]) < 0)
                {
                    retval.Max = values[i];
                    retval.MaxIndex = i;
                }
            }

            return retval;
        }

        public static MinMax<T> MinMax(T[] values, int startIndex, int lastIndex)
        {
            MinMax<T> retval = null;
            if (values == null || values.Length == 0)
                return retval;

            if (startIndex >= values.Length || lastIndex >= values.Length)
                return retval;

            retval = new MinMax<T>();
            retval.Min = values[startIndex];
            retval.Max = values[startIndex];

            for (int i = startIndex; i <= lastIndex; i++)
            {
                if (retval.Min.CompareTo(values[i]) > 0)
                {
                    retval.Min = values[i];
                    retval.MinIndex = i;
                }

                if (retval.Max.CompareTo(values[i]) < 0)
                {
                    retval.Max = values[i];
                    retval.MaxIndex = i;
                }
            }

            return retval;
        }

        public static MinMax<T> MinMax(T[] values, IComparer<T> comparator)
        {
            MinMax<T> retval = new MinMax<T>();
            if (values == null || values.Length == 0)
                return retval;

            retval.Min = values[0];
            retval.Max = values[0];

            for (int i = 0; i < values.Length; i++)
            {
                if (comparator.Compare(retval.Min, (values[i])) > 0)
                {
                    retval.Min = values[i];
                    retval.MinIndex = i;
                }

                if (comparator.Compare(retval.Max, (values[i])) < 0)
                {
                    retval.Max = values[i];
                    retval.MaxIndex = i;
                }
            }

            return retval;
        }
    }
}
