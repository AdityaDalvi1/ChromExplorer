using MathNet.Filtering;
using System;
using System.Linq;

namespace ChromExplorer.DataModel.PostProcessing
{
    //Fast and Precise Automatic Baseline Correction of One and Two-Dimensional NMR Spectra
    //WOLFGANGDIETRICH,* CHRISTIANH.RODEL, AND MARKUSNEUMANNL

    public class BaselineEstimator
    {
        public enum AggregationStrategy
        {
            None,
            Raw,
            Refined
        }
        public class RunStats
        {
            public RunStats()
            {
                Reset();
            }
            public void Reset()
            {
                OneSpikes = 0;
                TwoSpikes = 0;
                ThreeSpikes = 0;
                Iterations = 0;
            }
            public int OneSpikes { get; set; }
            public int TwoSpikes { get; set; }
            public int ThreeSpikes { get; set; }
            public int Iterations { get; set; }

            public override string ToString()
            {
                return string.Format("Spikes: {0}, {1}, {2}, iterations {3}", OneSpikes, TwoSpikes, ThreeSpikes, Iterations);
            }
        }

        private double[] _x;
        private double[] _y;

        private readonly bool[] _inPeak;
        private readonly bool[] _inSpike;
        private readonly double[] _yRefined;
        private readonly double[] _yAggregated;


        private double[] _noiseLevel;
        private double[] _noiseHalfWidth;
        private readonly int[] _binSizes;


        private double _tic;
        private double _sic;
        private double _sigma = 1.0;


        private readonly int _binSize;
        private readonly int _windowSize;
        private double _noiseMult = 3.0;

        private double _mean;
        private double _stDev;
        private bool _hasPeaks;

        private readonly RunStats _stats = new RunStats();


        public BaselineEstimator(int size, int binSize = 256, int windowSize = 3)
        {
            _x = new double[size];
            _y = new double[size];
            _inPeak = new bool[size];
            _inSpike = new bool[size];
            _yRefined = new double[size];
            _binSize = binSize;

            int bins = size / _binSize;

            if (size % _binSize > 0)
                bins++;

            _noiseLevel = new double[bins];
            _noiseHalfWidth = new double[bins];
            _binSizes = new int[bins];
            _windowSize = windowSize;
            _tic = 0;

        }

        public bool[] InPeak
        {
            get { return _inPeak; }
        }

        public double[] X
        {
            get { return _x; }
        }

        public double[] Y
        {
            get { return _y; }
        }

        public RunStats Stats
        {
            get { return _stats; }
        }

        public bool[] InSpike
        {
            get { return _inSpike; }
        }

        public double[] NoiseLevel
        {
            get { return _noiseLevel; }
        }

        public int BinSize
        {
            get { return _binSize; }
        }

        public double[] NoiseHalfWidth
        {
            get { return _noiseHalfWidth; }
        }

        public double[] YRefined
        {
            get { return _yRefined; }
        }

        public double Tic
        {
            get { return _tic; }
        }

        public double Sic
        {
            get { return _sic; }
        }

        public double Sigma
        {
            get { return _sigma; }
        }


        public int WindowSize
        {
            get { return _windowSize; }
        }

        public double NoiseMult
        {
            get { return _noiseMult; }
            set { _noiseMult = value; }
        }


        public bool Estimate(double[] x, double[] y, AggregationStrategy aggregationStrategy = AggregationStrategy.None)
        {

            if (x.Length != _inPeak.Length)
                return false;

            if (y.Length != _inPeak.Length)
                return false;

            _stats.Reset();



            if (aggregationStrategy == AggregationStrategy.Raw)
            {
                for (int i = 0; i < _x.Length; i++)
                {
                    _x[i] = x[i];
                    _y[i] += y[i];
                }

            }
            else
            {
                for (int i = 0; i < _x.Length; i++)
                {
                    _x[i] = x[i];
                    _y[i] = y[i];
                }
            }

            Reset();


            _hasPeaks = true;
            while (_hasPeaks)
            {
                UpdateStat();
                UpdateThreshold();

                _stats.Iterations++;

                if (_stats.Iterations > 10)
                    break;
            }

            CleanUp();
            EstimateBaseline();
            SubtractBaseline(aggregationStrategy);
            ValueBasedCleanup();
            EstimateTic();
            return true;

        }

        private void EstimateTic()
        {
            _tic = 0.0;
            _sic = 0.0;
            for (var index = 0; index < _yRefined.Length; index++)
            {
                var v = _yRefined[index];
                _tic += v;

                if (_x[index] > _xMin && _x[index] < _xMax)
                    _sic += v;
            }
        }

        private void SubtractBaseline(AggregationStrategy aggregationStrategy)
        {
            if (aggregationStrategy != AggregationStrategy.Refined)
            {
                for (int i = 0; i < _yRefined.Length; i++)
                {
                    _yRefined[i] = 0;
                }
            }

            for (int i = 0; i < _y.Length; i++)
            {
                if (_inSpike[i])
                {
                    continue;
                }

                if (_inSpike[i])
                {
                    continue;
                }

                int binIndex = i / _binSize;

                if (_y[i] > _noiseLevel[binIndex] + _noiseHalfWidth[binIndex])
                {
                    _yRefined[i] += _y[i] - _noiseLevel[binIndex];
                }


            }
        }

        private void EstimateBaseline()
        {
            for (int i = 0; i < _y.Length; i++)
            {
                int binIndex = i / _binSize;

                if (!_inPeak[i] && !_inSpike[i])
                {
                    _noiseLevel[binIndex] += _y[i];
                    _binSizes[binIndex]++;
                }
            }

            for (int i = 0; i < _noiseLevel.Length; i++)
            {
                _noiseLevel[i] /= _binSizes[i];

            }

            var filter = OnlineFilter.CreateDenoise(_windowSize);


            _noiseLevel = filter.ProcessSamples(_noiseLevel);

            for (int i = 0; i < _y.Length; i++)
            {
                int binIndex = i / _binSize;

                if (!_inPeak[i] && !_inSpike[i])
                {
                    _noiseHalfWidth[binIndex] += (_y[i] - _noiseLevel[binIndex]) * (_y[i] - _noiseLevel[binIndex]);
                }
            }

            for (int i = 0; i < _noiseLevel.Length; i++)
            {
                _noiseHalfWidth[i] = _noiseMult * Math.Sqrt(_noiseHalfWidth[i] / (_binSizes[i]));

            }

            filter = OnlineFilter.CreateDenoise(_windowSize);
            _noiseHalfWidth = filter.ProcessSamples(_noiseHalfWidth);

            _sigma = _noiseHalfWidth.Sum() / _noiseHalfWidth.Length;
        }

        private void Reset()
        {
            for (int i = 0; i < _inPeak.Length; i++)
            {
                _inPeak[i] = false;
                _inSpike[i] = false;
            }


            for (int i = 0; i < _noiseLevel.Length; i++)
            {
                _noiseLevel[i] = 0.0;
                _noiseHalfWidth[i] = 0.0;
                _binSizes[i] = 0;
            }
        }

        private void UpdateThreshold()
        {
            double tr = _mean + 3 * _stDev;
            _hasPeaks = false;
            for (int i = 0; i < _inPeak.Length; i++)
            {
                if (_y[i] > tr)
                {

                    _inPeak[i] = true;
                    _hasPeaks = true;
                }

            }
        }

        private double _epsilon = 1e-06;

        private void ValueBasedCleanup()
        {
            for (int i = 1; i < _yRefined.Length - 1; i++)
            {
                if (_yRefined[i] > 0 && _yRefined[i - 1] < _epsilon && _yRefined[i + 1] < _epsilon)
                {
                    _yRefined[i] = 0.0;
                }
            }

            for (int i = 1; i < _yRefined.Length - 2; i++)
            {
                if (_yRefined[i] > 0 && _yRefined[i + 1] > 0 && _yRefined[i - 1] < _epsilon && _yRefined[i + 2] < _epsilon)
                {
                    _yRefined[i] = 0.0;
                    _yRefined[i + 1] = 0.0;
                }
            }

            for (int i = 1; i < _yRefined.Length - 3; i++)
            {
                if (_yRefined[i] > 0 && _yRefined[i + 1] > 0 && _yRefined[i + 2] > 0 && _yRefined[i - 1] < _epsilon && _yRefined[i + 3] < _epsilon)
                {
                    _yRefined[i] = 0.0;
                    _yRefined[i + 1] = 0.0;
                    _yRefined[i + 2] = 0.0;
                }
            }
        }


        private void CleanUp()
        {
            for (int i = 1; i < _y.Length - 1; i++)
            {
                if (_inPeak[i] && !_inPeak[i - 1] && !_inPeak[i + 1])
                {
                    _inPeak[i] = false;
                    _inSpike[i] = true;
                    _stats.OneSpikes++;
                }
            }

            for (int i = 1; i < _y.Length - 2; i++)
            {
                if (_inPeak[i] && _inPeak[i + 1] && !_inPeak[i - 1] && !_inPeak[i + 2])
                {
                    _inPeak[i] = false;
                    _inPeak[i + 1] = false;
                    _inSpike[i] = true;
                    _inSpike[i + 1] = true;
                    _stats.TwoSpikes++;
                }
            }

            for (int i = 1; i < _y.Length - 3; i++)
            {
                if (_inPeak[i] && _inPeak[i + 1] && _inPeak[i + 2] && !_inPeak[i - 1] && !_inPeak[i + 3])
                {
                    _inPeak[i] = false;
                    _inPeak[i + 1] = false;
                    _inPeak[i + 2] = false;
                    _inSpike[i] = true;
                    _inSpike[i + 1] = true;
                    _inSpike[i + 2] = true;
                    _stats.ThreeSpikes++;
                }
            }

        }

        private void UpdateStat()
        {
            double s = 0;

            for (int i = 0; i < _y.Length; i++)
            {
                if (!_inPeak[i])
                {
                    s += _y[i];
                }
            }

            _mean = s / _y.Length;

            s = 0;

            for (int i = 0; i < _y.Length; i++)
            {
                if (!_inPeak[i])
                {
                    s += (_y[i] - _mean) * (_y[i] - _mean);
                }
            }

            _stDev = Math.Sqrt(s / (_y.Length - 1));
        }

        private double _xMin;
        private double _xMax;
        public void SetSicRange(double zoomRectXMin, double zoomRectXMax)
        {
            _xMin = zoomRectXMin;
            _xMax = zoomRectXMax;
        }
    }
}