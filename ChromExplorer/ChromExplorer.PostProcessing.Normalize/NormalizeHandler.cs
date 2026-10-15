using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Media;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Spectrum;
using ChromExplorer.PostProcessing.Normalize.Views;

namespace ChromExplorer.PostProcessing.Normalize
{
    [Export(typeof(IPostProcessingOperation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class NormalizeHandler : IPostProcessingOperation
    {
        private readonly ConfigurationParameters _configuration;
        private NormalizeConfigView _configView;
        private bool _isActive;

        public NormalizeHandler()
        {
            _configuration = new ConfigurationParameters();
            _configView = new NormalizeConfigView();
            _configView.DataContext = _configuration;
            ObjectKey = "Normalize";
            Title = "Normalize";

            _isActive = _configuration.IsActive;
        }

        #region Implementation of IResourceDescriptor

        public string ObjectKey { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tooltip { get; set; }
        public ImageSource Icon { get; set; }
        public int Order { get { return 5; } }
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

            spectrum.UpdateStatistics();

            double max = spectrum.Max;
            double min = spectrum.Min;

            double offset = 0;
            double scale = max;
            
            if(_configuration.ShiftToZero)
            {
                offset = min;
                scale = (max - min)/_configuration.NormalizeTo;
            }
   
            var x = new double[spectrum.Points.Length];
            var y = new double[spectrum.Points.Length];

            for (int i = 0; i < x.Length; i++)
            {
                x[i] = spectrum.Points.Xvals[i];
                y[i] = (spectrum.Points.Yvals[i] - offset)/ scale;
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

        public void SaveSettings()
        {
            if (_configuration != null)
            {
                _configuration.IsActive = IsActive;
                _configuration.Save();
            }
            //throw new System.NotImplementedException();
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
