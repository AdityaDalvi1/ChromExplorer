using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Properties;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using log4net;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Win32;

namespace ChromExplorer.Modules.MascotExporter.ViewModels
{
    [Export(typeof (MascotSettingsViewViewModel))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MascotSettingsViewViewModel : INotifyPropertyChanged
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Uri DefaultUri = new Uri("ChromatorgramView", UriKind.Relative);
        private readonly Services.MascotExporter _exporter;
        private const string Extension = "mascot";
        private readonly ChromDataProviderFactory _factory;
        private const string Filter = "Mascot Chromatogram File|*.mascot|All Files|*.*";
        private readonly IRegionManager _regionManager;
        private string _filename;
        private string _currentPath;
        private MascotExportSettings _settings;
        private bool _controlEnabled;
        private string _messageText;

        [ImportingConstructor]
        public MascotSettingsViewViewModel(IEventAggregator eventAggregator, ChromDataProviderFactory factory,
            IRegionManager regionManager)
        {
            MessageText = "Mascot Settings View";

            Settings = new MascotExportSettings();

            _factory = factory;
            _regionManager = regionManager;

            _exporter = factory.GetExporter<Services.MascotExporter>();

            if (_exporter == null)
            {
                throw new NullReferenceException("MascotExporter is null");
            }


            BrowseCommand = new DelegateCommand<object>(OnBrowseCommand, OnBrowseCommandCanExecute);
            ExportCommand = new DelegateCommand<object>(OnExportCommand, OnExportCommandCanExecute);


            var eventAggregator1 = eventAggregator;
            eventAggregator1.GetEvent<DataProviderChangedEvent>().Subscribe(OnProviderChanged, ThreadOption.UIThread);
            eventAggregator1.GetEvent<ChromatogramChangedEvent>()
                .Subscribe(OnChromatogramChanged, ThreadOption.UIThread);
            eventAggregator1.GetEvent<ChromExportCompeted>().Subscribe(OnChromatogramExported, ThreadOption.UIThread);
        }

        public DelegateCommand<object> BrowseCommand { get; private set; }
        public DelegateCommand<object> ExportCommand { get; private set; }

        public string Filename
        {
            get { return _filename; }
            set
            {
                if (value == _filename) return;
                _filename = value;
                OnPropertyChanged("Filename");
            }
        }

        public string CurrentPath
        {
            get { return _currentPath; }
            set
            {
                if (value == _currentPath) return;
                _currentPath = value;
                OnPropertyChanged("CurrentPath");
            }
        }

        public MascotExportSettings Settings
        {
            get { return _settings; }
            set
            {
                if (Equals(value, _settings)) return;
                _settings = value;
                OnPropertyChanged("Settings");
            }
        }

        public bool ControlEnabled
        {
            get { return _controlEnabled; }
            private set
            {
                if (value.Equals(_controlEnabled)) return;
                _controlEnabled = value;
                OnPropertyChanged("ControlEnabled");
            }
        }

        public string MessageText
        {
            get { return _messageText; }
            set
            {
                if (value == _messageText) return;
                _messageText = value;
                OnPropertyChanged("MessageText");
            }
        }

        private void OnChromatogramExported(object obj)
        {
            _regionManager.RequestNavigate(RegionNames.MainContentArea, DefaultUri);
        }

        private bool OnExportCommandCanExecute(object arg)
        {
            return ControlEnabled && !_exporter.IsBusy;
        }

        private void OnExportCommand(object obj)
        {
            log.Info("OnExportCommand called");

            if (string.IsNullOrEmpty(Filename))
                return;

            if (!ChromExplorer.Infrastructure.FileSaveGuard.CanSaveTo(Filename, "Mascot export file"))
                return;

            Settings.Filename = Filename;
            _exporter.Export(Settings);

            ExportCommand.RaiseCanExecuteChanged();
        }

        private bool OnBrowseCommandCanExecute(object arg)
        {
            return ControlEnabled;
        }

        private void OnBrowseCommand(object obj)
        {
            log.Info("OnBrowseCommand called");

            try
            {
                var fi = new FileInfo(Filename);
                var dlg = new SaveFileDialog
                {
                    DefaultExt = "." + Extension,
                    Filter = Filter,
                    FileName = Path.GetFileName(Filename),
                    InitialDirectory = CurrentPath
                };

                var result = ChromExplorer.Infrastructure.FileStorage.ShowSaveDialog(dlg, ChromExplorer.Infrastructure.StorageCategory.Exports, "Mascot export file");

                if (result == true)
                {
                    Filename = dlg.FileName;
                }
            }
            catch (Exception ex)
            {
                log.Warn(ex);
            }
        }

        private void OnProviderChanged(Type t)
        {
            //UpdateChrom();
            //ControlEnabled = false;
        }

        private void OnChromatogramChanged(GenericChromatogram chrom)
        {
            try
            {
                Settings.Filename = string.Empty;
                ControlEnabled = chrom != null;

                if (chrom != null && _factory.Current != null)
                {
                    var chromname = _factory.Current.Filename;

                    if (string.IsNullOrEmpty(chromname))
                        chromname = "unnamed.mtd";

                    var fi = new FileInfo(chromname);
                    var chromPath = fi.Directory;
                    var chromFileName = fi.Name;
                    var chromBaseName = Path.GetFileNameWithoutExtension(fi.Name);

                    if (chromPath != null)
                    {
                        var exportBaseName = string.Format("{0}.{1}", chromBaseName, Extension);
                        var exportFullName = Path.Combine(chromPath.FullName, exportBaseName);

                        var isValid = !string.IsNullOrEmpty(exportBaseName) &&
                                      exportBaseName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                            //&& !File.Exists(exportFullName)
                            ;


                        if (isValid)
                        {
                            Filename = exportFullName;
                            CurrentPath = chromPath.FullName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            BrowseCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}