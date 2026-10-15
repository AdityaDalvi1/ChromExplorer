using ChromExplorer.Assets;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Interfaces;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.ModasReader;
using log4net;
using Microsoft.Practices.Prism.Events;
using Microsoft.SqlServer.Server;
using Mindscape.LightSpeed.Querying;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChromExplorer.Modules.ModasChromProvider.Services
{
    [Export(typeof(IChromDataProvider))]
    [ExactTypeMetadata(typeof(ModasChromProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ModasChromProvider : IChromDataProvider
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private class LoadWorkerArgs
        {
            public TimeUnits Units { get; set; }
            public bool RecalcTic { get; set; }
            public double PPD { get; set; }
            public double MzFrom { get; set; }
            public double MzTo { get; set; }
            public double NFL { get; set; }
            public QueryExpression Query { get; set; }

            public bool RemoveBaseLine { get; set; }

            public bool NormalizeByIT { get; set; }

            public double ReferenceIT { get; set; }

        }


        private readonly IUserSettingsProvider _settingsProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly IStatusUpdateService _statusService;
        private readonly IPostProcessingService _postProcessingService;
        private readonly BackgroundWorker _loadWorker = null;
        private LoadWorkerArgs _runArgs = null;

        private ModasDataReader _data;

        [ImportingConstructor]
        public ModasChromProvider(IEventAggregator eventAggregator, IStatusUpdateService statusService,
            IPostProcessingService postProcessingService, IUserSettingsProvider settingsProvider)
        {

            _settingsProvider = settingsProvider;
            _postProcessingService = postProcessingService;
            _eventAggregator = eventAggregator;
            _statusService = statusService;

            InitDescriptors();

            _loadWorker = new BackgroundWorker();

            _loadWorker.DoWork += new DoWorkEventHandler(LoadFileAsyncHandler);
            _loadWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(LoadFileAsyncCompletedHandler);
        }

        private readonly Uri _importViewUri = new Uri("ModasImportView", UriKind.Relative);

        public Uri ImportSettingsViewKey
        {
            get { return _importViewUri; }
        }


        public bool Load(string filename, bool async = true)
        {
            log.InfoFormat("ModasChromProvider: Load({0}) called", filename);

            Filename = filename;
            var data = new ModasDataReader(filename);

            if (!data.IsModasFile)
            {
                return false;
            }

            if (data.UpdateRequired)
            {
                data.UpdateFromPreviousVersions();
            }

            data.LoadChromPoints(TimeUnits.Min, null);
            data.UpdateStatistic();

            bool recalcTic = true;
            bool removeBaseline = true;
            double nfl = ModasDataReader.DefaultNoiseFilterLevel;
            double ppd = data.PPD;
            TimeUnits units = TimeUnits.Min;
            bool normIT = true;
            double refIT = 100.0;

            if (_settingsProvider != null)
            {
                recalcTic = _settingsProvider.ModasImportSettings.RebuildTic;
                removeBaseline = _settingsProvider.ModasImportSettings.RemoveBaseLine;
                nfl = _settingsProvider.ModasImportSettings.NoiseFilterLevel;
                ppd = _settingsProvider.ModasImportSettings.PPD;
                units = _settingsProvider.ModasImportSettings.TimeUnits;

                normIT = _settingsProvider.ModasImportSettings.NormalizeByIT;
                refIT = _settingsProvider.ModasImportSettings.ReferenceIT;
            }

            
            BindToFile(data, units, recalcTic, data.MassFrom, data.MassTo, ppd, null, removeBaseline, nfl, normIT, refIT, async);

            return true;
        }

        public GenericChromatogram Chromatogram { get; private set; }
        public string Filename { get; private set; }

        #region Implementation of IResourceDescriptor

        public string ObjectKey { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tooltip { get; set; }
        public ImageSource Icon { get; set; }

        public bool IsBusy
        {
            get { return _loadWorker.IsBusy; }
        }

        private void InitDescriptors()
        {
            ObjectKey = "ModasChromProvider";
            Title = "Modas Chromatogram";
            Tooltip = "Modas DB3 Data provider";

            try
            {
                Icon = new BitmapImage(AssetExtension.GetResourceUri("LargeIcons/Devices/battery.png"));
            }
            catch (Exception)
            {
                log.Warn("LargeIcons/Devices/battery.png icon not found");
            }
           
        }

        #endregion

        private double _ppd = 0.0;
        public void BindToFile(ModasDataReader reader, TimeUnits units, bool recalcTic, double mzMin, double mzMax, double ppd, QueryExpression query, bool removeBaseLine, double nfl, bool normIT, double refIT, bool async = true)
        {
            if (_data != null)
            {
                _data.Dispose();
            }

            if (!_loadWorker.IsBusy)
            {
                _ppd = ppd;
                _data = reader;
                _data.BaselineRemoved = removeBaseLine;
                Filename = reader.FileName;
                _statusService.AppStatus = string.Format("Loading...");
                _statusService.SetWaitCursor(true);
                _runArgs = new LoadWorkerArgs { Units = units, RecalcTic = recalcTic, MzFrom = mzMin,
                    MzTo = mzMax, PPD = ppd, Query = query, RemoveBaseLine = removeBaseLine, NFL = nfl,
                    NormalizeByIT = normIT,
                    ReferenceIT = refIT
                };

                
                _loadWorker.RunWorkerAsync(_runArgs);

                if (!async)
                {
                    while(_loadWorker.IsBusy)
                        Thread.Sleep(100);
                }
                
                
            }
        }

        private void LoadFileAsyncCompletedHandler(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //TODO Error processing here 
                log.Error("LoadFileAsyncCompletedHandler: error detected", e.Error);
            }

            if(!_data.HasLoadErrors)
                _statusService.SetReadyStatus();
            
            var result = e.Result as IEnumerable<ChromatogramPoint>;

            if (result != null) {
                var fi = new FileInfo(_data.FileName);
                
                this.Chromatogram = new GenericChromatogram(_data, _data, result, _statusService, _postProcessingService)
                                        {
                                            Title = fi.Name,
                                            Description = fi.FullName,
                                            Filename = fi.FullName,
                                            PPD = _ppd
                                        };

                _eventAggregator.GetEvent<ChromatogramChangedEvent>().Publish(this.Chromatogram);
            }

            if (!_data.HasLoadErrors)
                _statusService.SetReadyStatus();

            _statusService.SetWaitCursor(false);
        }

        private void LoadFileAsyncHandler(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as LoadWorkerArgs;

            if (args == null)
                return;

            _data.SpectrumFilter = args.Query;
            e.Result = _data.ReadChromatogram(args.Units);

            _data.LoadData(args.MzFrom, args.MzTo, args.PPD, args.RecalcTic, _statusService, args.RemoveBaseLine, args.NFL, args.NormalizeByIT, args.ReferenceIT);

            if (args.RecalcTic)
                _data.RebuildTic();
        }
    }
}
