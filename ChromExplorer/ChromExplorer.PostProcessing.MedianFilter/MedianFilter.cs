using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Media;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Spectrum;
using ChromExplorer.PostProcessing.MedianFilter.Views;
using MathNet.Filtering;

namespace ChromExplorer.PostProcessing.MedianFilter
{
    [Export(typeof(IPostProcessingOperation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class MedianFilter : IPostProcessingOperation
    {
        private readonly ConfigurationParameters _configuration;
        private MedianFilterConfigView _configView;
        private bool _isActive;
        private int _currentIterations = 1;
        private int _lastLength = -1;
        private double _lastXStart = double.NaN;
        private double _lastXEnd = double.NaN;
        private int _lastWindowSize;

        public MedianFilter()
        {
            _configuration = new ConfigurationParameters();
            _configView = new MedianFilterConfigView();
            _configView.DataContext = _configuration;
            ObjectKey = "MedianFilter";
            Title = "Median Filter";

            _isActive = _configuration.IsActive;
            _lastWindowSize = _configuration.MedianWindowSize;
        }

        #region Implementation of IResourceDescriptor

        public string ObjectKey { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tooltip { get; set; }
        public ImageSource Icon { get; set; }
        public int Order { get { return 10; } }
        public PostProcessingType Type { get { return PostProcessingType.MS; } }

        #endregion

        #region Implementation of IPostProcessingOperation

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

        public object ConfigView
        {
            get { return _configView; }
        }

        // TODO [Log]
        public void ApplyInPlace(MsSpectrum spectrum)
        {
            //throw new NotImplementedException();
            for (int i = 0; i < spectrum.Points.Yvals.Length; i++)
            {
                spectrum.Points.Yvals[i] = 0;
            }
        }

        // TODO [Log]
        public MsSpectrum Apply(MsSpectrum spectrum)
        {
            if (spectrum == null)
                return null;

            EnsureIterationsForInput(spectrum);

            var x = new double[spectrum.Points.Length];
            var y = new double[spectrum.Points.Length];

            for (int i = 0; i < x.Length; i++)
            {
                x[i] = spectrum.Points.Xvals[i];
                y[i] = spectrum.Points.Yvals[i];
            }

            var wsize = _configuration.MedianWindowSize;
            int offset = wsize / 2;

            int iterations = _currentIterations < 1 ? 1 : _currentIterations;
            for (int iter = 0; iter < iterations; iter++)
            {
                var filter = OnlineFilter.CreateDenoise(wsize);
                var sy = filter.ProcessSamples(y);

                for (int i = 0; i < y.Length; i++)
                {
                    var index = i + offset;

                    if (index < sy.Length)
                        y[i] = sy[index];
                }
            }

            spectrum.Points = new SimpleDataSource(x, y);
            return spectrum;
        }

        public void ApplyInPlace(SimpleDataSource dataSource)
        {
            // Not implemented for EIC
        }

        public SimpleDataSource Apply(SimpleDataSource dataSource)
        {
            // Not implemented for EIC
            return dataSource;
        }

        private void EnsureIterationsForInput(object input)
        {
            double[] xvals = null;
            int length = -1;

            if (input is MsSpectrum spec)
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

            // Compared by value (length + x-range) rather than array identity, because Apply()
            // always allocates new arrays, so successive Apply calls never share references even
            // when operating on the same logical spectrum.
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
                    if (_configuration.MedianWindowSize != _lastWindowSize)
                    {
                        _currentIterations = 1;
                    }
                    else
                    {
                        _currentIterations++;
                    }

                    _lastWindowSize = _configuration.MedianWindowSize;
                }
                else
                {
                    _currentIterations = 1;
                }
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
