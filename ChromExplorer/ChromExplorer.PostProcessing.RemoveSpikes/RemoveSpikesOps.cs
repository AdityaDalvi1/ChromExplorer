using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Reflection;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Spectrum;
using ChromExplorer.DataModel.SpikeRemoval;
using ChromExplorer.PostProcessing.RemoveSpikes.Views;
using log4net;

namespace ChromExplorer.PostProcessing.RemoveSpikes
{
    [Export(typeof(IPostProcessingOperation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class RemoveSpikesOps : IPostProcessingOperation
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ConfigurationParameters _config;
        private readonly RemoveSpikesConfig _configView;
        private bool _isActive;

        public RemoveSpikesOps()
        {
            _config = new ConfigurationParameters();
            _configView = new RemoveSpikesConfig {DataContext = _config};

            ObjectKey = "RemoveSpikes";
            Title = "Remove spikes";

            _isActive = _config.IsActive;
            Order = 1;
        }

        #region Implementation of IPostProcessingOperation

        public string ObjectKey { get; set; }
        public string Title { get; set; }
        
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

        public object ConfigView {
            get { return _configView; }
        }
        public int Order { get; private set; }
        public PostProcessingType Type { get { return PostProcessingType.MS; } }

        //TODO Check logic in all operations. Should we return new spectrum or not?
        public void ApplyInPlace(MsSpectrum spectrum)
        {
            if(!spectrum.BaselineRemoved)
                return;

            if (spectrum.Type != SpectrumType.MS)
                return;
          
            var r = new StatisticalSpikeRemover(spectrum.Points);
            Configure(r, spectrum);
            r.RemoveSpikes();
            spectrum.UpdateStatistics();
        }

        private void Configure(StatisticalSpikeRemover statisticalSpikeRemover, MsSpectrum spectrum)
        {
            statisticalSpikeRemover.PPD = spectrum.PPD;
            statisticalSpikeRemover.Mult = _config.MedianMultiplier;
            statisticalSpikeRemover.HWHM = _config.WidthThreshold;
            statisticalSpikeRemover.MinWidth = _config.MinWidth;
            statisticalSpikeRemover.MaxWidth = _config.MaxWidth;
            statisticalSpikeRemover.PeakPickingThreshold = _config.PPThreshold;
        }

        
        public MsSpectrum Apply(MsSpectrum spectrum)
        {
            var timer = Stopwatch.StartNew();
            if (!spectrum.BaselineRemoved)
                return spectrum;

            if (spectrum.Type != SpectrumType.MS)
                return spectrum;

            var x = new double[spectrum.Points.Length];
            var y = new double[spectrum.Points.Length];

            for (int i = 0; i < x.Length; i++)
            {
                x[i] = spectrum.Points.Xvals[i];
                y[i] = spectrum.Points.Yvals[i];
            }

            var r = new StatisticalSpikeRemover(new SimpleDataSource(x, y));
            Configure(r, spectrum);
            r.RemoveSpikes();

            spectrum.Points = new SimpleDataSource(r.DS.Xvals, r.DS.Yvals);

            log.InfoFormat("Spike removal took: {0} ms", timer.ElapsedMilliseconds);
            return  spectrum;
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
            if (_config != null)
            {
                _config.IsActive = IsActive;
                _config.Save();
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
