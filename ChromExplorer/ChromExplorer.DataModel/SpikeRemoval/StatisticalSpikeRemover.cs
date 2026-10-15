using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.PeakPicking;
using log4net;

namespace ChromExplorer.DataModel.SpikeRemoval
{
    public struct SpectrumPoint
    {
        public int Index;
        public double Intensity;

    }

    public class StatisticalSpikeRemover
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


        private readonly SimpleDataSource _ds;
        private readonly List<SpectrumPoint> _sorted;
        private readonly List<double> _std;
        private readonly List<double> _increments;
        private List<PeakInfo> _peaks = null; 

        public StatisticalSpikeRemover(SimpleDataSource ds)
        {
            _ds = ds;
            _sorted = new List<SpectrumPoint>();
            _std = new List<double>();
            _increments = new List<double>();

            PPD = 25.0;
            MaxWidth = 3.0;
            MinWidth = 1.0/25.0;
            PeakPickingThreshold = 5;
            Mult = 3.0;
            HWHM = 0.2;

        }

        public double PeakPickingThreshold { get; set; }
    
        public double MinWidth { get; set; }
        public double MaxWidth { get; set; }
        public double PPD { get; set; }
        public double Mult { get; set; }
        public double HWHM { get; set; }

        public void PeakPeaks()
        {
            var s = new PeakPickingSettings();
            s.MinPeakHalfSize = (int)(MinWidth*PPD);
            if (s.MinPeakHalfSize < 1)
                s.MinPeakHalfSize = 1;

            s.PeakHalfSize = (int)(MaxWidth * PPD);
            if (s.PeakHalfSize < s.MinPeakHalfSize)
                s.PeakHalfSize = s.MinPeakHalfSize;

            var pp = new PeakPicker(s);

            _peaks = pp.FindPeaks<CentroidPeakAnalyzer>(DS.Xvals, DS.Yvals, PeakPickingThreshold, false);

            int index = 0;
            foreach (var peakInfo in _peaks)
            {
                _sorted.Add(new SpectrumPoint() {Index = index++, Intensity = peakInfo.Intensity});
            }

        }

        public List<SpectrumPoint> SortedIntensity
        {
            get { return _sorted; }
        }

        public List<double> Std
        {
            get { return _std; }
        }

        public List<double> Increments
        {
            get { return _increments; }
        }

        public SimpleDataSource DS
        {
            get { return _ds; }
        }

        public void Sort()
        {
            _sorted.Sort(YCompare);   
        }

        public void Cleanup(bool fill = false)
        {
            int cutoffIndex = -1;

            double previousStd = 0;
            double s = 0.0;
            double ss = 0.0;

            var increments = new List<double>();
            for (int i = 0; i < _sorted.Count; i++)
            {
                s += _sorted[i].Intensity;
                ss += _sorted[i].Intensity*_sorted[i].Intensity;

                double std = Math.Sqrt(ss/(i + 1) - (s/(i + 1))*(s/(i + 1)));

                if (i > 0)
                {
                    if (fill)
                    {
                        _std.Add(std);
                    }

                    if (i == 1)
                    {
                        previousStd = std;
                    } 
                    else
                    {
                        double inc = std - previousStd;
                        previousStd = std;
                        increments.Add(inc);
                      

                        if (fill)
                        {
                            _increments.Add(inc);
                        }
                    }
                }

                
            }

            var median = increments.Median();

            for (int i = 0; i < increments.Count; i++)
            {
                if (increments[i] > Mult * median)
                {
                    cutoffIndex = i + 2;
                    break;
                }
            }

            //Console.WriteLine("Cutoff index = {0}", cutoffIndex);

            if (cutoffIndex > 0)
            {
                log.InfoFormat("Spike removal: Total peaks: {0}, cutoof index {1}", _peaks.Count, cutoffIndex);
                for (int i = cutoffIndex; i < _sorted.Count; i++)
                {
                    var p = _peaks[_sorted[i].Index];

                    if (p.HWHM < HWHM)
                    {
                        for (int j = p.LeftIndex; j < p.RightIndex; j++)
                        {
                            DS.Yvals[j] = 0;
                        }

                        log.InfoFormat("Spike at {0} has been removed (HWHM = {1})", p.Position, p.HWHM);
                    }
                }
            }
        }

        private static int YCompare(SpectrumPoint l, SpectrumPoint r)
        {
            //return l.Y.CompareTo(r.Y);
            return l.Intensity.CompareTo(r.Intensity);
        }


        public static void Save(string filename, IEnumerable<double> points)
        {
            var file = new StreamWriter(filename);

            int index = 0;
            foreach (var p in points)
            {
                file.WriteLine("{0}\t{1}", index++, p);
            }

            file.Close();
        }

        public static void Save(string filename, IEnumerable<SpectrumPoint> points)
        {
            var file = new StreamWriter(filename);

            foreach (var spectrumPoint in points)
            {
                file.WriteLine("{0}\t{1}", spectrumPoint.Index, spectrumPoint.Intensity);      
            }
          
            file.Close();
        }

        public void RemoveSpikes()
        {
            this.PeakPeaks();
            this.Sort();
            this.Cleanup();
        }
    }
}
