using ChromExplorer.Controls;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.Syringe.Settings;
using ChromExplorer.Syringe;
using log4net;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace ChromExplorer.Modules.Syringe.ViewModels
{
    [Export(typeof(SyringeControlViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class SyringeControlViewViewModel : IPartImportsSatisfiedNotification, INavigationAware
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ISyringeControl _syringeControl;
        private readonly IEventAggregator _eventAggregator;
        private bool _msConnected = false;
        private bool _manualStopRequested = false;
        private bool _wasRunning = false;
        private bool _msAcquisitionRequested = false;
        private readonly string _connectionString = @"Data Source=source.db3;Version=3;";

        [Import] private CompositionContainer _container;
        private DelegateCommand _applyCommand;
        private DelegateCommand _startCommand;
        private DelegateCommand _stopCommand;
        private PortSettingsEditorViewModel _portEditorContext;
        private string _sampleName;
        private List<string> _remoteMethods;
        private bool _startMs;


        [ImportingConstructor]
        public SyringeControlViewViewModel(IEventAggregator eventAggregator, ISyringeControl syringeService)
        {
            _eventAggregator = eventAggregator;
            _syringeControl = syringeService;


            _eventAggregator.GetEvent<MTE30StatusUpdatedEvent>().Subscribe(OnMTE30StatusUpdated, ThreadOption.UIThread);

            SampleName = "sample";
            SampleRunCount = 0;
            RemoteMethods = new List<string>();
            UseInjectionDuration = true;
            StartMS = true;
            AcquisitionDurationMin = 1.0;
            FillRemoteMethods();

            ApplyCommand = new DelegateCommand(OnApply);
            StartCommand = new DelegateCommand(OnStart, OnStartCanExecute);
            StopCommand = new DelegateCommand(OnStop, OnStopCanExecute);

            UpdateButtons(); // if no syringe

            _syringeControl.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ISyringeControl.IsRunning))
                {
                    var isRunning = _syringeControl.IsRunning;

                    if (_wasRunning && !isRunning)
                    {
                        try
                        {
                            var settings = SyringeModuleSettings.Default;
                            var parameter = BuildStartParameterString(settings);
                            var description = _manualStopRequested ? "Syringe Stopped (Manual)." : "Syringe Stopped (Auto).";
                            InsertLog("Syringe Control", description, null);

                            if (_msAcquisitionRequested)
                            {
                                var msDesc = _manualStopRequested ? "Data Acquisition Stopped (Manual)." : "Data Acquisition Stopped (Auto).";
                                InsertLog("MS Control", msDesc, string.Empty);
                                _eventAggregator.GetEvent<MTE30AcquisitionStopRequest>().Publish(null);
                                _msAcquisitionRequested = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error("Failed to write Syringe Stop audit log", ex);
                        }
                        finally
                        {
                            _manualStopRequested = false;
                        }
                    }

                    _wasRunning = isRunning;
                    UpdateButtons();
                }
            };



        }

        private string BuildStartParameterString(SyringeModuleSettings settings)
        {
            var durationMin = UseInjectionDuration
                ? settings.Volume / settings.SyringeRate
                : AcquisitionDurationMin;

            return string.Join("; ",
                $"SyringeDiameter={settings.SyringeDiameter}",
                $"SyringeRate={settings.SyringeRate}",
                $"Volume={settings.Volume}",
                $"StartMS={StartMS}",
                $"UseInjectionDuration={UseInjectionDuration}",
                $"AcquisitionDurationMin={AcquisitionDurationMin}",
                $"CalculatedDurationMin={durationMin}",
                $"SelectedModasMethod={SelectedModasMethod}",
                $"SampleName={SampleName}",
                $"SampleRunCount={SampleRunCount}");
        }

        private void InsertLog(string action, string description, string parameter)
        {
            int userId = 0;
            if (Application.Current?.Properties?["UserId"] != null)
                userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

            using (var con = new SQLiteConnection(_connectionString))
            {
                con.Open();

                var sql = @"INSERT INTO AuditLogM
                          (action, description, parameter, is_active, inserted_by, inserted_date)
                          VALUES
                          (@action, @desc, @param, 1, @uid, datetime('now','localtime'))";

                using (var cmd = new SQLiteCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@action", action);
                    cmd.Parameters.AddWithValue("@desc", description);
                    cmd.Parameters.AddWithValue("@param", parameter ?? string.Empty);
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void OnMTE30StatusUpdated(MTE30StatusData obj)
        {
            _msConnected = obj.Connected;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            if (StartCommand == null || StopCommand == null)
                return;

            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
        }

        private void FillRemoteMethods()
        {
            RemoteMethods.Clear();
            RemoteMethods.Add("Last");
            RemoteMethods.Add("SmallMolecules");
            RemoteMethods.Add("Peptides");
            RemoteMethods.Add("Explosives");

            SelectedModasMethod = RemoteMethods.First();
        }

        private bool OnStopCanExecute()
        {
            return _syringeControl.IsConnected && _syringeControl.IsRunning;
        }

        private void OnStop()
        {
            _manualStopRequested = true;
            _syringeControl.Stop();

            try
            {
                var settings = SyringeModuleSettings.Default;
                var parameter = BuildStartParameterString(settings);
                InsertLog("Syringe Control", "Syringe Stop.", parameter);
            }
            catch (Exception ex)
            {
                log.Error("Failed to write Syringe Stop Click audit log.", ex);
            }

            if (_msAcquisitionRequested)
            {
                _eventAggregator.GetEvent<MTE30AcquisitionStopRequest>().Publish(null);
                _msAcquisitionRequested = false;
            }
        }

        private bool OnStartCanExecute()
        {
            if (StartMS)
                return _syringeControl.IsConnected && !_syringeControl.IsRunning && _msConnected;

            return _syringeControl.IsConnected && !_syringeControl.IsRunning;
        }

        private void OnStart()
        {
            var settings = SyringeModuleSettings.Default;
            _manualStopRequested = false;
            _syringeControl.Start(settings.SyringeDiameter, settings.SyringeRate, settings.Volume);

            try
            {
                var parameter = BuildStartParameterString(settings);
                InsertLog("Syringe Control", "Control Set And Syringe Started.", parameter);
            }
            catch (Exception ex)
            {
                log.Error("Failed to write Syringe Start audit log.", ex);
            }

            if (StartMS)
            {
                var data = new MTE30AcquisitionRequestData()
                {
                    DurationMin =
                        UseInjectionDuration ? settings.Volume / settings.SyringeRate : AcquisitionDurationMin,
                    MethodHint = SelectedModasMethod,
                    SampleName = SampleName,
                    SampleRunCount = SampleRunCount
                };

                try
                {
                    var parameter = BuildStartParameterString(settings);
                    InsertLog("MS Control", "MS Started", parameter);
                    InsertLog("MS Control", "Data Acquisition Start.", data.ToString());
                }
                catch (Exception ex)
                {
                    log.Error("Failed to write MS Start audit log", ex);
                }

                log.Info($"Acquisition Request: {data}");

                _eventAggregator.GetEvent<MTE30AcquisitionRequest>().Publish(data);

                _msAcquisitionRequested = true;

                SampleRunCount++;
            }

            UpdateButtons();
            settings.Save();

        }

        private void OnApply()
        {
            PortEditorContext.UpdateConfig(SyringeModuleSettings.Default.Port);
            SyringeModuleSettings.Default.Save();

            try
            {
                var portParameter = string.Join("; ",
                    $"PortName={PortEditorContext?.Name}",
                    $"BaudRate={PortEditorContext?.BaudRate}",
                    $"DataBits={PortEditorContext?.DataBits}",
                    $"StopBits={PortEditorContext?.StopBits}",
                    $"Parity={PortEditorContext?.Parity}",
                    $"Handshake={PortEditorContext?.Handshake}",
                    $"Timeout={PortEditorContext?.Timeout}",
                    $"UseTcpIP={PortEditorContext?.UseTcpIP}",
                    $"Address={PortEditorContext?.Address}",
                    $"Port={PortEditorContext?.Port}");

                InsertLog("Syringe Port Control", "Control Set Port Settings Applied.", portParameter);
            }
            catch (Exception ex)
            {
                log.Error("Failed to write Syringe Apply audit log", ex);
            }
        }

        public DelegateCommand ApplyCommand
        {
            get => _applyCommand;
            private set => _applyCommand = value;
        }

        public DelegateCommand StartCommand
        {
            get => _startCommand;
            private set => _startCommand = value;
        }

        public DelegateCommand StopCommand
        {
            get => _stopCommand;
            private set => _stopCommand = value;
        }

        public PortSettingsEditorViewModel PortEditorContext
        {
            get => _portEditorContext;
            private set => _portEditorContext = value;
        }

        public string SampleName
        {
            get => _sampleName;
            set
            {
                if (_sampleName != value)
                {
                    _sampleName = value;
                    SampleRunCount = 0;
                }
            }
        }

        public int SampleRunCount { get; private set; }
        public double AcquisitionDurationMin { get; set; }
        public bool UseInjectionDuration { get; set; }

        public string SelectedModasMethod { get; set; }

        public bool StartMS
        {
            get => _startMs;
            set
            {
                _startMs = value;
                UpdateButtons();
            }
        }

        public List<string> RemoteMethods
        {
            get => _remoteMethods;
            private set => _remoteMethods = value;
        }

        public void OnImportsSatisfied()
        {
            PortEditorContext = new PortSettingsEditorViewModel(SyringeModuleSettings.Default.Port);
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _eventAggregator.GetEvent<MTE30StatusRequest>().Publish(null);
            log.Info("NavigateTo");
            if (!_syringeControl.IsConnected)
            {
                _syringeControl.Connect();
            }

            log.Info($"Connected: {_syringeControl.IsConnected}, isRunning {_syringeControl.IsRunning}");
        }
        private static Uri _viewerUri = new Uri("SyringeControlView", UriKind.Relative);
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            log.Info("IsNavigationTarget");

            if (navigationContext.Uri == _viewerUri)
                return true;

            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            log.Info("OnNavigatedFrom");
        }
    }
}

