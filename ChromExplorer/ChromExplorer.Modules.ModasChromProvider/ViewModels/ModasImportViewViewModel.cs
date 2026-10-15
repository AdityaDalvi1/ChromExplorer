using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Windows;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Properties;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.ModasReader;
using log4net;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Win32;
using Mindscape.LightSpeed.Querying;


namespace ChromExplorer.Modules.ModasChromProvider.ViewModels
{
    [Export(typeof (ModasImportViewViewModel))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ModasImportViewViewModel : INotifyPropertyChanged
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Uri DefaultUri = new Uri("ChromatorgramView", UriKind.Relative);
        private ChromFilterSettings _filterSettings;
        private ModasDataReader _reader;
        private readonly InteractionRequest<Confirmation> _confirmationFromViewModelInteractionRequest;
        private readonly InteractionRequest<Notification> _notificationFromViewModelInteractionRequest;
        private readonly Services.ModasChromProvider _provider;
        private readonly IRegionManager _regionManager;
        private readonly IUserSettingsProvider _settingsProvider;
        private string _filename;
        private int _chromPoints;
        private double _ppd;
        private double _mzMin;
        private double _mzMax;
        private double _noiseFilerLevel;
        private bool _recalcTic;
        private bool _removeBaseLine;
        private TimeUnits _timeUnits = TimeUnits.Min;
        private ChromFilters _selectedFilter;
        private bool _isFileValid;
        private bool _normalizeByIT;
        private double _referenceIT;

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;";

        [ImportingConstructor]
        public ModasImportViewViewModel(ChromDataProviderFactory factory, IEventAggregator eventAggregator,
            IRegionManager regionManager, IUserSettingsProvider settingsProvider)
        {
            _regionManager = regionManager;
            var eventAggregator1 = eventAggregator;
            _provider = factory.GetProvider<Services.ModasChromProvider>();
            _settingsProvider = settingsProvider;

            if (_provider == null)
            {
                throw new NullReferenceException("ModasChromProvider is null");
            }

            if (eventAggregator1 != null)
            {
                eventAggregator1.GetEvent<ChromatogramChangedEvent>().Subscribe(OnLoadCompleted, ThreadOption.UIThread);
            }

            if (_settingsProvider == null)
            {
                throw new NullReferenceException("IUserSettingsProvider is null");
            }

            _notificationFromViewModelInteractionRequest = new InteractionRequest<Notification>();
            _confirmationFromViewModelInteractionRequest = new InteractionRequest<Confirmation>();

            BrowseCommand = new DelegateCommand<object>(OnBrowseCommand);
            LoadCommand = new DelegateCommand<object>(OnLoadCommand, OnLoadCommandCanExcute);

            ResetToDefault();

            NoiseFilerLevel = ModasDataReader.DefaultNoiseFilterLevel;

            PropertyChanged += ModasImportViewViewModelPropertyChanged;
        }

        public bool IsFileValid
        {
            get { return _isFileValid; }
            private set
            {
                if (value == _isFileValid) return;
                _isFileValid = value;
                OnPropertyChanged(nameof(IsFileValid));
            }
        }

        public IInteractionRequest NotificationFromViewModelInteractionRequest
        {
            get { return _notificationFromViewModelInteractionRequest; }
        }

        /// <summary>
        ///     Gets the notification from view model interaction request. View binds to this property
        /// </summary>
        public IInteractionRequest ConfirmationFromViewModelInteractionRequest
        {
            get { return _confirmationFromViewModelInteractionRequest; }
        }

        public string Filename
        {
            get { return _filename; }
            private set
            {
                if (value == _filename) return;
                _filename = value;
                OnPropertyChanged("Filename");
            }
        }

        public int ChromPoints
        {
            get { return _chromPoints; }
            private set
            {
                if (value == _chromPoints) return;
                _chromPoints = value;
                OnPropertyChanged("ChromPoints");
            }
        }

        public double PPD
        {
            get { return _ppd; }
            set
            {
                if (value.Equals(_ppd)) return;
                _ppd = value;
                OnPropertyChanged("PPD");
            }
        }

        public double MzMin
        {
            get { return _mzMin; }
            set
            {
                if (value.Equals(_mzMin)) return;
                _mzMin = value;
                OnPropertyChanged("MzMin");
            }
        }

        public double MzMax
        {
            get { return _mzMax; }
            set
            {
                if (value.Equals(_mzMax)) return;
                _mzMax = value;
                OnPropertyChanged("MzMax");
            }
        }

        public double NoiseFilerLevel
        {
            get { return _noiseFilerLevel; }
            set
            {
                if (value.Equals(_noiseFilerLevel)) return;
                _noiseFilerLevel = value;
                OnPropertyChanged("NoiseFilerLevel");
            }
        }

        public bool RecalcTic
        {
            get { return _recalcTic; }
            set
            {
                if (value.Equals(_recalcTic)) return;
                _recalcTic = value;
                OnPropertyChanged("RecalcTic");
            }
        }

        public bool RemoveBaseLine
        {
            get { return _removeBaseLine; }
            set
            {
                if (value.Equals(_removeBaseLine)) return;
                _removeBaseLine = value;
                OnPropertyChanged("RemoveBaseLine");
            }
        }

        public TimeUnits TimeUnits
        {
            get { return _timeUnits; }
            set
            {
                if (value == _timeUnits) return;
                _timeUnits = value;
                OnPropertyChanged("TimeUnits");
            }
        }

        public ChromFilters SelectedFilter
        {
            get { return _selectedFilter; }
            set
            {
                if (value == _selectedFilter) return;
                _selectedFilter = value;
                OnPropertyChanged("SelectedFilter");
            }
        }

        public ChromFilterSettings FilterSettings
        {
            get { return _filterSettings; }
            set
            {
                UnregisterFilterEventHandlers(_filterSettings);
                _filterSettings = value;
                RegisterFilterEventEsttings(_filterSettings);
                OnPropertyChanged("FilterSettings");
            }
        }

        public bool NormalizeByIT
        {
            get => this._normalizeByIT;
            set
            {
                if (value.Equals(this._normalizeByIT))
                    return;
                this._normalizeByIT = value;
                this.OnPropertyChanged(nameof(NormalizeByIT));
            }
        }

        public double ReferenceIT
        {
            get => this._referenceIT;
            set
            {
                if (value.Equals(this._referenceIT))
                    return;
                this._referenceIT = value;
                this.OnPropertyChanged(nameof(ReferenceIT));
            }
        }


        public DelegateCommand<object> BrowseCommand { get; private set; }
        public DelegateCommand<object> LoadCommand { get; private set; }

        private void ModasImportViewViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedFilter")
            {
                UpdateFilterSettings();
            }
        }

        private void UpdateFilterSettings()
        {
            if (_reader == null || !_reader.IsModasFile)
            {
                return;
            }

            var fs = _reader.GetFilterSettings();
            fs.FilterType = SelectedFilter;

            FilterSettings = fs;
            UpdateFileInfo(FilterSettings.Query);
        }

        private void RegisterFilterEventEsttings(ChromFilterSettings settings)
        {
            if (settings == null)
                return;

            settings.PropertyChanged += OnFilterPropertyChanged;
        }

        private void OnFilterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateFileInfo(FilterSettings.Query);
        }

        private void UnregisterFilterEventHandlers(ChromFilterSettings settings)
        {
            if (settings == null)
                return;

            settings.PropertyChanged -= OnFilterPropertyChanged;
        }

        private bool OnLoadCommandCanExcute(object arg)
        {
            return IsFileValid && !_provider.IsBusy;
        }

        private void OnLoadCommand(object obj)
        {
            log.Debug("OnLoadCommand(object obj): called");

            if (_provider.IsBusy)
            {
                LoadCommand.RaiseCanExecuteChanged();
                return;
            }

            if (_reader != null)
            {
                _provider.BindToFile(_reader, TimeUnits, RecalcTic, MzMin, MzMax, PPD, FilterSettings.Query,
                    RemoveBaseLine, NoiseFilerLevel, NormalizeByIT, ReferenceIT);

                UpdateUserSettings();
            }

            LoadCommand.RaiseCanExecuteChanged();
            string fileName = Path.GetFileName(Filename);
            string loadedBy = Application.Current?.Properties?.Contains("Username") == true
                ? Application.Current.Properties["Username"].ToString()
                : "Unknown";
            InsertLog("File Loaded", $"File loaded by '{loadedBy}' | File: {fileName} | Path: {Filename}", fileName);
        }

        private void InsertLog(string action, string description, string fileName)
        {
            int userId = 0;
            if (Application.Current?.Properties?["UserId"] != null)
                userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

            using (var con = new SQLiteConnection(_connectionString))
            {
                con.Open();

                var sql = @"INSERT INTO AuditLogM
                          (action, description,file_name, is_active, inserted_by, inserted_date)
                          VALUES
                          (@action, @desc,@fileName, 1, @uid, datetime('now','localtime'))";

                using (var cmd = new SQLiteCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@action", action);
                    cmd.Parameters.AddWithValue("@desc", description);
                    cmd.Parameters.AddWithValue("@fileName", fileName);
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void UpdateUserSettings()
        {
            _settingsProvider.ModasImportSettings.NoiseFilterLevel = NoiseFilerLevel;
            _settingsProvider.ModasImportSettings.PPD = PPD;
            _settingsProvider.ModasImportSettings.RebuildTic = RecalcTic;
            _settingsProvider.ModasImportSettings.RemoveBaseLine = RemoveBaseLine;
            _settingsProvider.ModasImportSettings.TimeUnits = TimeUnits;
            _settingsProvider.ModasImportSettings.NormalizeByIT = NormalizeByIT;
            _settingsProvider.ModasImportSettings.ReferenceIT = ReferenceIT;

            _settingsProvider.Save();
        }

        private void OnBrowseCommand(object obj)
        {
            log.Debug("OnBrowseCommand(object obj) called ");

            var dlg = new OpenFileDialog {DefaultExt = ".mtd", Filter = "Modas Chromatogram File|*.mtd|All Files|*.*"};

            var result = dlg.ShowDialog();

            if (result == true)
            {
                Filename = dlg.FileName;

                MapFile(Filename);

                if (!IsFileValid)
                {
                    RaiseNotificationFromViewModelInteractionRequest("Error", "Unknown file format");
                    ResetToDefault();
                }

                LoadCommand.RaiseCanExecuteChanged();
            }
        }

        private void UpdateFileInfo(QueryExpression spectrumFilter)
        {
            if (_reader == null)
                return;

            try
            {
                _reader.LoadChromPoints(TimeUnits, spectrumFilter);
                _reader.UpdateStatistic();

                //PPD = _reader.PPD;

                MzMin = _reader.MassFrom;
                MzMax = _reader.MassTo;

                ChromPoints = _reader.ChromPoints.Count;
            }
            catch (Exception ex)
            {
                log.Error("UpdateFileInfo: ", ex);
            }
        }

        private void MapFile(string filename)
        {
            try
            {
                ResetToDefault();

                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null;
                }

                _reader = new ModasDataReader(filename);

                if (!_reader.IsModasFile)
                {
                    return;
                }

                if (_reader.UpdateRequired)
                {
                    _reader.UpdateFromPreviousVersions();
                }

                UpdateFileInfo(null);

                Filename = filename;
                IsFileValid = true;
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            //throw new NotImplementedException();
        }

        private void ResetToDefault()
        {
            Filename = String.Empty;
            IsFileValid = false;
            ChromPoints = 0;
            PPD = _settingsProvider.ModasImportSettings.PPD;
            NoiseFilerLevel = _settingsProvider.ModasImportSettings.NoiseFilterLevel;
            MzMax = 0;
            MzMin = 0;
            RecalcTic = _settingsProvider.ModasImportSettings.RebuildTic;
            RemoveBaseLine = _settingsProvider.ModasImportSettings.RemoveBaseLine;
            TimeUnits = _settingsProvider.ModasImportSettings.TimeUnits;
            NormalizeByIT = _settingsProvider.ModasImportSettings.NormalizeByIT;
            ReferenceIT = _settingsProvider.ModasImportSettings.ReferenceIT;

            SelectedFilter = ChromFilters.None;
            FilterSettings = new ChromFilterSettings();
        }

        private void RaiseNotificationFromViewModelInteractionRequest(string title, string message)
        {
            _notificationFromViewModelInteractionRequest.Raise(
                new Notification {Content = message, Title = title},
                dummy => { });
        }

        private void OnLoadCompleted(GenericChromatogram obj)
        {
            SyncDisplayFromLoadedChromatogram(obj);

            if (_regionManager.Regions.ContainsRegionWithName("ImportSetupRegion"))
                _regionManager.Regions.Remove("ImportSetupRegion");
            _regionManager.RequestNavigate(RegionNames.MainContentArea, DefaultUri);
        }

        // This ViewModel is a MEF-shared singleton that's only actually constructed the first time
        // the Import screen is opened. If a file auto-loads (e.g. right after Start Analysis)
        // before that ever happens, this instance doesn't exist yet and misses that load event
        // entirely — OnLoadCompleted below can't retroactively catch up. So instead of relying
        // purely on that event, the view (ModasImportView) calls this every time it's navigated
        // to, actively re-pulling whatever is currently loaded rather than waiting for one.
        public void RefreshFromCurrentChromatogram()
        {
            SyncDisplayFromLoadedChromatogram(_provider?.Chromatogram);
        }

        // Keeps this screen's File/Points/PPD/Mass-range fields in sync with whatever chromatogram
        // is actually loaded, even when the load happened through a different path than this
        // screen's own Browse+Load — e.g. a chromatogram auto-opened right after Start Analysis
        // finishes. Without this, reopening Import after such a load showed blank/default fields
        // instead of the file that's actually on screen.
        private void SyncDisplayFromLoadedChromatogram(GenericChromatogram obj)
        {
            if (obj == null)
                return;

            var loadedReader = obj.Spectrums as ModasDataReader;

            if (loadedReader != null)
            {
                _reader = loadedReader;
                MzMin = loadedReader.MassFrom;
                MzMax = loadedReader.MassTo;
            }

            Filename = obj.Filename;
            ChromPoints = obj.ChromPoints?.Count ?? 0;
            PPD = obj.PPD;
            IsFileValid = true;

            LoadCommand.RaiseCanExecuteChanged();
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