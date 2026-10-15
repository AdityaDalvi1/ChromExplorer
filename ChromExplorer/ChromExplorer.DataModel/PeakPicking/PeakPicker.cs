using System.Collections.Generic;
using System.Diagnostics;

namespace ChromExplorer.DataModel.PeakPicking
{
    public class PeakPicker
    {
        private List<SpectrumDataPoint> sortedByIntensity;
        private List<SpectrumDataPoint> sortedByIndex;
        private int start_index;
        private int end_index;
        private double min;
        private double max;

        private PeakPickingSettings settings;
        private IComparer<SpectrumDataPoint> indexComparator = new IndexComparer();

        public PeakPicker()
        {
            this.settings = new PeakPickingSettings();
            sortedByIntensity = new List<SpectrumDataPoint>();
            sortedByIndex = new List<SpectrumDataPoint>();
        }

        public PeakPicker(PeakPickingSettings settings)
        {
            this.settings = settings;
            sortedByIntensity = new List<SpectrumDataPoint>();
            sortedByIndex = new List<SpectrumDataPoint>();
        }

        public PeakPickingSettings Settings 
        { 
            get { return settings; }
            set { settings = value; }
        }
  
        public List<SpectrumDataPoint> SortedByIntensity { get { return sortedByIntensity; } }
        public List<SpectrumDataPoint> SortedByIndex { get { return sortedByIndex; } }

        public List<PeakInfo> FindPeaks<T>(double[] xs, double[] ys, double threshold) where T : IPeakAnalyzer, new()
        {

            return FindPeaks<T>(xs, ys, double.NegativeInfinity, double.PositiveInfinity, threshold);
        }

        public List<PeakInfo> FindPeaks<T>(double[] xs, double[] ys, double threshold, bool percentage) where T : IPeakAnalyzer, new()
        {

            return FindPeaks<T>(xs, ys, double.NegativeInfinity, double.PositiveInfinity, threshold, percentage);
        }

        public List<PeakInfo> FindPeaks<T>(double[] xs, double[] ys, double min, double max, double threshold) where T : IPeakAnalyzer, new()
        {
            return FindPeaks<T>(xs, ys, min, max, threshold, false);
        }
        
        public List<PeakInfo> FindPeaks<T>(double[] xs, double[] ys, double min, double max, double threshold, bool percentage) where T : IPeakAnalyzer, new()
        {
            //TODO Assertions here, mb code contracts... need to read about
            var analyzer = new T();
            Initialize(xs, ys, min, max);

            int current = 0;
            int size = sortedByIndex.Count;

            List<PeakInfo> result = new List<PeakInfo>();

            double threshold_internal = threshold;
            if (percentage)
            {
                threshold_internal = this.min + (this.max - this.min) * threshold / 100.0;
            }
            

            while (current < size)
            {
                if (sortedByIntensity[current].Y < threshold_internal)
                {
                    current = size;
                    break;
                }

                if (sortedByIntensity[current].InPeak)
                {
                    current++;
                }
                else
                {
                    if (CheckPeak(sortedByIntensity[current], size))
                    {
                        var pi = AnalyzePeak(analyzer, sortedByIntensity[current], size);

                        if (pi != null)
                            result.Add(pi);
                    }

                    current++;
                }
            }
            
            return result;
        }

        public List<PeakInfo> FindPeaksLite<T>(double[] xs, double[] ys, double min, double max, double threshold, bool percentage) where T : IPeakAnalyzer, new()
        {
            //TODO Assertions here, mb code contracts... need to read about
            var analyzer = new T();
            InitializeLite(xs, ys, min, max);

            int current = 0;
            int size = sortedByIndex.Count;

            List<PeakInfo> result = new List<PeakInfo>();

            double threshold_internal = threshold;
            if (percentage)
            {
                threshold_internal = this.min + (this.max - this.min) * threshold / 100.0;
            }


            int steps = 10;
            double step = (this.max - threshold_internal) / (steps - 1);

            if (this.max < threshold_internal)
                return result;

            for (int i = 0; i < steps; i++)
            {
                double t = this.max - step * i;
                
                current = 0;

                while (current < size)
                {
                    if (sortedByIndex[current].Y < t)
                    {
                        current++;
                    }
                    else
                    {
                        if (sortedByIndex[current].InPeak)
                        {
                            current++;
                        }
                        else
                        {
                            if (CheckPeakLite(current, size))
                            {
                                var pi = AnalyzePeakLite(analyzer, current, size);

                                if (pi != null)
                                    result.Add(pi);
                            }

                            current++;
                        }
                    }
                }
            }

            return result;
        }

        private PeakInfo AnalyzePeak(IPeakAnalyzer analyzer, SpectrumDataPoint point, int size)
        {
            int index = sortedByIndex.BinarySearch(point, indexComparator); 

            sortedByIndex[index].InPeak = true;

            int left_index = -1;
            int right_index = -1;

            //Process left
            for (int i = 1; i < settings.PeakHalfSize; i++)
            {
                int current = index - i;
                if (current < 0)
                    break;

                if (sortedByIndex[current].InPeak)
                    break;

                sortedByIndex[current].InPeak = true;
                left_index = current;
            }


            //Process left
            for (int i = 1; i < settings.PeakHalfSize; i++)
            {
                int current = index + i;
                if (current >= size)
                    break;

                if (sortedByIndex[current].InPeak)
                    break;

                sortedByIndex[current].InPeak = true;
                right_index = current;
            }

            if (left_index >= 0 && right_index >= 0)
            {
                return analyzer.Analyze(sortedByIndex, left_index, right_index);
            }

            return null;
        }

        private bool CheckPeak(SpectrumDataPoint point, int size)
        {
            int peakIndex = sortedByIndex.BinarySearch(point, indexComparator); 

            if (peakIndex - settings.MinPeakHalfSize < 0)
                return false;

            if (peakIndex + settings.MinPeakHalfSize >= size)
                return false;

            if (sortedByIndex[peakIndex - 1].Y > sortedByIndex[peakIndex].Y
                || sortedByIndex[peakIndex + 1].Y > sortedByIndex[peakIndex].Y)
                return false;



            for (int i = 1; i < settings.MinPeakHalfSize; i++)
            {
                int index = peakIndex - i;
                if (sortedByIndex[index].InPeak)
                    return false;

                index = peakIndex + i;
                if (sortedByIndex[index].InPeak)
                    return false;
            }

            
            return true;
        }

        private void Initialize(double[] xs, double[] ys, double min, double max)
        {
            Stopwatch timer = Stopwatch.StartNew();

            start_index = 0;
            end_index = xs.Length - 1;

            sortedByIntensity.Clear();
            sortedByIntensity.Capacity = xs.Length;

            sortedByIndex.Clear();
            sortedByIndex.Capacity = xs.Length;

            List<double> search = new List<double>(xs);

            int minIndex = search.BinarySearch(min);

            if (minIndex < 0)
                minIndex = ~minIndex;

            int maxIndex = search.BinarySearch(max);

            if (maxIndex < 0)
                maxIndex = ~maxIndex;

            if (minIndex > start_index)
                start_index = minIndex;

            if (maxIndex < end_index)
                end_index = maxIndex;


            //Console.WriteLine("Reindex {0} ms", timer.ElapsedMilliseconds);

            timer.Restart();
            for (int i = start_index; i <= end_index; i++)
            {
                var point = new SpectrumDataPoint() { X = xs[i], Y = ys[i], Index = i };
                sortedByIntensity.Add(point);
                sortedByIndex.Add(point);

                if (i == start_index)
                {
                    this.min = ys[i];
                    this.max = ys[i];
                }
                else
                {
                    if (ys[i] > this.max)
                        this.max = ys[i];

                    if (ys[i] < this.min)
                    {
                        this.min = ys[i];
                    }
                }
            }

            //Console.WriteLine("Fill arrays {0} ms", timer.ElapsedMilliseconds);

            timer.Restart();
            sortedByIntensity.Sort(YCompare);
            //BubbleSort<SpectrumDataPoint>(sortedByIntensity, new IntensityComparer());
            //Console.WriteLine("Sort array {0} ms", timer.ElapsedMilliseconds);
        }

        private void InitializeLite(double[] xs, double[] ys, double min, double max)
        {
            Stopwatch timer = Stopwatch.StartNew();

            start_index = 0;
            end_index = xs.Length - 1;

            sortedByIntensity.Clear();
            sortedByIntensity.Capacity = xs.Length;

            sortedByIndex.Clear();
            sortedByIndex.Capacity = xs.Length;

            List<double> search = new List<double>(xs);

            int minIndex = search.BinarySearch(min);

            if (minIndex < 0)
                minIndex = ~minIndex;

            int maxIndex = search.BinarySearch(max);

            if (maxIndex < 0)
                maxIndex = ~maxIndex;

            if (minIndex > start_index)
                start_index = minIndex;

            if (maxIndex < end_index)
                end_index = maxIndex;


            //Console.WriteLine("Reindex {0} ms", timer.ElapsedMilliseconds);

            timer.Restart();
            for (int i = start_index; i <= end_index; i++)
            {
                var point = new SpectrumDataPoint() { X = xs[i], Y = ys[i], Index = i };
                //sortedByIntensity.Add(point);
                sortedByIndex.Add(point);

                if (i == start_index)
                {
                    this.min = ys[i];
                    this.max = ys[i];
                }
                else
                {
                    if (ys[i] > this.max)
                        this.max = ys[i];

                    if (ys[i] < this.min)
                    {
                        this.min = ys[i];
                    }
                }
            }

            //Console.WriteLine("Fill arrays {0} ms", timer.ElapsedMilliseconds);

            //timer.Restart();
            //sortedByIntensity.Sort(YCompare);
            //BubbleSort<SpectrumDataPoint>(sortedByIntensity, new IntensityComparer());
            //Console.WriteLine("Sort array {0} ms", timer.ElapsedMilliseconds);
        }

        private bool CheckPeakLite(int peakIndex, int size)
        {
            //int peakIndex = sortedByIndex.BinarySearch(point, indexComparator);

            if (peakIndex - settings.MinPeakHalfSize < 0)
                return false;

            if (peakIndex + settings.MinPeakHalfSize >= size)
                return false;

            if (sortedByIndex[peakIndex - 1].Y >= sortedByIndex[peakIndex].Y
                || sortedByIndex[peakIndex + 1].Y >= sortedByIndex[peakIndex].Y)
                return false;



            for (int i = 1; i < settings.MinPeakHalfSize; i++)
            {
                int index = peakIndex - i;
                if (sortedByIndex[index].InPeak)
                    return false;

                index = peakIndex + i;
                if (sortedByIndex[index].InPeak)
                    return false;
            }


            return true;
        }

        private PeakInfo AnalyzePeakLite(IPeakAnalyzer analyzer,int index, int size)
        {
            //int index = sortedByIndex.BinarySearch(point, indexComparator);

            sortedByIndex[index].InPeak = true;

            int left_index = -1;
            int right_index = -1;

            //Process left
            for (int i = 1; i < settings.PeakHalfSize; i++)
            {
                int current = index - i;
                if (current < 0)
                    break;

                if (sortedByIndex[current].InPeak)
                    break;

                sortedByIndex[current].InPeak = true;
                left_index = current;
            }


            //Process left
            for (int i = 1; i < settings.PeakHalfSize; i++)
            {
                int current = index + i;
                if (current >= size)
                    break;

                if (sortedByIndex[current].InPeak)
                    break;

                sortedByIndex[current].InPeak = true;
                right_index = current;
            }

            if (left_index >= 0 && right_index >= 0)
            {
                return analyzer.Analyze(sortedByIndex, left_index, right_index);
            }

            return null;
        }

        private static int YCompare(SpectrumDataPoint l, SpectrumDataPoint r)
        {
            //return l.Y.CompareTo(r.Y);
            return r.Y.CompareTo(l.Y);

        }

        private static int IndexCompare(SpectrumDataPoint l, SpectrumDataPoint r)
        {
            return l.Index.CompareTo(r.Index);
        }

        private static int PositionCompare(SpectrumDataPoint l, SpectrumDataPoint r)
        {
            return l.X.CompareTo(r.X);
        }

        public class IndexComparer : IComparer<SpectrumDataPoint>
        {

            public int Compare(SpectrumDataPoint x, SpectrumDataPoint y)
            {

                return x.Index.CompareTo(y.Index);

            }

        }

        public class IntensityComparer : IComparer<SpectrumDataPoint>
        {
            public int Compare(SpectrumDataPoint l, SpectrumDataPoint r)
            {

                return r.Y.CompareTo(l.Y);

            }
        }
        
      
    }
}
