using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChromExplorer.Assets;
using ChromExplorer.DataModel.Builders;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.Spectrum;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.TestChromProvider.Model;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Logging;

namespace ChromExplorer.Modules.TestChromProvider.Services
{
    [Export(typeof(IChromDataProvider))]
    [ExactTypeMetadata(typeof(TestChromDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    // TODO [Log(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error)]
    public class TestChromDataProvider : IChromDataProvider
    {
        private readonly ILoggerFacade _logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly TestSpectrumCollection _spectrums = new TestSpectrumCollection();

        [ImportingConstructor]
        public TestChromDataProvider(ILoggerFacade logger, IEventAggregator eventAggregator)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
            _logger.Log("TestChromDataProvider created", Category.Debug, Priority.Low);

            _eventAggregator = eventAggregator;

            _chromatogram = new GenericChromatogram();
            InitDescriptors();
        }

        private Uri _importViewUri = new Uri("TestImportView", UriKind.Relative);
        public Uri ImportSettingsViewKey
        {
            get { return _importViewUri; }
        }

        public bool Load(string filename, bool async = true)
        {
            _logger.Log(String.Format("TestChromDataProvider: Load({0}) called", filename), Category.Debug, Priority.Low);

            return false;
        }

        private GenericChromatogram _chromatogram;
        public GenericChromatogram Chromatogram
        {
            get { return _chromatogram; }
        }

        public string Filename { get; private set; }

        public void BuildTestChrom(int chromPoints, int spectrumPoints, string title, string description)
        {
            _spectrums.Clear();
            //_chromatogram.ChromPoints.Clear();

            var builder = new SineWaveformBuilder();
            var config = new WaveformConfigBase()
                             {
                                 Amplitude = 1.0,
                                 Offset = 1.0,
                                 Phase = 0,
                                 Samples = spectrumPoints,
                                 Frequency = 10,
                                 SamplingRate = (chromPoints + 10) * 2.0,

                             };

            var cps = new List<ChromatogramPoint>();
            for(int i = 0; i < chromPoints; i++)
            {
                config.Frequency = 10 + i;
                config.Amplitude = 1.0 + i;
                config.Offset = config.Amplitude;
                var ds = builder.CreateDataSource(config);

                var spectrum = new MsSpectrum(ds);

                _spectrums.Add(spectrum);

                cps.Add(new ChromatogramPoint()
                            {
                                Index = i,
                                SpectrumID = i,
                                Time = i,
                                TimeUnits = "au",
                                TIC = spectrum.TIC
                            });
            }

            _spectrums.SpectrumSize = _spectrums[0].Points.Length;
            _spectrums.Positions = _spectrums[0].Points.Xvals;

            _chromatogram = new GenericChromatogram(_spectrums, new TestChromRebuilder(_spectrums), cps);
            _chromatogram.Title = title;
            _chromatogram.Description = description;
            Filename = "generated";
            _eventAggregator.GetEvent<ChromatogramChangedEvent>().Publish(_chromatogram);
        }

        public void BuildRampChrom(int points)
        {
            var x = new double[points];
            var y = new double[points];

            for (int i = 0; i < points; i++)
            {
                x[i] = i;
                y[i] = i;
            }

            _chromatogram.Points = new SimpleDataSource(x, y);
        }

        #region Implementation of IResourceDescriptor

        public string ObjectKey { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tooltip { get; set; }
        public ImageSource Icon { get; set; }

        private void InitDescriptors()
        {
            ObjectKey = "TestChromProvider";
            Title = "Test Data";
            Tooltip = "Test Data provider";
            Icon = new BitmapImage(AssetExtension.GetResourceUri("LargeIcons/Devices/phone.png"));
        }

        #endregion
    }
}
