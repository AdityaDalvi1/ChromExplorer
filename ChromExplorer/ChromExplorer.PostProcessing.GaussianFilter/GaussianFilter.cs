using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Windows.Media;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Spectrum;
using ChromExplorer.PostProcessing.GaussianFilter.Views;
using log4net;

namespace ChromExplorer.PostProcessing.GaussianFilter
{
    [Export(typeof(IPostProcessingOperation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class GaussianFilter : IPostProcessingOperation
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ConfigurationParameters _configuration;
        private GaussianFilterConfigView _configView;
        private int _currentIterations = 1;
        private int _lastLength = -1;
        private double _lastXStart = double.NaN;
        private double _lastXEnd = double.NaN;
        private double _lastSigma;
        private int _lastKernelSize;
        private bool _isActive;

        public object ConfigView
        {
            get { return _configView; }
        }

        public GaussianFilter()
        {
            ObjectKey = "GaussianFilter";
            Title = "Gaussian Filter";

            _isActive = false;
            _configuration = ConfigurationParameters.DefaultValue;

            _configView = new GaussianFilterConfigView();
            _configView.DataContext = _configuration;

            _isActive = _configuration.IsActive;
            _lastSigma = _configuration.Sigma;
            _lastKernelSize = _configuration.KernelSize;
        }

        #region Implementation of IResourceDescriptor

        public string ObjectKey { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tooltip { get; set; }
        public ImageSource Icon { get; set; }

        #endregion

        #region Implementation of IPostProcessingOperation

        public int Order { get { return 15; } } // Slightly higher order than Savitzky-Golay (10)

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

        public PostProcessingType Type { get { return PostProcessingType.MS; } }

        public void ApplyInPlace(MsSpectrum spectrum)
        {
            if (spectrum == null)
                return;

            try
            {
                EnsureIterationsForInput(spectrum);
                double[] x;
                double[] y;

                Filter(spectrum.Points.Xvals, spectrum.Points.Yvals, out x, out y);

                for (int i = 0; i < spectrum.Points.Yvals.Length; i++)
                {
                    spectrum.Points.Yvals[i] = y[i];
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public MsSpectrum Apply(MsSpectrum spectrum)
        {
            if (spectrum == null)
                return null;

            EnsureIterationsForInput(spectrum);
            double[] x;
            double[] y;

            Filter(spectrum.Points.Xvals, spectrum.Points.Yvals, out x, out y);

            spectrum.Points = new SimpleDataSource(x, y);

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
            double[] xvals = null;
            int length = -1;

            if (input is SimpleDataSource ds)
            {
                xvals = ds.Xvals;
                length = ds.Length;
            }
            else if (input is MsSpectrum spec)
            {
                xvals = spec.Points?.Xvals;
                length = spec.Points?.Length ?? -1;
            }

            double xStart = double.NaN;
            double xEnd = double.NaN;
            if (xvals != null && xvals.Length > 0)
            {
                xStart = xvals[0];
                xEnd = xvals[xvals.Length - 1];
            }

            // Compared by value (length + x-range) rather than array identity, because Filter()
            // always allocates new arrays, so successive Apply calls never share references even
            // when operating on the same logical spectrum/chromatogram.
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
                    if (Math.Abs(_configuration.Sigma - _lastSigma) > 1e-9 || _configuration.KernelSize != _lastKernelSize)
                    {
                        _currentIterations = 1;
                    }
                    else
                    {
                        _currentIterations++;
                    }

                    _lastSigma = _configuration.Sigma;
                    _lastKernelSize = _configuration.KernelSize;
                }
                else
                {
                    _currentIterations = 1;
                }
            }
        }

        #endregion

        #region Filter Implementation

        private double[] BuildGaussianKernel(double sigma, int kernelSize)
        {
            if (kernelSize % 2 == 0) kernelSize++; // Ensure kernel size is odd

            double[] kernel = new double[kernelSize];
            int radius = kernelSize / 2;
            double sum = 0;

            for (int i = 0; i < kernelSize; i++)
            {
                int x = i - radius;
                kernel[i] = Math.Exp(-(x * x) / (2 * sigma * sigma)) / (Math.Sqrt(2 * Math.PI) * sigma);
                sum += kernel[i];
            }

            // Normalize the kernel
            for (int i = 0; i < kernelSize; i++)
            {
                kernel[i] /= sum;
            }

            return kernel;
        }

        public void Filter(double[] x, double[] y, out double[] smoothedX, out double[] smoothedY)
        {
            int kernelSize = _configuration.KernelSize;
            double sigma = _configuration.Sigma;

            if (x.Length < kernelSize || sigma <= 0)
            {
                smoothedX = x;
                smoothedY = y;
                return;
            }

            double[] kernel = BuildGaussianKernel(sigma, kernelSize);
            int radius = kernelSize / 2;

            double[] tempX = new double[x.Length];
            double[] tempY = new double[y.Length];
            Array.Copy(x, tempX, x.Length);
            Array.Copy(y, tempY, y.Length);

            for (int iter = 0; iter < _currentIterations; iter++)
            {
                double[] nextY = new double[y.Length];
                for (int i = 0; i < x.Length; i++)
                {
                    double sum = 0;
                    for (int j = 0; j < kernelSize; j++)
                    {
                        int dataIndex = i + (j - radius);
                        if (dataIndex < 0) dataIndex = 0;
                        if (dataIndex >= x.Length) dataIndex = x.Length - 1;
                        sum += tempY[dataIndex] * kernel[j];
                    }
                    nextY[i] = sum;
                }
                tempY = nextY;
            }

            smoothedX = tempX;
            smoothedY = tempY;
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
