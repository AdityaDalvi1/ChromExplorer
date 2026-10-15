using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChromExplorer.Assets;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.DAL;
using ChromExplorer.DataModel.Interfaces;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using Microsoft.Practices.Prism.Events;

namespace ChromExplorer.Modules.MappedFileProvider.Services
{
    [Export(typeof(IChromDataProvider))]
    [ExactTypeMetadata(typeof(MappedFileProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MappedFileProvider : IChromDataProvider
    {
        private class LoadWorkerArgs
        {
            public TimeUnits Units { get; set; }
            public bool IsInMemory { get; set; }
            public bool RecalcTic { get; set; }
        }

        private readonly IEventAggregator _eventAggregator;
        private readonly IStatusUpdateService _statusService;
        private readonly IPostProcessingService _postProcessingService;
        private readonly BackgroundWorker _loadWorker = null;
        private FileMappedSpectrumData _data;

        [ImportingConstructor]
        public MappedFileProvider(IEventAggregator eventAggregator, IStatusUpdateService statusService, IPostProcessingService postProcessingService)
        {
            _postProcessingService = postProcessingService;
            _eventAggregator = eventAggregator;
            _statusService = statusService;
            InitDescriptors();

            _loadWorker = new BackgroundWorker();

            _loadWorker.DoWork += new DoWorkEventHandler(LoadFileAsyncHandler);
            _loadWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(LoadFileAsyncCompletedHandler);
        }


        public bool IsBusy { get { return _loadWorker.IsBusy; } }

        public string ObjectKey { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tooltip { get; set; }
        public ImageSource Icon { get; set; }

        private readonly Uri _importViewUri = new Uri("MappedFileImportView", UriKind.Relative);


        public Uri ImportSettingsViewKey
        {
            get { return _importViewUri; }
        }

        public GenericChromatogram Chromatogram { get; private set; }
        public string Filename { get; private set; }

        public bool Load(string filename, bool async = true)
        {
            Filename = filename;
            var data = new FileMappedSpectrumData(filename);

            if (!data.Info.IsValid)
            {
                return false;
            }

            BindToFile(data, TimeUnits.Min, true, false);

            return true;
        }

        public void BindToFile(FileMappedSpectrumData data, TimeUnits units)
        {
            BindToFile(data, units, false, false);
        }

        private LoadWorkerArgs _runArgs = null;
        public void BindToFile(FileMappedSpectrumData data, TimeUnits units, bool isInMemory, bool recalcTic)
        {
            if(_data != null)
            {
                _data.Dispose();
            }

            if (!_loadWorker.IsBusy)
            {
                Filename = data.File.FullName;
                _data = data;
                _statusService.AppStatus = string.Format("Loading...");
                _statusService.SetWaitCursor(true);
                _runArgs = new LoadWorkerArgs {Units = units, IsInMemory = isInMemory, RecalcTic = recalcTic};
                _loadWorker.RunWorkerAsync(_runArgs);
            }

        }

        void LoadFileAsyncCompletedHandler(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Error != null)
            {
                //TODO Error processing here 
            }

            _statusService.SetReadyStatus();
            var result = e.Result as IEnumerable<ChromatogramPoint>;

            if (result != null)
            {
                this.Chromatogram = new GenericChromatogram(_data, _data, result, _statusService, _postProcessingService)
                                        {Title = _data.File.Name, Filename = _data.File.FullName, };

                _eventAggregator.GetEvent<ChromatogramChangedEvent>().Publish(this.Chromatogram);
            }

            _statusService.SetReadyStatus();
            _statusService.SetWaitCursor(false);
        }

        void LoadFileAsyncHandler(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as LoadWorkerArgs;

            if (args == null)
                return;

            e.Result = _data.ReadChromatogram(args.Units.ToString(), TimeMultipliers.GetMultyplier(args.Units));

            if (args.IsInMemory)
            {
                _data.LoadData(_statusService);

                if(args.RecalcTic)
                    _data.RebuildTic();
            }

        }

        private void InitDescriptors()
        {
            ObjectKey = "MappedFileProvider";
            Title = "Mapped File Chromatogram";
            Tooltip = "Mapped File Provider";
            Icon = new BitmapImage(AssetExtension.GetResourceUri("LargeIcons/Devices/printer.png"));
        }

    }
}
