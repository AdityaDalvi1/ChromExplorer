using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Numerics;
using System.Reflection;
using System.Windows.Media;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Spectrum;
using ChromExplorer.PostProcessing.SavitzkyGolaySmoother.Views;
using log4net;
using MathNet.Numerics.LinearAlgebra;

namespace ChromExplorer.PostProcessing.SavitzkyGolaySmoother
{
    [Export(typeof(IPostProcessingOperation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class EicSavitzkyGolaySmoother : IPostProcessingOperation
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ConfigurationParameters _configuration;
        private SGalayConfigurationView _configView;
        private double[] _c = null;
        private bool _isActive;
        private int _currentIterations = 1;
        private int _lastLength = -1;
        private double _lastXStart = double.NaN;
        private double _lastXEnd = double.NaN;
        private int _lastM;
        private int _lastNl;
        private int _lastNr;

        public object ConfigView
        {
            get { return _configView; }
        }

        public EicSavitzkyGolaySmoother()
        {
            ObjectKey = "EicSavitzkyGolaySmoother";
            Title = "Savitzky-Golay Smoother";

            _isActive = false;
            _configuration = new ConfigurationParameters();

            _configView = new SGalayConfigurationView();
            _configView.DataContext = _configuration;

            _isActive = _configuration.IsActive;
            _lastM = _configuration.M;
            _lastNl = _configuration.nL;
            _lastNr = _configuration.nR;
        }

        #region Implementation of IResourceDescriptor

        public string ObjectKey { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tooltip { get; set; }
        public ImageSource Icon { get; set; }

        #endregion

        #region Implementation of IPostProcessingOperation

        public int Order { get { return 100; } }

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (value == _isActive) return;
                _isActive = value;
                OnPropertyChanged("IsActive");
            }
        }

        public PostProcessingType Type { get { return PostProcessingType.EIC; } }

        public void ApplyInPlace(MsSpectrum spectrum)
        {
            // Not implemented for MS
        }

        public MsSpectrum Apply(MsSpectrum spectrum)
        {
            return spectrum;
        }

        public void ApplyInPlace(SimpleDataSource dataSource)
        {
            if (dataSource == null) return;
            EnsureIterationsForInput(dataSource);
            double[] x;
            double[] y;
            Filter(dataSource.Xvals, dataSource.Yvals, out x, out y);
            for (int i = 0; i < dataSource.Yvals.Length; i++)
            {
                dataSource.Yvals[i] = y[i];
            }
        }

        public SimpleDataSource Apply(SimpleDataSource dataSource)
        {
            if (dataSource == null) return null;
            EnsureIterationsForInput(dataSource);
            double[] x;
            double[] y;
            Filter(dataSource.Xvals, dataSource.Yvals, out x, out y);
            return new SimpleDataSource(x, y);
        }

        private void EnsureIterationsForInput(object input)
        {
            double xStart = double.NaN;
            double xEnd = double.NaN;
            int length = -1;

            if (input is SimpleDataSource ds)
            {
                length = ds.Length;
                if (ds.Xvals != null && ds.Xvals.Length > 0)
                {
                    xStart = ds.Xvals[0];
                    xEnd = ds.Xvals[ds.Xvals.Length - 1];
                }
            }

            bool signatureChanged = _lastLength != length
                                    || double.IsNaN(_lastXStart) || double.IsNaN(_lastXEnd)
                                    || Math.Abs(_lastXStart - xStart) > 1e-9
                                    || Math.Abs(_lastXEnd - xEnd) > 1e-9;

            if (signatureChanged)
            {
                _currentIterations = 1;
                _lastLength = length;
                _lastXStart = xStart;
                _lastXEnd = xEnd;
            }
        }

        public void SaveSettings()
        {
            if (_configuration != null)
            {
                _configuration.IsActive = IsActive;
                _configuration.Save();

                if (IsActive)
                {
                    // If parameters changed, reset iterations to 1.
                    // Otherwise, increment iterations to apply the filter repeatedly.
                    if (_configuration.M != _lastM || _configuration.nL != _lastNl || _configuration.nR != _lastNr)
                    {
                        _currentIterations = 1;
                    }
                    else
                    {
                        _currentIterations++;
                    }

                    _lastM = _configuration.M;
                    _lastNl = _configuration.nL;
                    _lastNr = _configuration.nR;
                }
                else
                {
                    _currentIterations = 1;
                }
            }
        }

        #endregion

        #region Filter Implementation

        private void BuildCoefficients()
        {
            int size = _configuration.nL + _configuration.nR + 1;
            int order = _configuration.M;
            int nl = _configuration.nL;
            int nr = _configuration.nR;

            _c = new double[size];

            Matrix<double> A = Matrix<double>.Build.Dense(size, order + 1);

            for (int i = -nl; i <= nr; i++)
            {
                for (int j = 0; j <= order; j++)
                {
                    A[i + nl, j] = (int)BigInteger.Pow(i, j);
                }
            }

            Matrix<double> B = A.Clone();
            var BT = B.Transpose();
            var BB = BT.Multiply(A);

            Matrix<double> InverseB = BB.Inverse();

            Vector<double> m = InverseB.Row(0);

            for (int i = -nl; i <= nr; i++)
            {
                Vector<double> n = Vector<double>.Build.Dense(order + 1);
                for (int j = 0; j <= order; j++)
                {
                    n[j] = (int)BigInteger.Pow(i, j);
                }

                _c[i + nl] = m.DotProduct(n);
            }
        }

        public void Filter(double[] x, double[] y, out double[] smoothedX, out double[] smoothedY)
        {
            if (x.Length < _configuration.nL + _configuration.nR + 1)
            {
                smoothedX = x;
                smoothedY = y;
                return;
            }

            BuildCoefficients();

            double[] tempX = x;
            double[] tempY = y;

            int iterations = _currentIterations < 1 ? 1 : _currentIterations;
            for (int iter = 0; iter < iterations; iter++)
            {
                double[] nextX;
                double[] nextY;
                SmoothOnce(tempX, tempY, out nextX, out nextY);
                tempX = nextX;
                tempY = nextY;
            }

            smoothedX = tempX;
            smoothedY = tempY;
        }

        private void SmoothOnce(double[] x, double[] y, out double[] smoothedX, out double[] smoothedY)
        {
            smoothedX = new double[x.Length];
            smoothedY = new double[y.Length];

            for (int i = 0; i < _configuration.nL; i++)
            {
                smoothedX[i] = x[i];
                smoothedY[i] = y[i];
            }

            for (int i = _configuration.nL; i < x.Length - _configuration.nR; i++)
            {
                double sum = 0.0;
                for (int j = i - _configuration.nL; j < i + _configuration.nR + 1; j++)
                {
                    sum += y[j] * _c[j - i + _configuration.nL];
                }

                smoothedX[i] = x[i];
                smoothedY[i] = sum;
            }

            for (int i = 0; i < _configuration.nL; i++)
            {
                smoothedY[i] = smoothedY[_configuration.nL];
            }

            for (int i = x.Length - _configuration.nR; i < x.Length; i++)
            {
                smoothedX[i] = x[i];
                smoothedY[i] = smoothedY[x.Length - _configuration.nR - 1];
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
