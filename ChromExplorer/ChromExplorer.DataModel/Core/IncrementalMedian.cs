using System.Collections.Generic;
using System.Linq;

namespace ChromExplorer.DataModel.Core
{
    public static class EnumerableExtensions
    {
        public static double Median(this IEnumerable<double> list)
        {
            var items = new List<double>(list);

            if (items.Count == 0)
                return 0.0;

            items.Sort();

            
            int index = items.Count / 2;

            if (items.Count % 2 == 1)
            {
                return items[index];
            }
            else
            {
                return  (items[index - 1] + items[index]) / 2.0;
            }
        }

       
    }

    public class IncrementalMedian
    {
        //TODO does not work. We need sorted multiset here
        private readonly SortedSet<double> _items; 
        public IncrementalMedian()
        {
            _items = new SortedSet<double>();
            Count = 0;
            Median = 0;
        }

        public void Reset()
        {
            _items.Clear();
            Count = 0;
        }

        public void Add(double v)
        {
            _items.Add(v);
            Count++;
            int index = Count/2;
            if (Count%2 == 1)
            {
                Median = _items.ElementAt(index);
            } else
            {
                Median = (_items.ElementAt(index - 1) + _items.ElementAt(index)) / 2.0;
            }
            
        }

        public int Count { get ; private set; }
        public double Median { get; private set; }

    }
}
