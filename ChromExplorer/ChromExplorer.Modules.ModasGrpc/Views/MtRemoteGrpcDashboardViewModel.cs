using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.Auth.Services;
using ChromExplorer.Modules.ModasChromProvider.Services;
using ChromExplorer.Modules.ModasGrpc.Models;
using ChromExplorer.Modules.ModasGrpc.Services;
using Grpc.Core;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Win32;
using Modas.Grpc.Client;
using Modas.Grpc.Client.Shared;
using Modas.Grpc.Controls;
using Modas.Grpc.Models;
using Newtonsoft.Json;
using OxyPlot;
using OxyPlot.Series;
using Serilog;

namespace ChromExplorer.Modules.ModasGrpc.Views
{
    /// <summary>Drives the top-right connection indicator. Green = Ready/Connected,
    /// Orange = Standby, Grey = Disconnected, Red = Error.</summary>
    public enum InstrumentIndicatorState
    {
        Disconnected,
        Connected,
        Standby,
        Error
    }

    [Export(typeof(MtRemoteGrpcDashboardViewModel))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class MtRemoteGrpcDashboardViewModel : INotifyPropertyChanged, IDisposable
    {
        private static readonly Brush ConnectedBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));

        private static readonly Brush StandbyBrush = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 152, 0));

        private static readonly Brush DisconnectedBrush = new SolidColorBrush(Color.FromRgb(158, 158, 158));

        private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(229, 57, 53));

        private InstrumentIndicatorState _indicatorState = InstrumentIndicatorState.Disconnected;

        private readonly Dispatcher _dispatcher;

        private const int DefaultPort = 5001;

        private const string DefaultHost = "127.0.0.1";

        private readonly string _connectionString = "Data Source=source.db3;Version=3;BusyTimeout=15000;";

        private readonly IModasRemoteClient _client;

        private int _scanCount = -1;

        private string _instrumentState = "";

        private int _scanDuration = -1;

        private string _ionSourceId;

        private string _methodFileName = "default_ms.mtm2";

        private bool _enableSave = false;

        private string _fullFilename = "C:\\Temp\\myfilename.mtd";

        private bool _sendSpectraData = true;

        private bool _sendProfileData = false;

        private readonly ConfigService _config;

        private string _selectedAnalysisType;

        private string _sampleName = "";

        private string _lastAnalysisFilename;

        private readonly ObservableCollection<SequenceItem> _sequenceItems = new ObservableCollection<SequenceItem>();

        // The sequence file currently open/saved (null = never saved: "No name yet").
        private string _currentSequenceFilePath;
        // True once grid contents differ from what was last saved/opened — an approved file that
        // has been edited since is no longer the approved one, so it must be saved under a new
        // name and re-reviewed before it can run.
        private bool _sequenceDirty;
        private bool _suppressSequenceDirty;
        private string _sequenceReviewText = "";

        public string SequenceFileDisplayName =>
            string.IsNullOrEmpty(_currentSequenceFilePath) ? "No name yet" : Path.GetFileName(_currentSequenceFilePath);

        public string SequenceFileStatusText =>
            string.IsNullOrEmpty(_currentSequenceFilePath)
                ? ""
                : (_sequenceDirty ? "Modified — save as a new file" : _sequenceReviewText);

        private System.Windows.Media.Brush _sequenceStatusBrush = System.Windows.Media.Brushes.DarkOrange;
        private string _sequenceRemarkText = "";

        // Green = Approved, red = Rejected, orange = anything still pending/modified.
        public System.Windows.Media.Brush SequenceFileStatusBrush => _sequenceDirty ? System.Windows.Media.Brushes.DarkOrange : _sequenceStatusBrush;

        // The reviewer's remark, shown in red under the file name when the file was Rejected.
        public string SequenceFileRemarkText => _sequenceRemarkText;

        private void RefreshSequenceFileInfo(bool requeryReview = true)
        {
            if (requeryReview)
            {
                _sequenceRemarkText = "";
                _sequenceStatusBrush = System.Windows.Media.Brushes.DarkOrange;
                if (string.IsNullOrEmpty(_currentSequenceFilePath))
                {
                    _sequenceReviewText = "";
                }
                else
                {
                    var state = SavedFileRegistry.GetReviewState(SavedFileRegistry.TypeSampleSequence, _currentSequenceFilePath);
                    _sequenceReviewText = !state.Found ? "Not tracked for review"
                        : state.IsApproved ? "Approved"
                        : state.IsRejected ? "Rejected — edit and save as a new file"
                        : state.IsReviewed ? "Reviewed"
                        : "Pending review";

                    if (state.IsApproved)
                        _sequenceStatusBrush = System.Windows.Media.Brushes.ForestGreen;
                    else if (state.IsRejected)
                    {
                        _sequenceStatusBrush = System.Windows.Media.Brushes.Firebrick;
                        _sequenceRemarkText = "Reviewer remark" + (string.IsNullOrWhiteSpace(state.ReviewedBy) ? "" : " (" + state.ReviewedBy + ")") + ": "
                            + (string.IsNullOrWhiteSpace(state.Remark) ? "(none)" : state.Remark);
                    }
                }
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SequenceFileDisplayName"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SequenceFileStatusText"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SequenceFileStatusBrush"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SequenceFileRemarkText"));
        }

        private void OnSequenceItemsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (SequenceItem old in e.OldItems)
                    old.PropertyChanged -= OnSequenceItemPropertyChanged;
            }
            if (e.NewItems != null)
            {
                foreach (SequenceItem added in e.NewItems)
                    added.PropertyChanged += OnSequenceItemPropertyChanged;
            }
            MarkSequenceDirty();
        }

        private void OnSequenceItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Run progress (Status / Data File) isn't an edit of the reviewed content.
            if (e.PropertyName == "Status" || e.PropertyName == "Filename")
                return;
            MarkSequenceDirty();
        }

        private void MarkSequenceDirty()
        {
            if (_suppressSequenceDirty || _sequenceDirty)
                return;
            _sequenceDirty = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SequenceFileStatusText"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SequenceFileStatusBrush"));
        }

        // A Reviewer's electronic signature is written as a block at the TOP of the CSV
        // ("ELECTRONIC SIGNATURE", key/value rows, then a blank line) before the real header row.
        // Returns how many leading lines that block takes (0 if the file isn't signed) so Open can
        // skip it and a results write-back can keep it.
        private static int GetCsvSignatureBlockLength(string[] lines)
        {
            if (lines == null || lines.Length == 0 || !string.Equals(lines[0].Trim(), "ELECTRONIC SIGNATURE", StringComparison.OrdinalIgnoreCase))
                return 0;

            int i = 1;
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
                i++;
            return Math.Min(i + 1, lines.Length);
        }

        // The same rules are checked when SAVING a sequence (so a bad one is never sent to the
        // Reviewer in the first place) and again when running it. Returns null if everything is
        // fine, otherwise the message to show; 'action' is "save" or "run".
        private static string GetSequenceValidationError(List<SequenceItem> items, string action)
        {
            if (items.Count == 0)
                return "Add at least one sample before you " + action + " the sequence.";

            // Saving is strict: every field of every row must be filled in, so nothing incomplete
            // reaches the Reviewer. Running keeps the older, looser rule for Instrument Method /
            // Data Folder (an empty one falls back to its Analysis Sample Type preset).
            bool strict = action == "save";
            var problems = new List<string>();
            for (int index = 0; index < items.Count; index++)
            {
                var i = items[index];
                var missing = new List<string>();
                if (string.IsNullOrWhiteSpace(i.SampleName))
                    missing.Add("Sample Name");
                if (string.IsNullOrWhiteSpace(i.SampleClassification) || i.SampleClassification == SequenceItem.SelectSampleTypePlaceholder)
                    missing.Add("Sample Type");
                if (string.IsNullOrWhiteSpace(i.AnalysisSampleType) || i.AnalysisSampleType == SequenceItem.SelectAnalysisSampleTypePlaceholder)
                    missing.Add("Analysis Sample Type");
                if (strict && string.IsNullOrWhiteSpace(i.InstrumentMethod))
                    missing.Add("Instrument Method");
                if (strict && string.IsNullOrWhiteSpace(i.DataFolder))
                    missing.Add("Data Folder");

                if (missing.Count > 0)
                {
                    string label = string.IsNullOrWhiteSpace(i.SampleName) ? "" : " (" + i.SampleName.Trim() + ")";
                    problems.Add("Row " + (index + 1) + label + ": " + string.Join(", ", missing) + " not selected");
                }
            }
            if (problems.Count > 0)
            {
                const int maxShown = 10;
                string list = string.Join("\n", problems.Take(maxShown));
                if (problems.Count > maxShown)
                    list += "\n… and " + (problems.Count - maxShown) + " more row(s)";
                return "Please complete every field of every sample before you " + action + " the sequence:\n\n" + list;
            }

            var duplicate = items.GroupBy(i => i.SampleName.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
                return "Sample Name '" + duplicate.Key + "' is used more than once. Each sample needs a unique name.";

            return null;
        }

        private List<string> BuildSequenceCsvLines()
        {
            List<string> lines = new List<string>();
            lines.Add(string.Join(",", SequenceCsvHeader.Select(EscapeCsvField)));
            int serialNumber = 1;
            foreach (SequenceItem sequenceItem in _sequenceItems)
            {
                lines.Add(string.Join(",", new string[8] { (serialNumber++).ToString(), sequenceItem.Status, sequenceItem.SampleName, sequenceItem.SampleClassification, sequenceItem.AnalysisSampleType, sequenceItem.InstrumentMethod, sequenceItem.DataFolder, sequenceItem.Filename }.Select(EscapeCsvField)));
            }
            return lines;
        }

        // A sequence may only run once the saved file it came from has been reviewed and
        // Approved, and hasn't been edited since.
        private bool IsSequenceApprovedForRun(out string reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(_currentSequenceFilePath))
            {
                reason = "This sequence has not been saved yet (No name yet).\n\nSave it, send it for review, and run it once a Reviewer has approved it.";
                return false;
            }
            if (_sequenceDirty)
            {
                reason = "This sequence was changed after it was saved/approved.\n\nSave it under a new file name and get that file approved before running.";
                return false;
            }
            var state = SavedFileRegistry.GetReviewState(SavedFileRegistry.TypeSampleSequence, _currentSequenceFilePath);
            RefreshSequenceFileInfo();
            if (!state.Found)
            {
                reason = "This sequence file is not tracked for review. Save it again, then have a Reviewer approve it.";
                return false;
            }
            if (state.IsRejected)
            {
                reason = "This sequence was Rejected" + (string.IsNullOrWhiteSpace(state.ReviewedBy) ? "" : " by " + state.ReviewedBy)
                    + (string.IsNullOrWhiteSpace(state.Remark) ? "" : "\n\nReviewer remark: " + state.Remark)
                    + "\n\nEdit it and save it under a new file name, then get the new file approved.";
                return false;
            }
            if (!state.IsReviewed)
            {
                reason = "This sequence is still waiting for a Reviewer's approval.\n\nIt can be run only after it has been reviewed and approved.";
                return false;
            }
            if (!state.IsApproved)
            {
                reason = "This sequence file is not approved (or has been deactivated), so it cannot be run.";
                return false;
            }
            return true;
        }

        private SequenceItem _selectedSequenceItem;

        private bool _isSequenceRunning;

        private bool _stopSequenceRequested;

        // Set true only while a single sample or sequence-item acquisition is actually running
        // (between StartMeasurements starting and returning) — OnStatusOutOfRangeDetected uses
        // this to know whether a Buffer Gas/Vacuum alert should trigger an auto-abort or just be
        // logged as informational (e.g. readings before/after a run).
        private bool _isAnalysisRunning;
        private volatile string _manualAbortReason;

        // Raises the Abort button's enabled state whenever an acquisition starts or ends.
        private bool IsAnalysisRunning
        {
            get { return _isAnalysisRunning; }
            set
            {
                _isAnalysisRunning = value;
                AbortCommand?.RaiseCanExecuteChanged();
            }
        }

        // Set by OnStatusOutOfRangeDetected when it auto-aborts a running acquisition (Buffer Gas
        // went Low, or a vacuum chamber crossed its threshold) — OnAnalysis checks this right
        // after StartMeasurements returns, since StopMeasurements() makes StartMeasurements
        // return normally rather than throwing, so this is the only way OnAnalysis can tell an
        // auto-abort apart from a genuinely completed acquisition.
        private volatile string _autoAbortReason;

        private SequenceItem _currentSequenceItem;

        private readonly ChromDataProviderFactory _chromDataProviderFactory;

        private readonly IRegionManager _regionManager;

        private static readonly string[] SequenceCsvHeader = new string[8] { "Serial Number", "Status", "Sample Name", "Sample Type", "Analysis Sample Type", "Instrument Method", "Data Folder", "Data File" };

        // App.config "SimulatorMode": "true" = simulator (instrument checks skipped); anything
        // else, or missing = real mass spectrometer (checks enforced). It is deliberately NOT
        // guessed from the instrument's serial number — a real instrument can report the same
        // placeholder serial as the simulator, which would silently switch the safety checks off.
        private static bool IsSimulatorMode =>
            string.Equals(ConfigurationManager.AppSettings["SimulatorMode"], "true", StringComparison.OrdinalIgnoreCase);

        // Called when control is granted: records which mode the app is running in.
        private void ApplyInstrumentMode(string serialNumber)
        {
            bool simulator = IsSimulatorMode;

            StatusViewViewModel.SafetyChecksEnabled = !simulator;

            try
            {
                InsertLog("Instrument Mode",
                    (simulator ? "Simulator" : "Real instrument") + " mode | Serial number: " + serialNumber + " | Instrument safety checks " + (simulator ? "OFF" : "ON"), "");
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Instrument Mode audit log");
            }
        }

        private static string Host => ConfigurationManager.AppSettings["ModasGrpcHost"] ?? "127.0.0.1";

        private static int Port
        {
            get
            {
                string s = ConfigurationManager.AppSettings["ModasGrpcPort"];
                int result;
                return int.TryParse(s, out result) ? result : 5001;
            }
        }

        public List<string> AnalysisTypes { get; private set; }

        public string SampleName
        {
            get
            {
                return _sampleName;
            }
            set
            {
                SetField(ref _sampleName, value, "SampleName");
            }
        }

        public string SelectedAnalysisType
        {
            get
            {
                return _selectedAnalysisType;
            }
            set
            {
                CommitMethodChanges(_selectedAnalysisType);
                if (SetField(ref _selectedAnalysisType, value, "SelectedAnalysisType"))
                {
                    UpdateMethod(_selectedAnalysisType);
                }
            }
        }

        public AnalysisMethod Method { get; private set; }

        public StatusViewViewModel StatusViewModel { get; }

        public RemoteMessageViewViewModel RemoteMessageVM { get; }

        public SyringeControlViewViewModel SyringeControlVM { get; }

        public IonSourceControlViewViewModel IonSourceControlVM { get; }

        public MethodOverridesViewViewModel MethodOverridesVM { get; }

        // Live state of the Control tab's sub-views, exposed as model types so consumers such as
        // the Method Report don't need a reference to Modas.Grpc.Controls. These are the exact
        // objects the Ion Source / Syringe / Overrides views are bound to.
        public IonSourceConfig LiveIonSourceConfig => IonSourceControlVM.Config;

        public SyringeConfig LiveSyringeConfig => SyringeControlVM.SyringeConfig;

        public bool IsSyringeSectionVisible => SyringeControlVM.Visibility == Visibility.Visible;

        public MethodOverrides LiveOverrides => MethodOverridesVM.Overrides;

        public GeneralSettingsViewModel GeneralVM { get; }

        public InteractionRequest<Notification> AlertRequest { get; }

        public AsyncDelegateCommand RequestControlCommand { get; }

        public AsyncDelegateCommand PingCommand { get; }

        public AsyncDelegateCommand StatusFeedCommand { get; }

        public AsyncDelegateCommand StopStatusFeedCommand { get; }

        public AsyncDelegateCommand OffCommand { get; }

        public AsyncDelegateCommand StandbyCommand { get; }

        public AsyncDelegateCommand ReadyCommand { get; }

        public AsyncDelegateCommand StartCommand { get; }

        public AsyncDelegateCommand StopCommand { get; }

        public AsyncDelegateCommand InstrumentStatusCommand { get; }

        public AsyncDelegateCommand ResetCommand { get; }

        public AsyncDelegateCommand IonSourceInfoCommand { get; }

        public AsyncDelegateCommand DebugIsCommand { get; }

        public AsyncDelegateCommand LoadMethodCommand { get; }

        public AsyncDelegateCommand InitCommand { get; }

        public AsyncDelegateCommand SaveAnalysisCommand { get; }

        public AsyncDelegateCommand AnalysisCommand { get; }

        public AsyncDelegateCommand CreateSequenceCommand { get; }

        public AsyncDelegateCommand AddSampleCommand { get; }

        public AsyncDelegateCommand RemoveSampleCommand { get; }

        public AsyncDelegateCommand OpenSequenceCommand { get; }

        public AsyncDelegateCommand SaveSequenceCommand { get; }

        public AsyncDelegateCommand RunSequenceCommand { get; }

        public AsyncDelegateCommand StopSequenceCommand { get; }

        public AsyncDelegateCommand AbortCommand { get; }

        public ObservableCollection<SequenceItem> SequenceItems => _sequenceItems;

        public IEnumerable<string> SampleTypeOptions => new string[1] { "Select Sample Type" }.Concat(SequenceItem.SampleClassifications);

        public IEnumerable<string> AnalysisSampleTypeOptions => new string[1] { "Select Analysis Sample Type" }.Concat(AnalysisTypes);

        public SequenceItem SelectedSequenceItem
        {
            get
            {
                return _selectedSequenceItem;
            }
            set
            {
                if (SetField(ref _selectedSequenceItem, value, "SelectedSequenceItem"))
                {
                    RemoveSampleCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsSequenceRunning
        {
            get
            {
                return _isSequenceRunning;
            }
            private set
            {
                if (SetField(ref _isSequenceRunning, value, "IsSequenceRunning"))
                {
                    CreateSequenceCommand.RaiseCanExecuteChanged();
                    AddSampleCommand.RaiseCanExecuteChanged();
                    RemoveSampleCommand.RaiseCanExecuteChanged();
                    OpenSequenceCommand.RaiseCanExecuteChanged();
                    SaveSequenceCommand.RaiseCanExecuteChanged();
                    RunSequenceCommand.RaiseCanExecuteChanged();
                    StopSequenceCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public int ScanCount
        {
            get
            {
                return _scanCount;
            }
            set
            {
                SetField(ref _scanCount, value, "ScanCount");
            }
        }

        public string InstrumentState
        {
            get
            {
                return _instrumentState;
            }
            set
            {
                SetField(ref _instrumentState, value, "InstrumentState");
            }
        }

        public int ScanDuration
        {
            get
            {
                return _scanDuration;
            }
            set
            {
                SetField(ref _scanDuration, value, "ScanDuration");
            }
        }

        public string IonSourceID
        {
            get
            {
                return _ionSourceId;
            }
            set
            {
                SetField(ref _ionSourceId, value, "IonSourceID");
            }
        }

        public string MethodFileName
        {
            get
            {
                return _methodFileName;
            }
            set
            {
                SetField(ref _methodFileName, value, "MethodFileName");
            }
        }

        public PlotModel SpectrumPlot { get; }

        public PlotController Controller { get; }

        public bool EnableSave
        {
            get
            {
                return _enableSave;
            }
            set
            {
                SetField(ref _enableSave, value, "EnableSave");
            }
        }

        public string FullFilename
        {
            get
            {
                return _fullFilename;
            }
            set
            {
                SetField(ref _fullFilename, value, "FullFilename");
            }
        }

        public bool SendSpectraData
        {
            get
            {
                return _sendSpectraData;
            }
            set
            {
                SetField(ref _sendSpectraData, value, "SendSpectraData");
            }
        }

        public bool SendProfileData
        {
            get
            {
                return _sendProfileData;
            }
            set
            {
                SetField(ref _sendProfileData, value, "SendProfileData");
            }
        }

        public InstrumentIndicatorState IndicatorState
        {
            get
            {
                return _indicatorState;
            }
            private set
            {
                if (SetField(ref _indicatorState, value, "IndicatorState"))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IndicatorBrush"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IndicatorLabel"));
                }
            }
        }

        public Brush IndicatorBrush
        {
            get
            {
                switch (_indicatorState)
                {
                    case InstrumentIndicatorState.Connected: return ConnectedBrush;
                    case InstrumentIndicatorState.Standby: return StandbyBrush;
                    case InstrumentIndicatorState.Error: return ErrorBrush;
                    default: return DisconnectedBrush;
                }
            }
        }

        public string IndicatorLabel
        {
            get
            {
                switch (_indicatorState)
                {
                    case InstrumentIndicatorState.Connected: return "Ready";
                    case InstrumentIndicatorState.Standby: return "Standby";
                    case InstrumentIndicatorState.Error: return "Error";
                    default: return "Disconnected";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [ImportingConstructor]
        public MtRemoteGrpcDashboardViewModel(ChromDataProviderFactory chromDataProviderFactory, IRegionManager regionManager)
        {
            _chromDataProviderFactory = chromDataProviderFactory;
            _regionManager = regionManager;
            _dispatcher = ((Application.Current != null) ? Application.Current.Dispatcher : Dispatcher.CurrentDispatcher);
            _config = new ConfigService();
            IModasRemoteClientFactory modasRemoteClientFactory = new MTRemoveServiceClientFactory();
            _client = modasRemoteClientFactory.Create(Host, Port, Modas.Grpc.Client.Shared.ChannelCredentials.Insecure);
            AppendRuntimeDiagnostics();
            _client.StatusReceived += _client_StatusReceived;
            _client.MeasurementReceived += _client_MeasurementReceived;
            StatusViewViewModel.SafetyChecksEnabled = !IsSimulatorMode;
            StatusViewModel = new StatusViewViewModel();
            StatusViewModel.OutOfRangeDetected += OnStatusOutOfRangeDetected;
            RemoteMessageVM = new RemoteMessageViewViewModel();
            SyringeControlVM = new SyringeControlViewViewModel(_client);
            SyringeControlVM.ActionCompleted += OnSyringeActionCompleted;
            IonSourceControlVM = new IonSourceControlViewViewModel(_client);
            MethodOverridesVM = new MethodOverridesViewViewModel(_client);
            GeneralVM = new GeneralSettingsViewModel();
            AnalysisTypes = new List<string>(_config.AnalysisTypes);
            SelectedAnalysisType = AnalysisTypes.FirstOrDefault();
            SpectrumPlot = new PlotModel();
            Controller = new PlotController();
            AlertRequest = new InteractionRequest<Notification>();
            RequestControlCommand = new AsyncDelegateCommand(OnRequestControl);
            PingCommand = new AsyncDelegateCommand(OnPing);
            StatusFeedCommand = new AsyncDelegateCommand(OnStatusFeed);
            StopStatusFeedCommand = new AsyncDelegateCommand(OnStopStatusFeed);
            OffCommand = new AsyncDelegateCommand(OnOff);
            StartCommand = new AsyncDelegateCommand(OnStart);
            StopCommand = new AsyncDelegateCommand(OnStop);
            StandbyCommand = new AsyncDelegateCommand(OnStandby);
            ReadyCommand = new AsyncDelegateCommand(OnReady);
            InstrumentStatusCommand = new AsyncDelegateCommand(OnInstrumentStatus);
            ResetCommand = new AsyncDelegateCommand(OnReset);
            IonSourceInfoCommand = new AsyncDelegateCommand(OnIonSourceInfo);
            DebugIsCommand = new AsyncDelegateCommand(OnDebugIs);
            LoadMethodCommand = new AsyncDelegateCommand(OnLoadMethod);
            InitCommand = new AsyncDelegateCommand(OnInit);
            SaveAnalysisCommand = new AsyncDelegateCommand(OnSave);
            AnalysisCommand = new AsyncDelegateCommand(OnAnalysis);
            CreateSequenceCommand = new AsyncDelegateCommand(OnCreateSequence, () => !_isSequenceRunning);
            AddSampleCommand = new AsyncDelegateCommand(OnAddSample, () => !_isSequenceRunning);
            RemoveSampleCommand = new AsyncDelegateCommand(OnRemoveSample, () => !_isSequenceRunning && SelectedSequenceItem != null);
            OpenSequenceCommand = new AsyncDelegateCommand(OnOpenSequence, () => !_isSequenceRunning);
            SaveSequenceCommand = new AsyncDelegateCommand(OnSaveSequence, () => !_isSequenceRunning);
            RunSequenceCommand = new AsyncDelegateCommand(OnRunSequence, () => !_isSequenceRunning && SequenceItems.Count > 0);
            StopSequenceCommand = new AsyncDelegateCommand(OnStopSequence, () => _isSequenceRunning);
            AbortCommand = new AsyncDelegateCommand(OnAbort, () => _isSequenceRunning || _isAnalysisRunning);
            _sequenceItems.CollectionChanged += OnSequenceItemsChanged;
        }

        // Set false at the top of every OnAnalysis() call, then true right before any of the
        // pre-start checks (not connected, Buffer Gas/Vacuum, Capillary timeout, duplicate
        // sample name, wrong drive) bails out with `return false`. Nothing was ever actually
        // sent to the instrument in that case — OnRunSequence uses this to leave the sequence
        // item's status as "Pending" (available to retry once the issue is fixed) instead of
        // "Abort", which is reserved for a run that genuinely started and then got cut short.
        private bool _lastRunWasPreStartBlock;

        private async Task<bool> OnAnalysis()
        {
            _lastRunWasPreStartBlock = false;

            if (Method == null)
            {
                _lastRunWasPreStartBlock = true;
                return false;
            }

            // With no instrument actually connected, none of the safety checks below mean
            // anything — there's no live Buffer Gas/Vacuum/Capillary reading at all (the Status
            // panel is simply empty), so they'd all silently pass and let "Start Analysis"/"Run
            // Sequence" go through StartMeasurements, mark every sample "Completed", and write
            // .mtd files despite nothing real ever having been acquired. Refuse outright instead.
            //
            // Deliberately NOT using IndicatorState here — MapIndicatorState only recognizes
            // Ready/Acquiring/Pumping as "Connected"; any other raw instrument state (e.g. Off,
            // Unknown — which is what an idling simulator/instrument reports) falls through to
            // its own "Disconnected" bucket by default, even while status telemetry is actively
            // streaming in. Checking for at least one live status row is a direct signal that
            // the client is actually receiving data, regardless of which raw state it's in.
            bool hasLiveStatusData = StatusViewModel?.Items != null && StatusViewModel.Items.Count > 0;
            // Part of the same simulator-mode switch (StatusViewViewModel.SafetyChecksEnabled):
            // while it is false this "instrument connected" check is skipped too.
            if (StatusViewViewModel.SafetyChecksEnabled && !hasLiveStatusData)
            {
                try
                {
                    InsertLog("Analysis Blocked - Not Connected",
                        "Analysis could not be started | Instrument is not connected (no live status data received)", "");
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Analysis Blocked - Not Connected audit log");
                }

                MessageBox.Show(
                    "Analysis cannot be started — the instrument is not connected.\n\nConnect to the instrument first, then try again.",
                    "Instrument Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);

                // A running sequence isn't going to reconnect on its own between samples — stop
                // it here instead of cycling through every remaining pending item one by one,
                // each hitting this same block.
                _stopSequenceRequested = true;
                _lastRunWasPreStartBlock = true;
                return false;
            }

            // Buffer Gas / Vacuum are fault conditions, not a "still warming up" state like
            // Capillary Temperature — if either is already bad right now, refuse to start
            // outright rather than waiting/polling for it to clear on its own.
            if (!CheckBufferGasAndVacuumBeforeStart())
            {
                _lastRunWasPreStartBlock = true;
                return false;
            }

            CommitMethodChanges(Method);
            var loadReply = await _client.LoadMethodFile(Method.ModasMethodFile);
            string methodName = Path.GetFileName(Method.ModasMethodFile ?? string.Empty);
            // The client swallows transport errors and returns a blank RemoteMessage (never
            // populated, OccuredAt == DateTime.MinValue): no reply at all means the instrument
            // could not be reached, which must stop the run in every mode.
            bool noLoadReply = loadReply == null || (string.IsNullOrEmpty(loadReply.Message) && loadReply.OccuredAt == default(DateTime));
            if (noLoadReply)
            {
                try
                {
                    InsertLog("Analysis Blocked - Instrument Not Reachable",
                        "Analysis could not be started | No reply from the instrument when loading the Instrument Method | File: " + Method.ModasMethodFile, "", methodName);
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Instrument Not Reachable audit log");
                }

                MessageBox.Show(
                    "Analysis cannot be started — there was no reply from the instrument.\n\nCheck that the instrument (or simulator) is running and connected, click Init, then try again.",
                    "Instrument Not Reachable", MessageBoxButton.OK, MessageBoxImage.Warning);

                _stopSequenceRequested = true;
                _lastRunWasPreStartBlock = true;
                return false;
            }

            if (loadReply.Severity == MessageSeverity.Error)
            {
                try
                {
                    InsertLog("Analysis Blocked - Instrument Method Not Loaded",
                        "The instrument rejected the Instrument Method | File: " + Method.ModasMethodFile + " | Reply: " + loadReply.Message, "", methodName);
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Instrument Method Not Loaded audit log");
                }

                bool controlNotGranted = (loadReply.Message ?? "").IndexOf("control", StringComparison.OrdinalIgnoreCase) >= 0;
                MessageBox.Show(
                    controlNotGranted
                        ? "The instrument did not accept the Instrument Method because ChromExplorer does not have control of the instrument yet.\n\nInstrument reply: " + loadReply.Message +
                          "\n\nConnect to the instrument first (request control), then try again."
                        : "The instrument could not load the Instrument Method:\n" + Method.ModasMethodFile + "\n\n" + loadReply.Message +
                          "\n\nCheck that the method file exists on the instrument computer (normally in its MTE30-R\\Methods\\Remote folder), then try again.",
                    controlNotGranted ? "Instrument Control Not Granted" : "Instrument Method Not Loaded", MessageBoxButton.OK, MessageBoxImage.Warning);

                _stopSequenceRequested = true;
                _lastRunWasPreStartBlock = true;
                return false;
            }
            try
            {
                InsertLog("Instrument Method Loaded", "Instrument Method sent to the instrument | File: " + Method.ModasMethodFile + " | Reply: " + (loadReply?.Message ?? ""), "", methodName);
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Instrument Method Loaded audit log");
            }
            double? capillarySetpoint = TryReadCapillarySetpoint(Method.ModasMethodFile);
            if (!await WaitForCapillaryTemperatureReady(capillarySetpoint))
            {
                _lastRunWasPreStartBlock = true;
                return false;
            }

            await ConfigureIonSource(Method.IonSourceConfig, enabled: true);
            await ConfigureMethodOverrides(Method.Overrides);
            string safeSampleType = (Method.SampleType ?? "Sample").Replace(" ", "_");
            string safeSampleName = (string.IsNullOrWhiteSpace(SampleName) ? "Sample" : SampleName.Trim().Replace(" ", "_"));
            string baseFilename = $"{safeSampleName}_{safeSampleType}_{DateTime.Now:ddMMyyyy_HHmm}";
            AcquisitionConfig config = new AcquisitionConfig
            {
                Filename = Path.Combine(Method.DataFolder, baseFilename + ".mtd"),
                SampleReference = "TODO: Sample Ref",
                SampleDescription = "TODO: Sample Desc",
                Category = "TODO Category",
                ScanCount = -1,
                ScanDurationSec = (int)Method.AcquisitionLengthSeconds,
                Type = MTSpectrumType.Centroid
            };

            if (!ChromExplorer.Infrastructure.FileSaveGuard.IsOnAllowedDrive(config.Filename, "sample analysis file"))
            {
                _lastRunWasPreStartBlock = true;
                return false;
            }

            string existingFileForSampleName = FindExistingFileForSampleName(Method.DataFolder, safeSampleName);
            if (existingFileForSampleName != null)
            {
                try
                {
                    InsertLog("Analysis Blocked - Duplicate Sample Name",
                        "Sample Name '" + SampleName + "' already has a saved file in this Data Folder: " + Method.DataFolder + " | Existing file: " + Path.GetFileName(existingFileForSampleName), "");
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Analysis Blocked - Duplicate Sample Name audit log");
                }

                MessageBox.Show("Sample Name '" + SampleName + "' already has a saved file in this Data Folder:\n" + Method.DataFolder + "\n\nExisting file: " + Path.GetFileName(existingFileForSampleName) + "\n\nUse a different Sample Name, or a different Data Folder, then try again.", "Duplicate Sample Name", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                _lastRunWasPreStartBlock = true;
                return false;
            }
            _lastAnalysisFilename = config.Filename;
            if (Method.SyringeConfig.IsVisible)
            {
                if (!await ConfigureSyringe(Method.SyringeConfig))
                {
                    try
                    {
                        InsertLog("Analysis Blocked - Syringe Not Connected",
                            "Analysis could not be started | Syringe did not start (not connected or no reply)", "");
                    }
                    catch (Exception exception)
                    {
                        Log.Logger.Error(exception, "Failed to write Analysis Blocked - Syringe audit log");
                    }

                    await ConfigureIonSource(Method.IonSourceConfig, enabled: false);
                    _stopSequenceRequested = true;
                    _lastRunWasPreStartBlock = true;
                    return false;
                }
            }
            try
            {
                InsertLog("Start Analysis", "Data Acquisition Start | Saved to: " + config.Filename, "", Path.GetFileName(config.Filename));
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Data Acquisition Start audit log");
            }
            _autoAbortReason = null;
            _manualAbortReason = null;
            StatusViewModel.CapillarySetpoint = capillarySetpoint;
            IsAnalysisRunning = true;
            try
            {
                await _client.StartMeasurements(config);
            }
            finally
            {
                IsAnalysisRunning = false;
                StatusViewModel.CapillarySetpoint = null;
            }

            // User pressed Abort (single sample or a sequence item): the acquisition was cut
            // short, so don't record/open the partial file as a finished sample.
            string manualReason = _manualAbortReason;
            _manualAbortReason = null;
            if (manualReason != null)
            {
                try
                {
                    InsertLog("Stop Analysis - Aborted by User", "Data Acquisition Aborted | Reason: " + manualReason + " | Saved to: " + config.Filename, "", Path.GetFileName(config.Filename));
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Data Acquisition Aborted by User audit log");
                }

                await ShutDownSyringeAndIonSourceSafely();

                MessageBox.Show(
                    "Acquisition stopped.\n\nSample: " + SampleName + "\nReason: " + manualReason,
                    "Acquisition Stopped", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            // StopMeasurements() (called from OnStatusOutOfRangeDetected on a Buffer
            // Gas/Vacuum safety trip) cancels the gRPC call internally and StartMeasurements
            // returns normally either way — _autoAbortReason is the only signal that this run
            // was cut short rather than genuinely completed.
            string abortReason = _autoAbortReason;
            _autoAbortReason = null;

            if (abortReason != null)
            {
                try
                {
                    InsertLog("Stop Analysis - Auto Aborted", "Data Acquisition Aborted | Reason: " + abortReason + " | Saved to: " + config.Filename, "", Path.GetFileName(config.Filename));
                }
                catch (Exception exception2)
                {
                    Log.Logger.Error(exception2, "Failed to write Data Acquisition Abort audit log");
                }

                await ShutDownSyringeAndIonSourceSafely();
                return false;
            }

            try
            {
                InsertLog("Stop Analysis", "Data Acquisition Stop | Saved to: " + config.Filename, "", Path.GetFileName(config.Filename));
            }
            catch (Exception exception2)
            {
                Log.Logger.Error(exception2, "Failed to write Data Acquisition Stop audit log");
            }
            SavedFileRegistry.Record("Sample Analysis", config.Filename);
            OpenAcquiredChromatogram(config.Filename);
            await ShutDownSyringeAndIonSourceSafely();
            return true;
        }

        // A failure while switching the syringe/ion source off after a run (especially right after
        // an abort, when the instrument may already be in a fault state) must not propagate —
        // otherwise OnAnalysis/OnRunSequence exit abnormally and the run buttons stay disabled.
        private async Task ShutDownSyringeAndIonSourceSafely()
        {
            try
            {
                if (Method.SyringeConfig.IsVisible)
                {
                    await ConfigureSyringe(Method.SyringeConfig, start: false);
                }
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Post-run: failed to stop the syringe");
            }

            try
            {
                await ConfigureIonSource(Method.IonSourceConfig, enabled: false);
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Post-run: failed to disable the ion source heater");
            }
        }

        private void RefreshRunCommandStates()
        {
            AnalysisCommand?.RaiseCanExecuteChanged();
            RunSequenceCommand?.RaiseCanExecuteChanged();
            StopSequenceCommand?.RaiseCanExecuteChanged();
            AbortCommand?.RaiseCanExecuteChanged();
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        // Refuses to start a single sample or a sequence item's acquisition if Buffer Gas is
        // already Low, or either vacuum chamber is already past its threshold, right now —
        // unlike Capillary Temperature (which naturally clears on its own within a couple of
        // minutes of warming up, so that one waits/polls), these are fault conditions that
        // won't resolve by waiting, so this refuses outright instead.
        private bool CheckBufferGasAndVacuumBeforeStart()
        {
            if (!StatusViewViewModel.SafetyChecksEnabled)
                return true;

            var items = StatusViewModel?.Items;
            if (items == null)
                return true;

            var bufferGas = items.FirstOrDefault(i => i.Name != null && i.Name.StartsWith("Buffer Gas", StringComparison.OrdinalIgnoreCase));
            if (bufferGas != null && bufferGas.IsOutOfRange)
            {
                BlockStartForUnsafeCondition("Buffer Gas", bufferGas.OutOfRangeReason);
                return false;
            }

            var vacuum1 = items.FirstOrDefault(i => i.Name != null && i.Name.StartsWith("1st Chamber Vacuum", StringComparison.OrdinalIgnoreCase));
            if (vacuum1 != null && vacuum1.IsOutOfRange)
            {
                BlockStartForUnsafeCondition("1st Chamber Vacuum", vacuum1.OutOfRangeReason);
                return false;
            }

            var vacuum2 = items.FirstOrDefault(i => i.Name != null && i.Name.StartsWith("2nd Chamber Vacuum", StringComparison.OrdinalIgnoreCase));
            if (vacuum2 != null && vacuum2.IsOutOfRange)
            {
                BlockStartForUnsafeCondition("2nd Chamber Vacuum", vacuum2.OutOfRangeReason);
                return false;
            }

            return true;
        }

        private void BlockStartForUnsafeCondition(string parameterName, string reason)
        {
            try
            {
                InsertLog("Analysis Blocked - " + parameterName,
                    "Analysis could not be started | " + parameterName + " is not in a safe state | " + reason, "");
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Analysis Blocked audit log");
            }

            MessageBox.Show(
                "Analysis cannot be started — " + parameterName + " is not in a safe state.\n\n" + reason + "\n\nResolve this before starting.",
                "Analysis Blocked", MessageBoxButton.OK, MessageBoxImage.Warning);

            // Same reasoning as the "not connected" block — a running sequence won't fix Buffer
            // Gas/Vacuum on its own between samples, so stop here instead of hitting this same
            // block again on every remaining pending item.
            _stopSequenceRequested = true;
        }

        // Gates the start of a single sample or a sequence item's acquisition on Capillary
        // Temperature having settled within its baseline window (see
        // StatusViewViewModel.ApplyZeroBaselineCheck / ZeroBaselineTolerance) — waits and polls
        // rather than just refusing to start, since the temperature reaching baseline is
        // something that naturally happens within a couple of minutes of the instrument
        // warming up. Gives up (and blocks the run) after a bounded timeout so a genuinely
        // stuck/faulty reading doesn't hang "Start Analysis" forever.
        private async Task<bool> WaitForCapillaryTemperatureReady(double? setpoint)
        {
            if (!StatusViewViewModel.SafetyChecksEnabled || !setpoint.HasValue)
                return true; // checks switched off, or method's set value unknown.

            StatusItem CapillaryItem() => StatusViewModel?.Items?
                .FirstOrDefault(i => i.Name != null && i.Name.StartsWith("Capillary Temperature", StringComparison.OrdinalIgnoreCase));

            bool InWindow(StatusItem it) =>
                it == null
                || !double.TryParse(it.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var actual)
                || StatusViewViewModel.IsWithinCapillaryWindow(setpoint.Value, actual);

            var item = CapillaryItem();
            if (InWindow(item))
                return true; // no reading yet, or already within ±3% of the method's set value.

            string waitReason = "Capillary Temperature is " + item.Value + ", needs to be within ±" + StatusViewViewModel.CapillaryTolerancePercent + "% of the method's set value " + setpoint.Value;
            // Short grace period only (a reading right at the edge usually settles in seconds);
            // a long silent wait looked like "Start Analysis does nothing", so after that the
            // user gets an immediate, explicit message instead.
            var timeout = TimeSpan.FromSeconds(15);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                await Task.Delay(1000);

                item = CapillaryItem();
                if (InWindow(item))
                    return true;
            }

            item = CapillaryItem();
            waitReason = "Capillary Temperature is " + item?.Value + ", needs to be within ±" + StatusViewViewModel.CapillaryTolerancePercent + "% of the method's set value " + setpoint.Value
                + " (" + Math.Round(setpoint.Value * (1 - StatusViewViewModel.CapillaryTolerancePercent / 100.0), 1) + " to " + Math.Round(setpoint.Value * (1 + StatusViewViewModel.CapillaryTolerancePercent / 100.0), 1) + ")";

            try
            {
                InsertLog("Analysis Blocked - Capillary Temperature", "Analysis could not be started | " + waitReason, "");
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Analysis Blocked Capillary Temperature audit log");
            }

            MessageBox.Show(
                "Analysis cannot be started — Capillary Temperature is not at the Instrument Method's set value yet.\n\n" +
                waitReason + "\n\nWait for it to settle, then start again.",
                "Capillary Temperature Not Ready", MessageBoxButton.OK, MessageBoxImage.Warning);

            // Having already waited the full timeout, this is more likely a genuine stuck
            // reading than "just needs a bit longer" — stop a running sequence here rather than
            // waiting the same 5 minutes again on every remaining pending item.
            _stopSequenceRequested = true;
            return false;
        }

        // Reads the set (operational) Capillary Temperature from the Instrument Method (.mtm2 is
        // plain XML: IonSourceChannel "Capillary Temperature (C)" -> OperationalValue). A bare file
        // name is looked up in the instrument's own Remote methods folder. Null if not found.
        private static double? TryReadCapillarySetpoint(string methodFile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(methodFile))
                    return null;

                string path = methodFile;
                if (!File.Exists(path))
                {
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MTE30-R", "Methods", "Remote", Path.GetFileName(methodFile));
                    if (!File.Exists(path))
                        return null;
                }

                var doc = System.Xml.Linq.XDocument.Load(path);
                foreach (var channel in doc.Descendants("IonSourceChannel"))
                {
                    string name = (string)channel.Element("Name");
                    if (name != null && name.StartsWith("Capillary Temperature", StringComparison.OrdinalIgnoreCase)
                        && double.TryParse((string)channel.Element("OperationalValue"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
                        && value > 0)
                    {
                        return value;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Logger.Warning(exception, "Could not read the capillary temperature set value from the Instrument Method");
            }
            return null;
        }

        private static string FindExistingFileForSampleName(string dataFolder, string safeSampleName)
        {
            if (string.IsNullOrWhiteSpace(dataFolder) || string.IsNullOrWhiteSpace(safeSampleName) || !Directory.Exists(dataFolder))
            {
                return null;
            }
            try
            {
                return Directory.EnumerateFiles(dataFolder, safeSampleName + "_*").FirstOrDefault();
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to check Data Folder '{DataFolder}' for existing files with Sample Name '{SampleName}'", dataFolder, safeSampleName);
                return null;
            }
        }

        private void OpenAcquiredChromatogram(string filename)
        {
            try
            {
                Log.Logger.Information("Auto-opening acquired chromatogram: {Filename}", filename);
                ChromExplorer.Modules.ModasChromProvider.Services.ModasChromProvider provider = _chromDataProviderFactory.GetProvider<ChromExplorer.Modules.ModasChromProvider.Services.ModasChromProvider>();
                provider.Load(filename);
                _regionManager.RequestNavigate("MainContentArea", new Uri("ChromatorgramView", UriKind.Relative));
                InsertLog("File Loaded", "File auto-loaded after analysis | File: " + Path.GetFileName(filename) + " | Path: " + filename, "", Path.GetFileName(filename));
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to auto-open acquired chromatogram: {Filename}", filename);
            }
        }

        private void OnSyringeActionCompleted(object sender, SyringeActionEventArgs e)
        {
            string verb = e.Action == MTSyringeAction.Start ? "Start" : "Stop";
            string parameter = $"Diameter={e.Diameter}; Rate={e.Rate}; Volume={e.Volume}";
            try
            {
                if (e.Success)
                    InsertLog("Syringe Control", "Syringe " + verb + " | Instrument accepted the command", parameter);
                else
                    InsertLog("Syringe Control - " + verb + " Failed", "Syringe " + verb + " failed | " + e.Message, parameter);
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Syringe Control audit log");
            }

            if (e.Success)
            {
                MessageBox.Show(
                    e.Action == MTSyringeAction.Start
                        ? "Syringe started. The instrument accepted the command."
                        : "Syringe stopped. The instrument accepted the command.",
                    "Syringe " + verb, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    "Syringe " + verb + " failed.\n\n" + e.Message,
                    "Syringe " + verb + " Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Returns false only when the syringe was supposed to be driven (Auto Start on) and the
        // instrument did not confirm it; true when it succeeded or wasn't applicable.
        private async Task<bool> ConfigureSyringe(SyringeConfig config, bool start = true)
        {
            if (!config.AutoEnable)
                return true;

            {
                config.Action = (start ? MTSyringeAction.Start : MTSyringeAction.Stop);
                var reply = await _client.SyringeControl(config);

                bool noReply = reply == null || (string.IsNullOrEmpty(reply.Message) && reply.OccuredAt == default(DateTime));
                bool failed = noReply || reply.Severity == MessageSeverity.Error || reply.Error != ErrorCode.None;
                string verb = start ? "Start" : "Stop";
                try
                {
                    InsertLog(failed ? "Syringe Control - Auto " + verb + " Failed" : "Syringe Control",
                        "Syringe auto " + verb + " with analysis | " + (failed ? (noReply ? "No reply from the instrument" : reply.Message) : "Instrument accepted the command"),
                        $"Diameter={config.Diameter}; Rate={config.Rate}; Volume={config.Volume}");
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Syringe auto start/stop audit log");
                }

                // Simulator mode: the syringe command is still sent and logged, but a failure
                // doesn't block the analysis (there is no real syringe to confirm it).
                if (failed && start && StatusViewViewModel.SafetyChecksEnabled)
                {
                    MessageBox.Show(
                        "Analysis cannot be started — the syringe is not connected or did not start.\n\n" +
                        (noReply ? "No reply from the instrument." : reply.Message) +
                        "\n\nConnect the syringe, or untick Auto Start to run without it.",
                        "Syringe Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return !failed || !StatusViewViewModel.SafetyChecksEnabled;
            }
        }

        private async Task ConfigureMethodOverrides(MethodOverrides methodOverrides)
        {
            await _client.ApplyMethodOverrides(methodOverrides);
        }

        private async Task ConfigureIonSource(IonSourceConfig methodIonSourceConfig, bool enabled)
        {
            if (methodIonSourceConfig.DsapHeaterConfig.AutoEnabled)
            {
                methodIonSourceConfig.DsapHeaterConfig.Enabled = enabled;
                await _client.UpdateDsapHeater(methodIonSourceConfig);
            }
        }

        private Task OnCreateSequence()
        {
            int count = _sequenceItems.Count;
            if (count > 0)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("This will clear the current sample sequence. Continue?", "New Sequence", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (messageBoxResult != MessageBoxResult.Yes)
                {
                    return Task.CompletedTask;
                }
            }
            _sequenceItems.Clear();
            SelectedSequenceItem = null;
            _currentSequenceFilePath = null;
            _sequenceDirty = false;
            RefreshSequenceFileInfo();
            RunSequenceCommand.RaiseCanExecuteChanged();
            try
            {
                InsertLog("Create Sequence", $"New Sample Sequence created | Previous list cleared ({count} sample(s))", "");
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Create Sequence audit log");
            }
            return Task.CompletedTask;
        }

        private Task OnAddSample()
        {
            _sequenceItems.Add(new SequenceItem
            {
                SampleName = "",
                SampleClassification = "Select Sample Type",
                AnalysisSampleType = "Select Analysis Sample Type",
                InstrumentMethod = "",
                DataFolder = "",
                Status = "Pending"
            });
            SelectedSequenceItem = _sequenceItems.LastOrDefault();
            try
            {
                InsertLog("Add Sample", "New row added to Sample Sequence", "");
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Add Sample audit log");
            }
            RunSequenceCommand.RaiseCanExecuteChanged();
            return Task.CompletedTask;
        }

        public void BrowseInstrumentMethodFor(SequenceItem item)
        {
            if (item != null)
            {
                // Same fixed D: drive methods folder as GeneralSettingsViewModel.OnBrowseMethod —
                // instrument methods should be found in one predictable place, not whatever
                // folder a file dialog last happened to be left in.
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Modas method files (*.mtm2)|*.mtm2|All files (*.*)|*.*"
                };
                if (FileStorage.ShowOpenDialog(openFileDialog, StorageCategory.InstrumentMethods, "instrument method file"))
                {
                    item.InstrumentMethod = openFileDialog.FileName;
                }
            }
        }

        public void BrowseDataFolderFor(SequenceItem item)
        {
            if (item != null)
            {
                // Same D: drive default as GeneralSettingsViewModel.OnBrowseDataFolder — data
                // folders live there, same as instrument methods and everything else tracked.
                string[] array = DataFolderOpenDialog.ShowFolder("D:\\", "Open Data Folder", "D");
                if (array != null)
                {
                    item.DataFolder = array.FirstOrDefault();
                }
            }
        }

        private Task OnRemoveSample()
        {
            RemoveSequenceItem(SelectedSequenceItem);
            return Task.CompletedTask;
        }

        public void RemoveSequenceItem(SequenceItem item)
        {
            if (item != null && !IsSequenceRunning)
            {
                string sampleName = item.SampleName;
                _sequenceItems.Remove(item);
                if (SelectedSequenceItem == item)
                {
                    SelectedSequenceItem = null;
                }
                try
                {
                    InsertLog("Remove Sample", "Sample Reference - " + sampleName + " | Removed from sequence", "");
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Remove Sample audit log");
                }
                RunSequenceCommand.RaiseCanExecuteChanged();
            }
        }

        public void FillDownSampleClassification(SequenceItem source)
        {
            FillDown(source, delegate (SequenceItem i)
            {
                i.SampleClassification = source.SampleClassification;
            }, "Sample Type");
        }

        public void FillDownAnalysisSampleType(SequenceItem source)
        {
            FillDown(source, delegate (SequenceItem i)
            {
                i.AnalysisSampleType = source.AnalysisSampleType;
            }, "Analysis Sample Type");
        }

        public void FillDownInstrumentMethod(SequenceItem source)
        {
            FillDown(source, delegate (SequenceItem i)
            {
                i.InstrumentMethod = source.InstrumentMethod;
            }, "Instrument Method");
        }

        public void FillDownDataFolder(SequenceItem source)
        {
            FillDown(source, delegate (SequenceItem i)
            {
                i.DataFolder = source.DataFolder;
            }, "Data Folder");
        }

        private void FillDown(SequenceItem source, Action<SequenceItem> apply, string fieldLabel)
        {
            if (source == null || IsSequenceRunning)
            {
                return;
            }
            int num = 0;
            foreach (SequenceItem sequenceItem in _sequenceItems)
            {
                if (sequenceItem != source && string.Equals(sequenceItem.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    apply(sequenceItem);
                    num++;
                }
            }
            if (num == 0)
            {
                return;
            }
            try
            {
                InsertLog("Fill Down", $"{fieldLabel} applied to {num} other sample(s) in the sequence", "");
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Fill Down audit log");
            }
        }

        private Task OnOpenSequence()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Sample Sequence (*.csv)|*.csv|All files (*.*)|*.*"
            };
            if (!FileStorage.ShowOpenDialog(openFileDialog, StorageCategory.SampleSequences, "sample sequence file"))
            {
                return Task.CompletedTask;
            }
            try
            {
                string[] array = File.ReadAllLines(openFileDialog.FileName);
                List<SequenceItem> list = new List<SequenceItem>();
                // An approved/rejected file starts with the Reviewer's signature block — the real
                // header row comes after it, so skip the block instead of reading it as samples.
                int headerIndex = GetCsvSignatureBlockLength(array);
                for (int i = headerIndex + 1; i < array.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(array[i]))
                    {
                        List<string> list2 = ParseCsvLine(array[i]);
                        if (list2.Count >= 7)
                        {
                            // Older saved sequences have 7 columns (no leading Serial Number);
                            // current ones have 8 — read whichever this file actually has rather
                            // than assuming the newer layout and breaking older saved files.
                            int offset = list2.Count >= 8 ? 1 : 0;

                            // The file now carries the results of any earlier run (Status / Data
                            // File), so read them back; an interrupted "Acquiring" row counts as
                            // Abort. Old files without a Status column are simply Pending.
                            string savedStatus = list2.Count >= 8 ? list2[1] : "Pending";
                            if (string.IsNullOrWhiteSpace(savedStatus))
                                savedStatus = "Pending";
                            else if (string.Equals(savedStatus, "Acquiring", StringComparison.OrdinalIgnoreCase))
                                savedStatus = "Abort";

                            list.Add(new SequenceItem
                            {
                                SampleName = list2[1 + offset],
                                SampleClassification = list2[2 + offset],
                                AnalysisSampleType = list2[3 + offset],
                                InstrumentMethod = list2[4 + offset],
                                DataFolder = list2[5 + offset],
                                Status = savedStatus,
                                Filename = list2.Count > 6 + offset ? list2[6 + offset] : ""
                            });
                        }
                    }
                }
                _suppressSequenceDirty = true;
                try
                {
                    _sequenceItems.Clear();
                    foreach (SequenceItem item in list)
                    {
                        _sequenceItems.Add(item);
                    }
                }
                finally
                {
                    _suppressSequenceDirty = false;
                }
                _currentSequenceFilePath = openFileDialog.FileName;
                _sequenceDirty = false;
                RefreshSequenceFileInfo();
                InsertLog("Open Sequence", $"Sample Sequence Loaded | File: {Path.GetFileName(openFileDialog.FileName)} | {list.Count} sample(s)", "", Path.GetFileName(openFileDialog.FileName));

                var openedState = SavedFileRegistry.GetReviewState(SavedFileRegistry.TypeSampleSequence, openFileDialog.FileName);
                if (openedState.IsRejected)
                {
                    InsertLog("Rejected Sequence Opened for Edit",
                        $"A Rejected Sample Sequence was opened | Rejected by: {openedState.ReviewedBy} | Remark: {openedState.Remark}", "", Path.GetFileName(openFileDialog.FileName));
                    SavedFileRegistry.ShowRejectedNotice(Path.GetFileName(openFileDialog.FileName), openedState.ReviewedBy, openedState.Remark,
                        "You can edit this sequence here. Use Save Sequence to save the revised sequence under a NEW file name — the original file is never overwritten.");
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to open sample sequence file: {File}", openFileDialog.FileName);
                try
                {
                    InsertLog("Open Sequence Failed", "Failed to open sample sequence file | File: " + Path.GetFileName(openFileDialog.FileName) + " | Error: " + ex.Message, "", Path.GetFileName(openFileDialog.FileName));
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Open Sequence Failed audit log");
                }
                MessageBox.Show("Failed to open sequence file:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            RunSequenceCommand.RaiseCanExecuteChanged();
            return Task.CompletedTask;
        }

        private Task OnSaveSequence()
        {
            // Check the sequence BEFORE it is saved (and so before it can reach the Reviewer) —
            // duplicate names / missing types used to show up only at Run time, after approval.
            string saveValidationError = GetSequenceValidationError(_sequenceItems.ToList(), "save");
            if (saveValidationError != null)
            {
                try
                {
                    InsertLog("Save Sequence Blocked", "Sample Sequence could not be saved | " + saveValidationError, "");
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Save Sequence Blocked audit log");
                }
                MessageBox.Show(saveValidationError, "Save Sequence", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return Task.CompletedTask;
            }

            // Saving a sequence that came from an existing file (e.g. a Rejected one being
            // revised) proposes a NEW name next to it — "<name>_rev1.csv", "_rev2", ... — never
            // the original's own name, since an already-tracked file is never overwritten.
            string suggestedFileName = null;
            string suggestedFolder = null;
            string revisedFromFile = _currentSequenceFilePath;
            if (!string.IsNullOrEmpty(revisedFromFile))
            {
                string suggestedPath = SavedFileRegistry.SuggestRevisionPath(revisedFromFile);
                suggestedFileName = Path.GetFileName(suggestedPath);
                suggestedFolder = Path.GetDirectoryName(suggestedPath);
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Sample Sequence (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = suggestedFileName ?? "",
                InitialDirectory = suggestedFolder ?? "",
                // The dialog's own built-in "X already exists — replace it?" prompt is
                // misleading here: FileSaveGuard.CanSaveTo right below never allows overwriting
                // an already-tracked file regardless of the answer, so saying "Yes" to the
                // native prompt looked like it should work but always ended in being forced to
                // pick a new name anyway. Suppressed so only our own, accurate message shows.
                OverwritePrompt = false
            };
            if (!FileStorage.ShowSaveDialog(saveFileDialog, StorageCategory.SampleSequences, "sample sequence file"))
            {
                return Task.CompletedTask;
            }
            if (!FileSaveGuard.CanSaveTo(saveFileDialog.FileName, "sample sequence file"))
            {
                return Task.CompletedTask;
            }
            try
            {
                // A revised copy is a fresh plan that has never been run under its own approval —
                // clear the previous file's run results so every row starts Pending again.
                if (!string.IsNullOrEmpty(revisedFromFile))
                {
                    foreach (SequenceItem item in _sequenceItems)
                    {
                        item.Status = "Pending";
                        item.Filename = "";
                    }
                }

                File.WriteAllLines(saveFileDialog.FileName, BuildSequenceCsvLines());
                SavedFileRegistry.Record("Sample Sequence", saveFileDialog.FileName);
                InsertLog("Save Sequence", $"Sample Sequence Saved | File: {Path.GetFileName(saveFileDialog.FileName)} | {_sequenceItems.Count} sample(s)", "", Path.GetFileName(saveFileDialog.FileName));

                bool wasRevision = !string.IsNullOrEmpty(revisedFromFile)
                    && !string.Equals(revisedFromFile, saveFileDialog.FileName, StringComparison.OrdinalIgnoreCase);
                if (wasRevision)
                {
                    var originalState = SavedFileRegistry.GetReviewState(SavedFileRegistry.TypeSampleSequence, revisedFromFile);
                    InsertLog("Save Sequence - Revised Copy",
                        $"Sample Sequence saved as a new file | Revised from: {Path.GetFileName(revisedFromFile)}{(originalState.IsRejected ? " (Rejected)" : "")} | New file: {Path.GetFileName(saveFileDialog.FileName)}",
                        "", Path.GetFileName(saveFileDialog.FileName));
                }

                _currentSequenceFilePath = saveFileDialog.FileName;
                _sequenceDirty = false;
                RefreshSequenceFileInfo();

                MessageBox.Show(
                    "Sample sequence saved successfully:\n" + saveFileDialog.FileName,
                    "Sequence Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to save sample sequence file: {File}", saveFileDialog.FileName);

                try
                {
                    InsertLog("Save Sequence Failed", "Failed to save sample sequence file | File: " + Path.GetFileName(saveFileDialog.FileName) + " | Error: " + ex.Message, "", Path.GetFileName(saveFileDialog.FileName));
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Save Sequence Failed audit log");
                }

                MessageBox.Show("Failed to save sequence file:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            return Task.CompletedTask;
        }

        private async Task OnRunSequence()
        {
            if (_sequenceItems.Count == 0 || IsSequenceRunning)
            {
                return;
            }
            if (!IsSequenceApprovedForRun(out string notApprovedReason))
            {
                try
                {
                    InsertLog("Run Sequence Blocked", "Sample Sequence could not be run | File: " + SequenceFileDisplayName + " | " + notApprovedReason.Replace("\n", " "), "",
                        string.IsNullOrEmpty(_currentSequenceFilePath) ? "" : Path.GetFileName(_currentSequenceFilePath));
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Run Sequence Blocked audit log");
                }
                MessageBox.Show(notApprovedReason, "Sequence Not Approved", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            CommitMethodChanges(_selectedAnalysisType);
            List<SequenceItem> pendingItems = _sequenceItems.Where((SequenceItem i) => string.Equals(i.Status, "Pending", StringComparison.OrdinalIgnoreCase)).ToList();
            if (pendingItems.Count == 0)
            {
                MessageBox.Show("There are no Pending samples to run. Samples that are Completed or Aborted are not run again — use Create/Open to set up a new sequence.", "Run Sequence", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string runValidationError = GetSequenceValidationError(pendingItems, "run");
            if (runValidationError != null)
            {
                MessageBox.Show(runValidationError, "Run Sequence", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            string savedAnalysisType = _selectedAnalysisType;
            string savedSampleName = SampleName;
            AnalysisMethod savedMethod = Method;
            _stopSequenceRequested = false;
            // Remember each row's status before this run, so the sequence file is updated (and the
            // summary shown) only if a sample really finished (Completed) or was aborted (Abort).
            var statusesAtStart = _sequenceItems.ToDictionary(i => i, i => i.Status);
            IsSequenceRunning = true;
            try
            {
                InsertLog("Run Sequence Started",
                    $"Sample Sequence run started | File: {SequenceFileDisplayName} (Approved) | {pendingItems.Count} sample(s) to run", "",
                    Path.GetFileName(_currentSequenceFilePath ?? ""));
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Run Sequence Started audit log");
            }
            try
            {
                foreach (SequenceItem item in _sequenceItems)
                {
                    if (_stopSequenceRequested)
                    {
                        break;
                    }
                    if (!string.Equals(item.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    int index = AnalysisTypes.IndexOf(item.AnalysisSampleType);
                    if (index < 0 || index >= _config.Methods.Count)
                    {
                        item.Status = "Abort";
                        Log.Logger.Warning("Sample Sequence: unknown Analysis Sample Type '{Type}', skipping. Sample: {SampleName}", item.AnalysisSampleType, item.SampleName);
                        continue;
                    }
                    AnalysisMethod runMethod = JsonConvert.DeserializeObject<AnalysisMethod>(JsonConvert.SerializeObject(_config.Methods[index]));
                    if (!string.IsNullOrWhiteSpace(item.InstrumentMethod))
                    {
                        runMethod.ModasMethodFile = item.InstrumentMethod;
                    }
                    if (!string.IsNullOrWhiteSpace(item.DataFolder))
                    {
                        runMethod.DataFolder = item.DataFolder;
                    }
                    item.Status = "Acquiring";
                    _currentSequenceItem = item;
                    try
                    {
                        Method = runMethod;
                        PropagateMethodChange(Method);
                        SampleName = item.SampleName;
                        bool succeeded = await OnAnalysis();
                        item.Filename = (_lastRunWasPreStartBlock || string.IsNullOrEmpty(_lastAnalysisFilename)) ? "" : Path.GetFileName(_lastAnalysisFilename);
                        if (item.Status == "Acquiring")
                        {
                            // A pre-start block (not connected, Buffer Gas/Vacuum, Capillary
                            // timeout, duplicate name, wrong drive) never actually reached the
                            // instrument — leave it "Pending" so it's still available to retry
                            // once the issue's fixed, rather than "Abort", which means an
                            // acquisition genuinely started and then got cut short.
                            item.Status = succeeded ? "Completed" : (_lastRunWasPreStartBlock ? "Pending" : "Abort");
                        }
                    }
                    catch (Exception ex)
                    {
                        Exception ex2 = ex;
                        if (item.Status == "Acquiring")
                        {
                            item.Status = "Abort";
                        }

                        // An unhandled failure mid-acquisition is exactly the kind of thing the
                        // instrument-safety auto-abort also guards against — don't let the
                        // sequence quietly move on to the next sample as if nothing happened.
                        _stopSequenceRequested = true;

                        Log.Logger.Error(ex2, "Sample Sequence: run failed for sample {SampleName}", item.SampleName);
                        try
                        {
                            InsertLog("Sequence Sample Abort", "Sample Reference - " + item.SampleName + " | Sample Type - " + item.SampleClassification + " | Error: " + ex2.Message, "");
                        }
                        catch (Exception exception)
                        {
                            Log.Logger.Error(exception, "Failed to write Sequence Sample Abort audit log");
                        }
                    }
                    finally
                    {
                        _currentSequenceItem = null;
                    }
                }
            }
            finally
            {
                Method = savedMethod;
                if (Method != null)
                {
                    PropagateMethodChange(Method);
                }
                _selectedAnalysisType = savedAnalysisType;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedAnalysisType"));
                SampleName = savedSampleName;
                IsSequenceRunning = false;
                _stopSequenceRequested = false;
                _currentSequenceItem = null;
                IsAnalysisRunning = false;
                RefreshRunCommandStates();
                FinishSequenceRun(statusesAtStart);
            }
        }

        // After a sequence run ends (finished, aborted or stopped): write each sample's final
        // Status and Data File back into the sequence file it came from, and tell the user.
        // Written directly rather than through FileSaveGuard — it's an update of this same file's
        // run results, not a new/overwriting save.
        private void FinishSequenceRun(Dictionary<SequenceItem, string> statusesAtStart)
        {
            // Only a sample that finished (Completed) or was aborted (Abort) during this run is a
            // result worth recording. If nothing got that far (every sample was blocked before it
            // started), leave the file exactly as it was and don't show a summary.
            bool anyResult = _sequenceItems.Any(i =>
                statusesAtStart.TryGetValue(i, out string before)
                && !string.Equals(before, i.Status, StringComparison.OrdinalIgnoreCase)
                && (string.Equals(i.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(i.Status, "Abort", StringComparison.OrdinalIgnoreCase)));
            if (!anyResult)
            {
                RefreshSequenceFileInfo();
                return;
            }

            int completed = _sequenceItems.Count(i => string.Equals(i.Status, "Completed", StringComparison.OrdinalIgnoreCase));
            int aborted = _sequenceItems.Count(i => string.Equals(i.Status, "Abort", StringComparison.OrdinalIgnoreCase));
            int pending = _sequenceItems.Count(i => string.Equals(i.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            bool allDone = aborted == 0 && pending == 0;

            string fileNote;
            try
            {
                // Keep the Reviewer's signature block at the top of the file — only the sample
                // rows below it (Status / Data File) are updated with the run results.
                var resultLines = new List<string>();
                string[] existing = File.Exists(_currentSequenceFilePath) ? File.ReadAllLines(_currentSequenceFilePath) : new string[0];
                int signatureLength = GetCsvSignatureBlockLength(existing);
                for (int s = 0; s < signatureLength; s++)
                    resultLines.Add(existing[s]);
                resultLines.AddRange(BuildSequenceCsvLines());

                File.WriteAllLines(_currentSequenceFilePath, resultLines);
                fileNote = "Results were saved in the sequence file:\n" + _currentSequenceFilePath;
                InsertLog(allDone ? "Sequence Run Completed" : "Sequence Run Stopped",
                    $"Sample Sequence finished | File: {Path.GetFileName(_currentSequenceFilePath)} | Completed: {completed} | Aborted: {aborted} | Pending: {pending} | Results written to the sequence file",
                    "", Path.GetFileName(_currentSequenceFilePath));
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to write sequence run results to {File}", _currentSequenceFilePath);
                fileNote = "The results could NOT be written to the sequence file:\n" + ex.Message;
                try
                {
                    InsertLog("Sequence Run - File Update Failed", "Could not write results to the sequence file | File: " + Path.GetFileName(_currentSequenceFilePath) + " | Error: " + ex.Message, "");
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Sequence File Update Failed audit log");
                }
            }

            RefreshSequenceFileInfo();

            MessageBox.Show(
                (allDone ? "The sample sequence has completed." : "The sample sequence has stopped.") +
                $"\n\nFile: {SequenceFileDisplayName}\nCompleted: {completed}\nAborted: {aborted}\nPending: {pending}\n\n{fileNote}",
                allDone ? "Sequence Completed" : "Sequence Stopped",
                MessageBoxButton.OK, allDone ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        // Abort for both modes: during a sequence it aborts the running sample and stops the
        // sequence; for a single "Start Analysis" run it cuts that acquisition short. Either way
        // it asks for a reason + the user's password, and the buttons return to normal once the
        // run unwinds (Abort disables, Start Analysis re-enables).
        private Task OnAbort()
        {
            if (IsSequenceRunning)
            {
                return OnStopSequence();
            }
            if (!_isAnalysisRunning)
            {
                return Task.CompletedTask;
            }

            string reason = ShowAbortReasonDialog();
            if (reason == null)
            {
                return Task.CompletedTask;
            }
            if (!ConfirmCurrentUserPasswordForAbort())
            {
                return Task.CompletedTask;
            }

            _manualAbortReason = reason;
            try
            {
                _client.StopMeasurements();
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to stop the in-progress measurement on Abort");
            }
            try
            {
                InsertLog("Analysis Aborted", "Analysis aborted by user | Reason: " + reason, "");
            }
            catch (Exception exception2)
            {
                Log.Logger.Error(exception2, "Failed to write Analysis Aborted audit log");
            }
            return Task.CompletedTask;
        }

        private Task OnStopSequence()
        {
            if (!IsSequenceRunning)
            {
                return Task.CompletedTask;
            }
            string text = ShowAbortReasonDialog();
            if (text == null)
            {
                return Task.CompletedTask;
            }
            if (!ConfirmCurrentUserPasswordForAbort())
            {
                return Task.CompletedTask;
            }
            _stopSequenceRequested = true;
            if (_currentSequenceItem != null)
            {
                _manualAbortReason = text;
                _currentSequenceItem.Status = "Abort";
                try
                {
                    _client.StopMeasurements();
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to stop the in-progress measurement on Abort");
                }
            }
            // Only the sample that was running is marked "Abort" — samples that never started
            // stay "Pending" so they can be run again.
            try
            {
                InsertLog("Sequence Aborted", "Sample Sequence aborted by user | Reason: " + text, "");
            }
            catch (Exception exception2)
            {
                Log.Logger.Error(exception2, "Failed to write Sequence Aborted audit log");
            }
            return Task.CompletedTask;
        }

        private bool ConfirmCurrentUserPasswordForAbort()
        {
            string text = ShowPasswordDialog();
            if (text == null)
            {
                return false;
            }
            if (!new AuthService().VerifyCurrentUserPassword(text))
            {
                try
                {
                    InsertLog("Sequence Abort - Password Verification Failed", "Incorrect password entered while confirming a sequence abort", "");
                }
                catch (Exception exception)
                {
                    Log.Logger.Error(exception, "Failed to write Sequence Abort password-failure audit log");
                }
                MessageBox.Show("Incorrect password. Abort cancelled.", "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Hand);
                return false;
            }
            return true;
        }

        private string ShowPasswordDialog()
        {
            Window dialog = new Window
            {
                Title = "Confirm Password",
                Width = 380.0,
                Height = 180.0,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };
            Grid grid = new Grid
            {
                Margin = new Thickness(20.0)
            };
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(1.0, GridUnitType.Star)
            });
            TextBlock element = new TextBlock
            {
                Text = "Enter your password to confirm this action:",
                FontWeight = System.Windows.FontWeights.SemiBold,
                Margin = new Thickness(0.0, 0.0, 0.0, 8.0),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(element, 0);
            PasswordBox txtPassword = new PasswordBox
            {
                Height = 28.0,
                BorderBrush = Brushes.LightGray,
                Padding = new Thickness(6.0)
            };
            // Plain-text twin of the PasswordBox, shown only while the eye button is pressed.
            TextBox txtPlain = new TextBox
            {
                Height = 28.0,
                BorderBrush = Brushes.LightGray,
                Padding = new Thickness(6.0),
                Visibility = Visibility.Collapsed
            };
            System.Windows.Controls.Primitives.ToggleButton btnEye = new System.Windows.Controls.Primitives.ToggleButton
            {
                Content = "👁",
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Emoji"),
                Width = 32.0,
                Height = 28.0,
                Margin = new Thickness(4.0, 0.0, 0.0, 0.0),
                ToolTip = "Show / hide password",
                Focusable = false
            };
            btnEye.Checked += delegate
            {
                txtPlain.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPlain.Visibility = Visibility.Visible;
                txtPlain.Focus();
                txtPlain.CaretIndex = txtPlain.Text.Length;
            };
            btnEye.Unchecked += delegate
            {
                txtPassword.Password = txtPlain.Text;
                txtPlain.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                txtPassword.Focus();
            };
            Func<string> readPassword = () => btnEye.IsChecked == true ? txtPlain.Text : txtPassword.Password;
            Grid passwordRow = new Grid();
            passwordRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            passwordRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(txtPassword, 0);
            Grid.SetColumn(txtPlain, 0);
            Grid.SetColumn(btnEye, 1);
            passwordRow.Children.Add(txtPassword);
            passwordRow.Children.Add(txtPlain);
            passwordRow.Children.Add(btnEye);
            Grid.SetRow(passwordRow, 1);
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
            };
            Grid.SetRow(stackPanel, 2);
            string result = null;
            Button button = new Button
            {
                Content = "OK",
                Width = 80.0,
                Height = 30.0,
                Margin = new Thickness(0.0, 0.0, 10.0, 0.0),
                Background = Brushes.SteelBlue,
                Foreground = Brushes.White
            };
            button.Click += delegate
            {
                if (string.IsNullOrEmpty(readPassword()))
                {
                    MessageBox.Show("Password cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    result = readPassword();
                    dialog.DialogResult = true;
                }
            };
            Button button2 = new Button
            {
                Content = "Cancel",
                Width = 80.0,
                Height = 30.0
            };
            button2.Click += delegate
            {
                dialog.DialogResult = false;
            };
            stackPanel.Children.Add(button);
            stackPanel.Children.Add(button2);
            grid.Children.Add(element);
            grid.Children.Add(passwordRow);
            grid.Children.Add(stackPanel);
            dialog.Content = grid;
            return (dialog.ShowDialog() == true) ? result : null;
        }

        private string ShowAbortReasonDialog()
        {
            Window dialog = new Window
            {
                Title = "Abort Sequence",
                Width = 420.0,
                Height = 220.0,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };
            Grid grid = new Grid
            {
                Margin = new Thickness(20.0)
            };
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(1.0, GridUnitType.Star)
            });
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });
            TextBlock element = new TextBlock
            {
                Text = "Enter a reason for aborting this sequence:",
                FontWeight = System.Windows.FontWeights.SemiBold,
                Margin = new Thickness(0.0, 0.0, 0.0, 8.0),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(element, 0);
            TextBox txtReason = new TextBox
            {
                Height = 60.0,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush = Brushes.LightGray,
                Padding = new Thickness(6.0)
            };
            Grid.SetRow(txtReason, 1);
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
            };
            Grid.SetRow(stackPanel, 2);
            string result = null;
            Button button = new Button
            {
                Content = "Abort",
                Width = 90.0,
                Height = 30.0,
                Margin = new Thickness(0.0, 0.0, 10.0, 0.0),
                Background = Brushes.IndianRed,
                Foreground = Brushes.White
            };
            button.Click += delegate
            {
                if (string.IsNullOrWhiteSpace(txtReason.Text))
                {
                    MessageBox.Show("Reason cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    result = txtReason.Text.Trim();
                    dialog.DialogResult = true;
                }
            };
            Button button2 = new Button
            {
                Content = "Cancel",
                Width = 90.0,
                Height = 30.0
            };
            button2.Click += delegate
            {
                dialog.DialogResult = false;
            };
            stackPanel.Children.Add(button);
            stackPanel.Children.Add(button2);
            grid.Children.Add(element);
            grid.Children.Add(txtReason);
            grid.Children.Add(stackPanel);
            dialog.Content = grid;
            return (dialog.ShowDialog() == true) ? result : null;
        }

        private Task OnSave()
        {
            _config.SaveMethods();
            try
            {
                CommitMethodChanges(_selectedAnalysisType);
                AnalysisMethod method = Method;
                if (method != null)
                {
                    string parameter = BuildControlSettingsParameterString(method);
                    InsertLog("Save", "Control Settings Saved | Sample Type - " + method.SampleType, parameter);
                }
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Control Settings Save audit log");
            }
            return Task.CompletedTask;
        }

        private string BuildControlSettingsParameterString(AnalysisMethod method)
        {
            MethodOverrides overrides = method.Overrides;
            IonSourceConfig ionSourceConfig = method.IonSourceConfig;
            SyringeConfig syringeConfig = method.SyringeConfig;
            List<string> list = new List<string>
        {
            "SampleType=" + method.SampleType,
            $"AcquisitionLengthSeconds={method.AcquisitionLengthSeconds}",
            "DataFolder=" + method.DataFolder,
            $"SprayVoltage={ionSourceConfig.SprayVoltage}",
            $"HeaterAutoEnabled={ionSourceConfig.DsapHeaterConfig.AutoEnabled}",
            $"SyringeVisible={syringeConfig.IsVisible}",
            $"SyringeDiameter={syringeConfig.Diameter}",
            $"SyringeRate={syringeConfig.Rate}",
            $"SyringeVolume={syringeConfig.Volume}",
            $"SyringeAutoEnable={syringeConfig.AutoEnable}",
            $"IonPolarity={overrides.IonPolarity.Value}",
            $"MeasurementType={overrides.MeasurementType.Value}"
        };
            switch (overrides.MeasurementType.Value)
            {
                case MTMeasurementTypes.MS2:
                    list.Add($"ParentIonMz={overrides.ParentIonMz.Value}");
                    list.Add($"FragmentationAmplitude={overrides.FragmentationAmplitude.Value}");
                    break;
                case MTMeasurementTypes.MS3:
                    list.Add($"ParentIonMz={overrides.ParentIonMz.Value}");
                    list.Add($"FragmentationAmplitude={overrides.FragmentationAmplitude.Value}");
                    list.Add($"SecondaryParentIonMz={overrides.SecondaryParentIonMz.Value}");
                    list.Add($"SecondaryFragmentationAmplitude={overrides.SecondaryFragmentationAmplitude.Value}");
                    break;
            }
            list.Add($"InjectionTimeMs={overrides.InjectionTimeMs.Value}");
            list.Add($"MzFrom={overrides.MFrom.Value}");
            list.Add($"MzTo={overrides.MTo.Value}");
            list.Add($"AgcTarget={overrides.AgcTarget.Value}");
            list.Add($"Microscans={overrides.MicroscanCount.Value}");
            return string.Join("; ", list);
        }

        private static string EscapeCsvField(string field)
        {
            field = field ?? "";
            if (field.IndexOfAny(new char[4] { ',', '"', '\n', '\r' }) < 0)
            {
                return field;
            }
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        private static List<string> ParseCsvLine(string line)
        {
            List<string> list = new List<string>();
            StringBuilder stringBuilder = new StringBuilder();
            bool flag = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (flag)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            stringBuilder.Append('"');
                            i++;
                        }
                        else
                        {
                            flag = false;
                        }
                    }
                    else
                    {
                        stringBuilder.Append(c);
                    }
                    continue;
                }
                switch (c)
                {
                    case '"':
                        flag = true;
                        break;
                    case ',':
                        list.Add(stringBuilder.ToString());
                        stringBuilder.Clear();
                        break;
                    default:
                        stringBuilder.Append(c);
                        break;
                }
            }
            list.Add(stringBuilder.ToString());
            return list;
        }

        private void InsertLog(string action, string description, string parameter, string fileName = "")
        {
            int num = 0;
            if (Application.Current?.Properties?["UserId"] != null)
            {
                num = Convert.ToInt32(Application.Current.Properties["UserId"]);
            }
            using (SQLiteConnection sQLiteConnection = new SQLiteConnection(_connectionString))
            {
                sQLiteConnection.Open();
                string commandText = "INSERT INTO AuditLogM\r\n                          (action, description, parameter, file_name, is_active, inserted_by, inserted_date)\r\n                          VALUES\r\n                          (@action, @desc, @param, @fileName, 1, @uid, datetime('now','localtime'))";
                using (SQLiteCommand sQLiteCommand = new SQLiteCommand(commandText, sQLiteConnection))
                {
                    sQLiteCommand.Parameters.AddWithValue("@action", action);
                    sQLiteCommand.Parameters.AddWithValue("@desc", description);
                    sQLiteCommand.Parameters.AddWithValue("@param", parameter ?? string.Empty);
                    sQLiteCommand.Parameters.AddWithValue("@fileName", fileName ?? string.Empty);
                    sQLiteCommand.Parameters.AddWithValue("@uid", num);
                    sQLiteCommand.ExecuteNonQuery();
                }
            }
        }

        private async Task OnInit()
        {
            RequestControlStatus result;
            try
            {
                result = await _client.RequestControl();
            }
            catch (Exception ex)
            {
                Exception ex2 = ex;
                Log.Logger.Warning(ex2, "OnInit RequestControl failed");
                IndicatorState = InstrumentIndicatorState.Error;
                throw;
            }
            if (result.IsControlGranted)
            {
                ApplyInstrumentMode(result.SerialNumber);
                string description = $"Control Connect - Server: {Host}, SN: {result.SerialNumber}, VER: {result.SoftwareVersion}, Success: {result.IsControlGranted}";
                InsertLog("Init", "Control Connected.", description);
                IndicatorState = InstrumentIndicatorState.Connected;
                await _client.UpdateStatusFeed();
            }
            else
            {
                IndicatorState = InstrumentIndicatorState.Disconnected;
            }
        }

        private void UpdateMethod(string sampleType)
        {
            int index = AnalysisTypes.IndexOf(sampleType);
            AnalysisMethod method = _config.Methods[index];
            if (_dispatcher.CheckAccess())
            {
                Method = method;
                PropagateMethodChange(Method);
                return;
            }
            _dispatcher.BeginInvoke((Action)delegate
            {
                Method = method;
                PropagateMethodChange(Method);
            });
        }

        private void PropagateMethodChange(AnalysisMethod method)
        {
            GeneralVM.Method = method;
            GeneralVM.UpdateFromMethod(method);
            SyringeControlVM.SyringeConfig = method.SyringeConfig;
            IonSourceControlVM.Config = method.IonSourceConfig;
            MethodOverridesVM.Overrides = method.Overrides;
        }

        public void CommitMethodChanges(string methodName)
        {
            if (!string.IsNullOrEmpty(methodName))
            {
                int index = AnalysisTypes.IndexOf(methodName);
                AnalysisMethod method = _config.Methods[index];
                CommitMethodChanges(method);
            }
        }

        public void CommitMethodChanges(AnalysisMethod method)
        {
            GeneralVM.CommitToMethod(method);
        }

        private void AppendRuntimeDiagnostics()
        {
            try
            {
                Assembly assembly = typeof(MTRemoteServiceClientImpl).Assembly;
                Assembly assembly2 = typeof(Channel).Assembly;
                Log.Logger.Information($"Loaded Modas.Grpc.Client: {assembly.GetName().Version} ({SafeLocation(assembly)})");
                Log.Logger.Information($"Loaded Grpc.Core: {assembly2.GetName().Version} ({SafeLocation(assembly2)})");
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Diagnostics error (assemblies)");
            }
            try
            {
                Task.Run(async delegate
                {
                    try
                    {
                        using (TcpClient tcp = new TcpClient())
                        {
                            Task connectTask = tcp.ConnectAsync(Host, Port);
                            if (await Task.WhenAny(connectTask, Task.Delay(1500)).ConfigureAwait(continueOnCapturedContext: false) != connectTask)
                            {
                                Log.Logger.Warning($"TCP check: timeout connecting to {Host}:{Port}");
                                return;
                            }
                            await connectTask.ConfigureAwait(continueOnCapturedContext: false);
                            Log.Logger.Information($"TCP check: connected to {Host}:{Port}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Exception ex2 = ex;
                        Log.Logger.Warning(ex2, $"TCP check: failed to connect to {Host}:{Port}");
                    }
                });
            }
            catch (Exception exception2)
            {
                Log.Logger.Error(exception2, "Diagnostics error (TCP)");
            }
        }

        private static string SafeLocation(Assembly asm)
        {
            try
            {
                return asm.Location;
            }
            catch
            {
                return "<unknown>";
            }
        }

        private static InstrumentIndicatorState MapIndicatorState(MTMassSpecState state)
        {
            switch (state)
            {
                case MTMassSpecState.Ready:
                case MTMassSpecState.Acquiring:
                case MTMassSpecState.Pumping:
                    return InstrumentIndicatorState.Connected;
                case MTMassSpecState.Standby:
                    return InstrumentIndicatorState.Standby;
                case MTMassSpecState.Error:
                    return InstrumentIndicatorState.Error;
                default:
                    return InstrumentIndicatorState.Disconnected;
            }
        }

        private void _client_MeasurementReceived(object sender, MeasurementsReceivedEventArgs e)
        {
            UpdatePlot(e.Dataset);
        }

        private void _client_StatusReceived(object sender, StatusReceivedEventArgs e)
        {
            IonSourceID = e.IonSourceID;
            InstrumentState = e.State.ToString();
            IndicatorState = MapIndicatorState(e.State);
            StatusViewModel.UpdateStatus(e.StatusItems);
        }

        // One audit log entry per occurrence - StatusViewViewModel only raises this on the
        // transition into the out-of-range state, not on every status poll while it stays there.
        //
        // Safety auto-abort: if Buffer Gas goes Low, or either chamber's vacuum crosses its
        // threshold, WHILE a sample/sequence-item acquisition is actually running, cancel it
        // immediately rather than let it keep acquiring on an instrument that's no longer in a
        // safe state. Capillary Temperature is deliberately excluded here — that one gates the
        // START of a run (see WaitForCapillaryTemperatureReady), it doesn't abort one already
        // running.
        private void OnStatusOutOfRangeDetected(object sender, StatusOutOfRangeEventArgs e)
        {
            try
            {
                InsertLog("Status Alert", $"{e.Name} out of range | Value: {e.Value} | {e.Reason}", $"Category={e.Category}");
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Status Alert audit log");
            }

            if (!StatusViewViewModel.SafetyChecksEnabled || !_isAnalysisRunning || e.Name == null)
                return;

            bool isBufferGas = e.Name.StartsWith("Buffer Gas", StringComparison.OrdinalIgnoreCase);
            bool isVacuum = e.Name.StartsWith("1st Chamber Vacuum", StringComparison.OrdinalIgnoreCase)
                || e.Name.StartsWith("2nd Chamber Vacuum", StringComparison.OrdinalIgnoreCase);

            bool isCapillary = e.Name.StartsWith("Capillary Temperature", StringComparison.OrdinalIgnoreCase);

            if (!isBufferGas && !isVacuum && !isCapillary)
                return;

            string reason = isBufferGas
                ? $"Buffer Gas went Low during acquisition | {e.Reason}"
                : isCapillary
                    ? $"Capillary Temperature moved outside the method's set value ±{StatusViewViewModel.CapillaryTolerancePercent}% during acquisition | {e.Reason}"
                    : $"{e.Name} crossed its safety threshold during acquisition | {e.Reason}";

            _autoAbortReason = reason;
            _stopSequenceRequested = true;

            try
            {
                _client.StopMeasurements();
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Auto-abort: StopMeasurements failed");
            }

            try
            {
                InsertLog("Analysis Auto-Aborted", reason, $"Category={e.Category}");
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, "Failed to write Analysis Auto-Aborted audit log");
            }

            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                MessageBox.Show(reason, "Analysis Aborted", MessageBoxButton.OK, MessageBoxImage.Error)));
        }

        private async Task OnLoadMethod()
        {
            HandleMessage(await _client.LoadMethodFile(MethodFileName));
        }

        private async Task OnDebugIs()
        {
            try
            {
                IonSourceConfig msg = new IonSourceConfig
                {
                    SprayVoltage = 0.0
                };
                HandleMessage(await _client.UpdateSprayVoltage(msg));
            }
            catch (Exception ex)
            {
                Exception e = ex;
                Log.Logger.Warning(e, "Exception on OnDebugIs");
                AlertRequest.Raise(new Notification
                {
                    Title = "Error",
                    Content = "Debug IS error: " + e.Message
                });
            }
        }

        private async Task OnIonSourceInfo()
        {
            string id = (IonSourceID = await _client.GetIonSourceInfo());
            AlertRequest.Raise(new Notification
            {
                Title = "Ion Source Info",
                Content = "Ion Source ID: " + id
            });
        }

        private async Task OnReset()
        {
            Log.Logger.Debug("Requesting reset");
            RemoteMessage msg = await _client.Reset();
            HandleMessage(msg);
            object arg = msg.Severity;
            InsertLog("Reset", $"Reset command sent | Result: {arg} | Status: {await GetCurrentStateSafe()}", "");
        }

        private async Task OnInstrumentStatus()
        {
            InstrumentState = (await _client.GetInstrumentState()).ToString();
            AlertRequest.Raise(new Notification
            {
                Title = "Instrument Status",
                Content = "Current State: " + InstrumentState
            });
        }

        private Task OnStop()
        {
            _client.StopMeasurements();
            return Task.CompletedTask;
        }

        private async Task OnStart()
        {
            string shortName = Guid.NewGuid().ToString() + ".mtd";
            string fullPath = Path.GetFullPath(shortName);
            FullFilename = fullPath;
            AcquisitionConfig config = new AcquisitionConfig
            {
                Filename = (EnableSave ? FullFilename : string.Empty),
                ScanCount = ScanCount,
                ScanDurationSec = ScanDuration
            };
            MTSpectrumType type = MTSpectrumType.None;
            if (SendSpectraData)
            {
                type = (SendProfileData ? MTSpectrumType.Profile : MTSpectrumType.Centroid);
            }
            config.Type = type;
            await _client.StartMeasurements(config);
        }

        private async Task OnOff()
        {
            Log.Logger.Debug("OnOff control");
            RemoteMessage msg = await _client.ToPowerOff();
            HandleMessage(msg);
            object arg = msg.Severity;
            InsertLog("Off", $"Off command sent | Result: {arg} | Status: {await GetCurrentStateSafe()}", "");
        }

        private async Task OnStandby()
        {
            Log.Logger.Debug("OnStandby control");
            RemoteMessage msg = await _client.ToStandby();
            HandleMessage(msg);
            object arg = msg.Severity;
            InsertLog("Standby", $"Standby command sent | Result: {arg} | Status: {await GetCurrentStateSafe()}", "");
        }

        private async Task OnReady()
        {
            Log.Logger.Debug("OnReady control");
            RemoteMessage msg = await _client.ToReady();
            HandleMessage(msg);
            object arg = msg.Severity;
            InsertLog("Ready", $"Ready command sent | Result: {arg} | Status: {await GetCurrentStateSafe()}", "");
        }

        private async Task<string> GetCurrentStateSafe()
        {
            try
            {
                return (await _client.GetInstrumentState()).ToString();
            }
            catch (Exception ex)
            {
                Exception ex2 = ex;
                Log.Logger.Error(ex2, "Failed to read instrument state for audit log");
                return "Unknown";
            }
        }

        private Task OnStopStatusFeed()
        {
            _client.StopStatusFeed();
            return Task.CompletedTask;
        }

        private async Task OnStatusFeed()
        {
            await _client.UpdateStatusFeed(1000);
        }

        private async Task OnPing()
        {
            try
            {
                Log.Logger.Debug("OnPing");
                string reply = await _client.Ping();
                Log.Logger.Debug("Ping request: " + reply);
                AlertRequest.Raise(new Notification
                {
                    Title = "Ping Result",
                    Content = "Ping response: " + reply
                });
            }
            catch (RpcException ex)
            {
                RpcException ex2 = ex;
                Log.Logger.Warning(ex2, "OnPing RPC error: {StatusCode} {Detail}", ex2.Status.StatusCode, ex2.Status.Detail);
                IndicatorState = InstrumentIndicatorState.Error;
                AlertRequest.Raise(new Notification
                {
                    Title = "Ping RPC Error",
                    Content = $"Ping RPC {ex2.Status.StatusCode}: {ex2.Status.Detail}"
                });
            }
            catch (Exception ex3)
            {
                Exception ex4 = ex3;
                Log.Logger.Warning(ex4, "OnPing exception");
                IndicatorState = InstrumentIndicatorState.Error;
                AlertRequest.Raise(new Notification
                {
                    Title = "Ping Error",
                    Content = "Ping error: " + ex4.Message
                });
            }
        }

        private async Task OnRequestControl()
        {
            try
            {
                RequestControlStatus result = await _client.RequestControl();
                Log.Logger.Debug($"Requesting control: SN: {result.SerialNumber}, VER {result.SoftwareVersion}, success {result.IsControlGranted}");
                if (result.IsControlGranted)
                    ApplyInstrumentMode(result.SerialNumber);
                IndicatorState = (result.IsControlGranted ? InstrumentIndicatorState.Connected : InstrumentIndicatorState.Disconnected);
                AlertRequest.Raise(new Notification
                {
                    Title = "Control Request",
                    Content = $"SN: {result.SerialNumber}\nVER: {result.SoftwareVersion}\nSuccess: {result.IsControlGranted}"
                });
            }
            catch (RpcException ex)
            {
                RpcException ex2 = ex;
                Log.Logger.Warning(ex2, "OnRequestControl RPC error: {StatusCode} {Detail}", ex2.Status.StatusCode, ex2.Status.Detail);
                IndicatorState = InstrumentIndicatorState.Error;
                AlertRequest.Raise(new Notification
                {
                    Title = "Control Request RPC Error",
                    Content = $"RequestControl RPC {ex2.Status.StatusCode}: {ex2.Status.Detail}"
                });
            }
            catch (Exception ex3)
            {
                Exception ex4 = ex3;
                Log.Logger.Warning(ex4, "OnRequestControl exception");
                IndicatorState = InstrumentIndicatorState.Error;
                AlertRequest.Raise(new Notification
                {
                    Title = "Control Request Error",
                    Content = "RequestControl error: " + ex4.Message
                });
            }
        }

        private void UpdatePlot(MTMeasurementDataSet data)
        {
            SpectrumPlot.Series.Clear();
            if (data.Header.SpectrumType == MTSpectrumType.Profile)
            {
                LineSeries lineSeries = new LineSeries
                {
                    Color = OxyColors.Blue,
                    StrokeThickness = 1.0
                };
                foreach (MTMeasurement measurement in data.Measurements)
                {
                    lineSeries.Points.Add(new DataPoint(measurement.MassChargeRatio, measurement.Abundance));
                }
                SpectrumPlot.Series.Add(lineSeries);
            }
            else
            {
                StemSeries stemSeries = new StemSeries
                {
                    Color = OxyColors.Blue,
                    StrokeThickness = 1.0
                };
                foreach (MTMeasurement measurement2 in data.Measurements)
                {
                    stemSeries.Points.Add(new DataPoint(measurement2.MassChargeRatio, measurement2.Abundance));
                }
                SpectrumPlot.Series.Add(stemSeries);
            }
            SpectrumPlot.InvalidatePlot(updateData: true);
        }

        private void HandleMessage(RemoteMessage msg)
        {
            RemoteMessageVM.Message = msg;
            AlertRequest.Raise(new Notification
            {
                Title = $"{msg.Severity} Message",
                Content = $"{msg.OccuredAt}\n{msg.Message}"
            });
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public void Dispose()
        {
            try
            {
                _client?.Dispose();
            }
            catch
            {
            }
        }
    }
}
