using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Windows;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.DAL;
using ChromExplorer.DataModel.Properties;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using log4net;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Win32;

namespace ChromExplorer.Modules.MappedFileProvider.ViewModels
{
    [Export(typeof (MappedFileImportViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class MappedFileImportViewViewModel : INotifyPropertyChanged
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Uri DefaultUri = new Uri("ChromatorgramView", UriKind.Relative);
        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";
        private bool _isFileValid;
        private FileMappedSpectrumData _spectrumData;
        private readonly InteractionRequest<Confirmation> _confirmationFromViewModelInteractionRequest;
        private readonly IEventAggregator _eventAggregator;
        private readonly InteractionRequest<Notification> _notificationFromViewModelInteractionRequest;
        public readonly Services.MappedFileProvider _provider;
        private readonly IRegionManager _regionManager;
        private string _filename;
        private int _chromPoints;
        private int _spectrumPoints;
        private double _mzMin;
        private double _mzMax;
        private bool _isInMemory;
        private bool _recalcTic;
        private TimeUnits _timeUnits;

        [ImportingConstructor]
        public MappedFileImportViewViewModel(ChromDataProviderFactory factory, IEventAggregator eventAggregator,
            IRegionManager regionManager)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _provider = factory.GetProvider<Services.MappedFileProvider>();

            if (_provider == null)
            {
                throw new NullReferenceException("MappedFileProvider is null");
            }

            if (_eventAggregator != null)
            {
                _eventAggregator.GetEvent<ChromatogramChangedEvent>().Subscribe(OnLoadCompleted, ThreadOption.UIThread);
            }

            _notificationFromViewModelInteractionRequest = new InteractionRequest<Notification>();
            _confirmationFromViewModelInteractionRequest = new InteractionRequest<Confirmation>();

            ResetToDefault();

            BrowseCommand = new DelegateCommand<object>(OnBrowseCommand);
            LoadCommand = new DelegateCommand<object>(OnLoadCommand, OnLoadCommandCanExcute);
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

        public int SpectrumPoints
        {
            get { return _spectrumPoints; }
            private set
            {
                if (value == _spectrumPoints) return;
                _spectrumPoints = value;
                OnPropertyChanged("SpectrumPoints");
            }
        }

        public double MzMin
        {
            get { return _mzMin; }
            private set
            {
                if (value.Equals(_mzMin)) return;
                _mzMin = value;
                OnPropertyChanged("MzMin");
            }
        }

        public double MzMax
        {
            get { return _mzMax; }
            private set
            {
                if (value.Equals(_mzMax)) return;
                _mzMax = value;
                OnPropertyChanged("MzMax");
            }
        }

        public bool IsInMemory
        {
            get { return _isInMemory; }
            set
            {
                if (value.Equals(_isInMemory)) return;
                _isInMemory = value;
                OnPropertyChanged("IsInMemory");
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

        public DelegateCommand<object> BrowseCommand { get; private set; }
        public DelegateCommand<object> LoadCommand { get; private set; }

        private void OnLoadCompleted(GenericChromatogram obj)
        {
            if (_regionManager.Regions.ContainsRegionWithName("ImportSetupRegion"))
                _regionManager.Regions.Remove("ImportSetupRegion");
            _regionManager.RequestNavigate(RegionNames.MainContentArea, DefaultUri);
        }

        private void OnLoadCommand(object obj)
        {
            if (_provider.IsBusy)
            {
                LoadCommand.RaiseCanExecuteChanged();
                return;
            }

            if (_spectrumData != null)
            {
                _provider.BindToFile(_spectrumData, TimeUnits, IsInMemory, RecalcTic);

                string fileName = Path.GetFileName(Filename);
                string loadedBy = Application.Current?.Properties?.Contains("Username") == true
                    ? Application.Current.Properties["Username"].ToString()
                    : "Unknown";
                InsertLog("File Loaded", $"File loaded by '{loadedBy}' | File: {fileName} | Path: {Filename}", fileName);
            }

            LoadCommand.RaiseCanExecuteChanged();
        }

        private void InsertLog(string action, string description, string fileName)
        {
            try
            {
                int userId = 0;
                if (Application.Current?.Properties?["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    var sql = @"INSERT INTO AuditLogM
                              (action, description, file_name, is_active, inserted_by, inserted_date)
                              VALUES
                              (@action, @desc, @fileName, 1, @uid, datetime('now','localtime'))";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@desc", description);
                        cmd.Parameters.AddWithValue("@fileName", fileName ?? string.Empty);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private bool OnLoadCommandCanExcute(object arg)
        {
            return _isFileValid && !_provider.IsBusy;
        }

        private void OnBrowseCommand(object obj)
        {
            ResetToDefault();

            var dlg = new OpenFileDialog {DefaultExt = ".mcd", Filter = "MassTech Chromatogram Data (.mcd)|*.mcd"};

            var result = dlg.ShowDialog();

            if (result == true)
            {
                Filename = dlg.FileName;

                MapFile(Filename);

                if (!_isFileValid)
                {
                    RaiseNotificationFromViewModelInteractionRequest("Error", "Unknown file format");
                    ResetToDefault();
                }

                LoadCommand.RaiseCanExecuteChanged();
            }
        }

        private void MapFile(string filename)
        {
            try
            {
                _spectrumData = new FileMappedSpectrumData(filename);

                _isFileValid = _spectrumData.Info.IsValid;
                ChromPoints = _spectrumData.Info.Spectrums;
                SpectrumPoints = _spectrumData.Info.Points;
                MzMin = _spectrumData.Info.MFrom;
                MzMax = MzMin + SpectrumPoints/_spectrumData.Info.PPD;
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void ResetToDefault()
        {
            Filename = String.Empty;
            _isFileValid = false;
            ChromPoints = 0;
            SpectrumPoints = 0;
            MzMax = 0;
            MzMin = 0;
            IsInMemory = true;
            TimeUnits = TimeUnits.Min;
        }

        private void RaiseNotificationFromViewModelInteractionRequest(string title, string message)
        {
            _notificationFromViewModelInteractionRequest.Raise(
                new Notification {Content = message, Title = title},
                dummy => { });
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