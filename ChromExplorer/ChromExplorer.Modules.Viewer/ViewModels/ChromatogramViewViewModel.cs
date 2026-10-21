using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Formats;
using ChromExplorer.DataModel.MethodFile;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Properties;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.ModasReader;
using ChromExplorer.Modules.ModasGrpc.Views;
using ChromExplorer.Modules.Quan.Services;
using ChromExplorer.Modules.Viewer.Services;
using ChromExplorer.NistLibrary;
using ChromExplorer.Plotter.Markers;
using ChromExplorer.Services.PickPeakerService.Services;
using log4net;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using ModasChromProviderService = ChromExplorer.Modules.ModasChromProvider.Services.ModasChromProvider;

namespace ChromExplorer.Modules.Viewer.ViewModels
{
    [Export(typeof(ChromatogramViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ChromatogramViewViewModel : IPartImportsSatisfiedNotification, INotifyPropertyChanged
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private GenericChromatogram _chromatogram;
        private readonly ChromDataProviderFactory _factory;
        private readonly PeakPickerService _peakPickerService;
        private readonly QuanService _quanService;
        private readonly RsdService _rsdService;
        private readonly IPostProcessingService _ppService;
        private readonly IEventAggregator _eventAggregator;
        private ChromatogramPlotViewModel _chromVM;
        private SpectrumPlotViewModel _spectrumVM;
        private int _minIndex;
        private int _maxIndex;
        private bool _enableChromMultiSelect;

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        [Import]
        public IRegionManager RegionManager { get; set; }

        [Import]
        public ChromExplorer.Modules.Viewer.Services.ForensicReportContext ForensicContext { get; set; }

        [Import]
        public ChromExplorer.Modules.Viewer.Services.ReportSessionService ReportSession { get; set; }

        // Only needed for "Apply Saved Method" to actually recompute SNR against the currently
        // loaded file (see ApplySNRMethod) — not required for the rest of the view model, so it's
        // an optional property import rather than a constructor dependency.
        [Import(AllowDefault = true)]
        public ISNRService SNRService { get; set; }

        [ImportingConstructor]
        public ChromatogramViewViewModel(IEventAggregator eventAggregator, ChromDataProviderFactory factory,
            PeakPickerService peakPickerService, QuanService quanService, RsdService rsdService, IPostProcessingService ppService)
        {
            log.Debug("ChromatogramViewViewModel() called");

            _peakPickerService = peakPickerService;
            _quanService = quanService;
            _rsdService = rsdService;
            _factory = factory;
            _ppService = ppService;
            _eventAggregator = eventAggregator;

            EnableChromMultiSelect = false;

            var eventAggregator1 = eventAggregator;
            eventAggregator1.GetEvent<DataProviderChangedEvent>().Subscribe(OnProviderChanged, ThreadOption.UIThread);
            eventAggregator1.GetEvent<ChromatogramChangedEvent>()
                .Subscribe(OnChromatogramChanged, ThreadOption.UIThread);
            eventAggregator1.GetEvent<PerformPeakPickingEvent>().Subscribe(OnPerformPeakPicking, ThreadOption.UIThread);
            eventAggregator1.GetEvent<PostProcessingAppliedEvent>().Subscribe(OnPostProcessingApplied,
                ThreadOption.UIThread);

            UpdateChrom();

            InitializeCommands();
        }

        private void InsertAuditLog(string action, string description, string fileName = "")
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
        public ChromatogramPlotViewModel ChromVM
        {
            get { return _chromVM; }
            private set
            {
                if (Equals(value, _chromVM)) return;
                _chromVM = value;
                OnPropertyChanged("ChromVM");
            }
        }

        public SpectrumPlotViewModel SpectrumVM
        {
            get { return _spectrumVM; }
            private set
            {
                if (Equals(value, _spectrumVM)) return;
                _spectrumVM = value;
                OnPropertyChanged("SpectrumVM");
            }
        }

        public GenericChromatogram Chromatogram
        {
            get { return _chromatogram; }
            private set
            {
                if (_chromatogram != null)
                    _chromatogram.PropertyChanged -= OnChromatogramInternalPropertyChanged;

                _chromatogram = value;

                if (_chromatogram != null)
                    _chromatogram.PropertyChanged += OnChromatogramInternalPropertyChanged;

                ChromVM = new ChromatogramPlotViewModel(_chromatogram, _quanService, _rsdService, _eventAggregator);
                SpectrumVM = new SpectrumPlotViewModel(_chromatogram, _peakPickerService);

                RebuildCompoundOptions();

                OnPropertyChanged("Chromatogram");
            }
        }

        // --- Compound (Parent Ion) selector --------------------------------------------------
        private ObservableCollection<CompoundOption> _compoundOptions;
        private CompoundOption _selectedCompoundOption;

        public ObservableCollection<CompoundOption> CompoundOptions
        {
            get { return _compoundOptions; }
            private set
            {
                if (Equals(value, _compoundOptions)) return;
                _compoundOptions = value;
                OnPropertyChanged("CompoundOptions");
                OnPropertyChanged("ShowCompoundSelector");
            }
        }

        // Always show the dropdown once a file is loaded — even a single-compound load (e.g. one
        // narrowed down via the Import "Mass Range" filter) still shows it, just with one real
        // compound option alongside "Please Select"/"All".
        public bool ShowCompoundSelector => CompoundOptions != null && CompoundOptions.Count > 0;

        // Set right before a reload-for-compound is kicked off, so the RebuildCompoundOptions()
        // triggered by that reload's ChromatogramChangedEvent restores this selection instead of
        // resetting back to the "Please Select" placeholder.
        private double? _pendingCompoundSelection;

        public CompoundOption SelectedCompoundOption
        {
            get { return _selectedCompoundOption; }
            set
            {
                if (Equals(value, _selectedCompoundOption)) return;

                // "Please Select" is a placeholder, not a real choice — ignore it if it's ever
                // re-selected (it stays disabled in the dropdown so this is mostly a safety net).
                if (value != null && !value.IsSelectable)
                    return;

                _selectedCompoundOption = value;

                ApplySelectedCompound(value);

                OnPropertyChanged("SelectedCompoundOption");
            }
        }

        // "All"/placeholder just clears the in-memory filter. A real compound that's already
        // loaded (its ParentIon is in Chromatogram.AvailableCompounds) filters in-memory, same as
        // before — instant, no DB hit. A compound that ISN'T loaded (e.g. the file was opened via
        // Import's Mass Range filter and only pulled in some compounds) triggers a fresh reload
        // scoped to just that compound, since its data was never brought into memory at all.
        private void ApplySelectedCompound(CompoundOption option)
        {
            if (Chromatogram == null)
                return;

            if (option?.ParentIon == null)
            {
                Chromatogram.SelectedCompound = null;
                return;
            }

            if (Chromatogram.AvailableCompounds != null && Chromatogram.AvailableCompounds.Contains(option.ParentIon.Value))
            {
                Chromatogram.SelectedCompound = option.ParentIon;
                return;
            }

            ReloadForCompound(option.ParentIon.Value);
        }

        private void ReloadForCompound(double parentIon)
        {
            var reader = Chromatogram?.Spectrums as ModasDataReader;
            var provider = _factory?.Current as ModasChromProviderService;

            if (reader == null || provider == null)
            {
                log.Warn("Data Channel: cannot reload for compound — provider/reader not a Modas source.");
                return;
            }

            var info = reader.GetAllCompoundInfos().FirstOrDefault(i => i.ParentIon.Equals(parentIon));

            if (info == null)
            {
                log.Warn($"Data Channel: compound {parentIon} not found in file, cannot reload.");
                return;
            }

            log.Info($"Data Channel: '{parentIon}' not already loaded — reloading file scoped to it.");

            _pendingCompoundSelection = parentIon;

            var query = reader.BuildCompoundFilter(info);
            provider.BindToFile(reader, ChromExplorer.Infrastructure.TimeUnits.Min, true, info.MFrom, info.MTo,
                reader.PPD, query, true, ModasDataReader.DefaultNoiseFilterLevel, true, 100.0);
        }

        // Default selection is the disabled "Please Select" placeholder — first load always
        // shows every scan, exactly like today. Filtering only happens once the user actively
        // picks "All" or a specific compound from the dropdown.
        //
        // The compound list itself always comes from the WHOLE file (GetAllCompoundInfos queries
        // the database directly), not just whatever subset happened to be loaded — so every
        // compound is always offered, even ones excluded by an Import-time filter.
        private void RebuildCompoundOptions()
        {
            var options = new ObservableCollection<CompoundOption>
            {
                new CompoundOption { ParentIon = null, Label = "Please Select", IsSelectable = false },
                new CompoundOption { ParentIon = null, Label = "All" }
            };

            var reader = Chromatogram?.Spectrums as ModasDataReader;
            IList<CompoundInfo> infos = reader != null ? reader.GetAllCompoundInfos() : Chromatogram?.AvailableCompoundInfos;

            if (infos != null)
            {
                foreach (var info in infos)
                {
                    options.Add(new CompoundOption
                    {
                        ParentIon = info.ParentIon,
                        Label = $"{info.ParentIon:0.#} ({info.MFrom:0.#} to {info.MTo:0.#})",
                        MFrom = info.MFrom,
                        MTo = info.MTo
                    });
                }
            }

            CompoundOptions = options;

            if (_pendingCompoundSelection.HasValue)
            {
                _selectedCompoundOption = options.FirstOrDefault(o => o.ParentIon == _pendingCompoundSelection.Value)
                                           ?? options[0];
                _pendingCompoundSelection = null;
            }
            else
            {
                _selectedCompoundOption = options[0];

                if (Chromatogram != null)
                    Chromatogram.SelectedCompound = null;
            }

            OnPropertyChanged("SelectedCompoundOption");
        }

        private void OnChromatogramInternalPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedCompound")
            {
                MinIndex = 0;
                MaxIndex = Chromatogram.ChromPoints.Count - 1;
            }
        }

        public int MinIndex
        {
            get { return _minIndex; }
            set
            {
                if (value == _minIndex) return;
                _minIndex = value;
                OnPropertyChanged("MinIndex");
            }
        }

        public int MaxIndex
        {
            get { return _maxIndex; }
            set
            {
                if (value == _maxIndex) return;
                _maxIndex = value;
                OnPropertyChanged("MaxIndex");
            }
        }

        public bool EnableChromMultiSelect
        {
            get { return _enableChromMultiSelect; }
            set
            {
                if (value.Equals(_enableChromMultiSelect)) return;
                _enableChromMultiSelect = value;
                OnPropertyChanged("EnableChromMultiSelect");
            }
        }

        public DelegateCommand<object> TestCommand { get; private set; }
        public DelegateCommand<object> IncrementSelectedCommand { get; private set; }
        public DelegateCommand<object> DecrementSelectedCommand { get; private set; }
        public DelegateCommand<object> GenerateReportCommand { get; private set; }
        public DelegateCommand<object> OpenInNistCommand { get; private set; }
        public DelegateCommand<object> ApplySavedMethodCommand { get; private set; }

        public Func<string> GetChromatogramSnapshot { get; set; }
        public Func<string> GetSpectrumSnapshot { get; set; }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            //UpdateChrom();
        }

        private void OnPostProcessingApplied(object obj)
        {
            if (_chromatogram != null)
            {
                if (obj == null || obj.ToString() == "MS")
                {
                    _chromatogram.RebuilCurrentSpectrum();
                }

                if (obj == null || obj.ToString() == "EIC")
                {
                    _chromatogram.RebuildChromatogram();
                }
            }
        }

        private void OnPerformPeakPicking(object obj)
        {
            if (SpectrumVM != null)
            {
                SpectrumVM.PerformPeakPicking();
            }
        }

        private bool HasGenerateReportAccess()
        {
            if (!Application.Current.Properties.Contains("RoleId"))
            {
                MessageBox.Show("Session expired. Please login again.");
                return false;
            }

            int roleId = Convert.ToInt32(Application.Current.Properties["RoleId"]);
            bool hasRights = false;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
SELECT COUNT(*)
FROM role_wise_rights_details r
INNER JOIN menuM m ON r.menu_id = m.menu_id
WHERE r.role_id = @role_id
AND m.menu_name = @page_name
AND r.is_active = 1
AND m.is_active = 1";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@role_id", roleId);
                    cmd.Parameters.AddWithValue("@page_name", "ChromatogramGenerateReport");

                    long count = (long)cmd.ExecuteScalar();
                    hasRights = count > 0;
                }
            }

            if (!hasRights)
            {
                MessageBox.Show("You are not authorized to generate this report.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            return hasRights;
        }

        private void OnGenerateReportExecute(object obj)
        {
            if (!HasGenerateReportAccess())
                return;

            var typeDialog = new ChromExplorer.Modules.Viewer.Views.ReportTypeSelectionDialog
            {
                QueuedFileCount = ReportSession?.Count ?? 0
            };
            if (Application.Current?.MainWindow != null && Application.Current.MainWindow != typeDialog)
                typeDialog.Owner = Application.Current.MainWindow;

            if (typeDialog.ShowDialog() != true)
                return;

            if (typeDialog.SelectedReportType == ChromExplorer.Modules.Viewer.Views.ReportType.AddToReport)
            {
                OnAddToReportExecute();
                return;
            }

            if (typeDialog.SelectedReportType == ChromExplorer.Modules.Viewer.Views.ReportType.Forensic)
            {
                OnGenerateForensicReport();
                return;
            }

            if (typeDialog.SelectedReportType == ChromExplorer.Modules.Viewer.Views.ReportType.Method)
            {
                OnGenerateMethodReport();
                return;
            }

            GenerateGeneralReport();
        }

        private void OnGenerateMethodReport()
        {
            var reader = Chromatogram?.Spectrums as ModasDataReader;
            if (reader == null)
            {
                MessageBox.Show("Load a Modas (.mtd) data file first. The Method Report is built from the acquisition settings stored in the loaded file.",
                    "Method Report", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MtdMethodInfo mtd;
            try
            {
                mtd = MtdMethodInfoReader.Read(reader);
            }
            catch (Exception ex)
            {
                log.Error("Method report: could not read method settings from the loaded file", ex);
                InsertAuditLog("Generate Method Report", "Could not read method settings from the loaded file : " + ex.Message);
                MessageBox.Show("Could not read the method settings from the loaded file:\n" + ex.Message,
                    "Method Report", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var data = BuildMethodReportData(mtd);

            var previewWindow = new ChromExplorer.Modules.Viewer.Views.MethodReportPreviewWindow(data);
            if (Application.Current?.MainWindow != null)
                previewWindow.Owner = Application.Current.MainWindow;

            bool? result = previewWindow.ShowDialog();

            if (result != true || !previewWindow.SaveRequested)
            {
                InsertAuditLog("Generate Method Report", "Method report discarded after in-app preview");
                return;
            }

            // Fixed location (D:\ChromReport\Method by default) — no Save dialog, see ReportStorage.
            string filePath = ReportStorage.BuildNewReportPath(ReportKind.Method);
            if (filePath == null)
                return;

            if (!FileSaveGuard.CanSaveTo(filePath, "method report"))
                return;

            try
            {
                new MethodPdfExporter().Export(data, filePath);

                // Not recorded in SavedFileRegistry on purpose: a Method Report is saved straight
                // to the report folder and must not appear under Review Saved Files -> Report.

                ReportStorage.ShowSavedMessage(filePath, "Method Report Saved");
                InsertAuditLog(
                    "Generate Method Report",
                    $"Method Report Generated Successfully | Saved to: {filePath}",
                    Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                InsertAuditLog("Generate Method Report", "Method Report Save Failed : " + ex.Message);
                MessageBox.Show("Error saving method report: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                log.Error("Method report save failed", ex);
            }
        }

        /// <summary>
        /// Builds the Method Report from the acquisition settings stored in the loaded .mtd file.
        /// A row (or a whole section) only appears when the file actually records that value:
        /// Ion Source only if it holds spray/heater readings, Parent Ion / Fragmentation only for
        /// MS2/MS3, the secondary rows only for MS3. Nothing is read from the live instrument tab.
        /// </summary>
        private MethodReportData BuildMethodReportData(MtdMethodInfo mtd)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            Func<double, string> num = v => v.ToString("0.######", culture);

            string currentUserName = Environment.UserName;
            if (Application.Current.Properties.Contains("Username") && Application.Current.Properties["Username"] != null)
                currentUserName = Application.Current.Properties["Username"].ToString();

            var data = new MethodReportData
            {
                GeneratedDate = DateTime.Now.ToString("f"),
                UserName = currentUserName
            };

            // Sample
            var sample = new MethodReportSection("Sample");
            if (!string.IsNullOrEmpty(mtd.FilePath)) sample.Add("File", Path.GetFileName(mtd.FilePath));
            if (!string.IsNullOrWhiteSpace(mtd.SampleName)) sample.Add("Sample Name", mtd.SampleName);
            if (!string.IsNullOrWhiteSpace(mtd.AnalysisType)) sample.Add("Analysis Type", mtd.AnalysisType);
            data.Sections.Add(sample);

            // General
            var general = new MethodReportSection("General");
            // The XML <Name> is a generic template ("Default MS"); the saved method file name is the real method used.
            string methodFileName = string.IsNullOrWhiteSpace(mtd.MethodFile) ? null : Path.GetFileNameWithoutExtension(mtd.MethodFile);
            string instrumentMethod = !string.IsNullOrWhiteSpace(methodFileName) ? methodFileName : mtd.MethodName;
            if (!string.IsNullOrWhiteSpace(instrumentMethod))
                general.Add("Instrument Method", instrumentMethod);
            if (!string.IsNullOrWhiteSpace(mtd.SerialNumber)) general.Add("Instrument Serial Number", mtd.SerialNumber);
            if (mtd.AcquiredAt.HasValue) general.Add("Acquired", mtd.AcquiredAt.Value.ToString("g"));
            if (mtd.DurationSeconds.HasValue) general.Add("Acquisition Length", mtd.DurationSeconds.Value.ToString("0.#", culture) + " seconds");
            if (mtd.ScanCount > 0) general.Add("Scans", mtd.ScanCount.ToString());
            if (!string.IsNullOrEmpty(mtd.FilePath)) general.Add("Data Folder", Path.GetDirectoryName(mtd.FilePath));
            data.Sections.Add(general);

            // Ion Source - only when the file holds a reading
            var ionSource = new MethodReportSection("Ion Source");
            if (mtd.SprayVoltage.HasValue) ionSource.Add("Spray Voltage", num(mtd.SprayVoltage.Value) + " kV");
            if (mtd.CapillaryTemperature.HasValue) ionSource.Add("Capillary Temperature", num(mtd.CapillaryTemperature.Value) + " °C");
            if (mtd.HeaterValue.HasValue) ionSource.Add("Heater Power", num(mtd.HeaterValue.Value) + " %");
            data.Sections.Add(ionSource);

            // Acquisition parameters
            bool isMs2OrMore = mtd.MsPower >= 2;
            bool isMs3 = mtd.MsPower >= 3;

            var p = new MethodReportSection("Acquisition Parameters");
            if (!string.IsNullOrWhiteSpace(mtd.Polarity)) p.Add("Ion Polarity", mtd.Polarity);
            p.Add("Measurement Type", mtd.MeasurementType);
            if (isMs2OrMore && mtd.ParentIons.Count > 0)
                p.Add("Parent Ion m/z", string.Join(", ", mtd.ParentIons.Select(num)));
            if (isMs2OrMore && mtd.FragmentationAmplitude.HasValue)
                p.Add("Fragmentation Amplitude", num(mtd.FragmentationAmplitude.Value));
            if (isMs3 && mtd.SecondaryParentIon.HasValue)
                p.Add("Secondary Parent Ion m/z", num(mtd.SecondaryParentIon.Value));
            if (isMs3 && mtd.SecondaryFragmentationAmplitude.HasValue)
                p.Add("Secondary Fragmentation Amplitude", num(mtd.SecondaryFragmentationAmplitude.Value));
            if (mtd.InjectionTimeMinMs.HasValue)
            {
                bool varies = mtd.InjectionTimeMaxMs.HasValue && Math.Abs(mtd.InjectionTimeMaxMs.Value - mtd.InjectionTimeMinMs.Value) > 1e-9;
                p.Add("Injection Time (ms)", varies
                    ? num(mtd.InjectionTimeMinMs.Value) + " - " + num(mtd.InjectionTimeMaxMs.Value)
                    : num(mtd.InjectionTimeMinMs.Value));
            }
            if (mtd.MassFrom.HasValue) p.Add("m/z From", num(mtd.MassFrom.Value));
            if (mtd.MassTo.HasValue) p.Add("m/z To", num(mtd.MassTo.Value));
            if (!string.IsNullOrWhiteSpace(mtd.AgcMode)) p.Add("AGC Mode", mtd.AgcMode);
            if (mtd.Microscans.HasValue) p.Add("Microscan Count", mtd.Microscans.Value.ToString());
            data.Sections.Add(p);

            return data;
        }

        private void OnAddToReportExecute()
        {
            if (Chromatogram == null)
            {
                MessageBox.Show("Load and process a file before adding it to the report.",
                    "Nothing to Add", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var data = BuildReportData();
                ReportSession?.Add(data);

                InsertAuditLog(
                    "Add to Report",
                    $"File added to pending multi-file report | Total queued: {ReportSession?.Count ?? 0}",
                    data.FileName);

                MessageBox.Show(
                    $"'{data.FileName}' added to report.\n\nTotal files queued: {ReportSession?.Count ?? 0}",
                    "Added to Report", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                InsertAuditLog("Add to Report", "Failed to add file to report: " + ex.Message);
                MessageBox.Show("Error adding file to report: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                log.Error("Add to report failed", ex);
            }
        }

        private void OnGenerateForensicReport()
        {
            if (ForensicContext != null)
            {
                ForensicContext.ChromatogramImage = DecodeDataUrl(GetChromatogramSnapshot?.Invoke());
                ForensicContext.SpectrumImage = DecodeDataUrl(GetSpectrumSnapshot?.Invoke());
            }

            if (RegionManager == null)
                return;

            RegionManager.RequestNavigate(
                ChromExplorer.Infrastructure.RegionNames.OverContentRegion,
                new Uri("ForensicReport", UriKind.Relative));
        }

        private void GenerateGeneralReport()
        {
            // The report is built ONLY from files explicitly queued via "Add to Report" — never
            // from whatever file happens to be open in the viewer at the moment. With nothing
            // queued, the preview is intentionally blank (see ReportPreviewWindow/BuildEmpty)
            // rather than silently substituting the current unsaved file's data.
            //
            // The preview window operates on ReportSession directly (not a snapshot copy) so the
            // per-file "✕" remove button there persists immediately.
            //
            // Save is immediate — no pre-save reviewer gate. Like every other tracked file type
            // (Method File, Data Processing Method, Sample Sequence), the PDF is written straight
            // away and then shows up in the "Review Saved Files" tab for a reviewer to approve or
            // reject after the fact.
            var previewWindow = new ChromExplorer.Modules.Viewer.Views.ReportPreviewWindow(ReportSession);
            if (Application.Current?.MainWindow != null)
                previewWindow.Owner = Application.Current.MainWindow;

            previewWindow.ShowDialog();

            int queuedCount = ReportSession?.Count ?? 0;

            if (previewWindow.Outcome == ChromExplorer.Modules.Viewer.Views.ReportPreviewOutcome.Discarded)
            {
                InsertAuditLog("Generate Report",
                    queuedCount == 0 ? "Preview closed with no files queued" : "Report discarded after in-app preview");
                return;
            }

            // Outcome.ReadyToSave
            var entries = previewWindow.ApprovedEntries ?? new List<ChromatogramReportData>();

            if (entries.Count == 0)
                return; // Save is disabled in this state; guard against it anyway.

            // No Save dialog here — the analyst doesn't pick a location/name for a file that
            // isn't reviewed yet, same as the old "Send for Review" never asked either. The PDF
            // is written straight to an app-managed folder; the analyst's copy only lands in the
            // report folder (D:\ChromReport\General) later, via the Download button on Review
            // Saved Files, once a Reviewer has actually approved/rejected it.
            // The staging folder is on the D: drive too (D:\ChromExplorer\PendingReports) — no
            // file is ever written outside the app's storage folders.
            string pendingReportsDir = FileStorage.EnsureFolder(StorageCategory.PendingReports);
            if (pendingReportsDir == null)
                return;

            string filePath = System.IO.Path.Combine(pendingReportsDir,
                ReportStorage.GeneralFilePrefix + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");

            if (!FileSaveGuard.CanSaveTo(filePath, "report"))
                return;

            try
            {
                new ChromatogramPdfExporter().Export(entries, filePath);

                long fileId = ChromExplorer.Infrastructure.SavedFileRegistry.Record(
                    ChromExplorer.Infrastructure.SavedFileRegistry.TypeReport, filePath);

                // A Report goes straight into the Pending Review queue the moment it's
                // generated — there is no "freely usable, unreviewed" state for it, unlike
                // Method File's separate manual "Submit for Review" step. The analyst only gets
                // a Download button for this PDF once a Reviewer has approved/rejected it (see
                // ReviewSavedFiles.xaml SavedFileRecord.CanDownload).
                int userId = 0;
                if (Application.Current?.Properties?["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);
                string username = Application.Current?.Properties?["Username"] as string ?? Environment.UserName;

                if (fileId > 0)
                    ChromExplorer.Infrastructure.SavedFileRegistry.SubmitForReview(fileId, userId, username);

                MessageBox.Show(
                    "Report sent for reviewer approval.\n\nYou'll be able to download the reviewed PDF from Review Saved Files once a reviewer has approved/rejected it.",
                    "Sent for Review", MessageBoxButton.OK, MessageBoxImage.Information);
                InsertAuditLog(
                    "Generate Report",
                    $"PDF Report Generated and Submitted for Review | Files: {entries.Count} | Saved to: {filePath}",
                    Path.GetFileName(filePath));

                // Reset the queue so the next report starts clean instead of silently
                // re-including files from a report that was already saved.
                ReportSession?.Clear();
            }
            catch (Exception ex)
            {
                InsertAuditLog("Generate Report", "PDF Report Save Failed : " + ex.Message);
                MessageBox.Show("Error saving report: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                log.Error("Report save failed", ex);
            }
        }

        private ChromatogramReportData BuildReportData()
        {
            var data = new ChromatogramReportData
            {
                GeneratedDate = DateTime.Now.ToString("f"),
                // Title is never filled in for files loaded from disk — the real data file name
                // lives in Filename; without this every report printed "Unknown" as its file name.
                FileName = !string.IsNullOrEmpty(Chromatogram?.Filename)
                    ? Path.GetFileName(Chromatogram.Filename)
                    : (Chromatogram?.Title ?? "Unknown"),
                Description = Chromatogram?.Description ?? "Analysis Report",

                TotalScans = Chromatogram?.Spectrums?.Count.ToString() ?? "0",
                DataPoints = Chromatogram?.Points?.Length.ToString() ?? "0",
                SelectedIndex = Chromatogram?.SelectedIndex.ToString() ?? "0",
                SelectedSIC = Chromatogram?.SelectedSIC.ToString("F0") ?? "0",
                VisibleArea = ChromVM?.Area.ToString("F0") ?? "0",
                XAxisUnits = "Retention Time (Min)",

                RetentionTime = Chromatogram?.CurrentSpectrum?.RetentionTime.ToString("F3") ?? "0",
                ScanIndex = Chromatogram?.CurrentSpectrum?.Index.ToString() ?? "0",
                ScanID = Chromatogram?.CurrentSpectrum?.ID.ToString() ?? "N/A",
                InjectionTimeMs = Chromatogram?.CurrentSpectrum?.InjectionTime.ToString() ?? "0",
                TIC = Chromatogram?.CurrentSpectrum?.TIC.ToString("F0") ?? "0",
                ParentIon = Chromatogram?.CurrentSpectrum?.ParentIon.ToString("F4") ?? "0",
                BaselineRemoved = Chromatogram?.CurrentSpectrum?.BaselineRemoved.ToString() ?? "False",
            };

            string currentUserName = Environment.UserName;
            if (Application.Current.Properties.Contains("Username") && Application.Current.Properties["Username"] != null)
            {
                currentUserName = Application.Current.Properties["Username"].ToString();
            }
            data.UserName = currentUserName;

            data.LogoImage = LoadLogoBytes();
            data.ChromatogramImage = DecodeDataUrl(GetChromatogramSnapshot?.Invoke());
            data.SpectrumImage = DecodeDataUrl(GetSpectrumSnapshot?.Invoke());

            // Detected chromatogram peaks
            if (ChromVM != null && ChromVM.Markers != null)
            {
                var sortedChromMarkers = ChromVM.Markers.OrderBy(m => m.Position.X).ToList();
                int i = 1;
                foreach (var marker in sortedChromMarkers)
                {
                    double rt = marker.Position.X;
                    double area = 0;
                    try
                    {
                        string[] parts = (marker.Text ?? "").Split('\n');
                        if (parts.Length > 1)
                        {
                            string areaPart = parts[1].Replace("Area:", "").Trim();
                            double.TryParse(areaPart, out area);
                        }
                    }
                    catch { }

                    data.ChromPeakRows.Add(new[] { (i++).ToString(), rt.ToString("F4"), $"RT:{rt:F3}  A:{area:F0}" });
                }
            }

            // Spectrum peak list
            if (SpectrumVM != null && SpectrumVM.Markers != null)
            {
                var sortedMarkers = SpectrumVM.Markers.OrderBy(m => m.Position.X).ToList();
                for (int i = 0; i < sortedMarkers.Count; i++)
                {
                    var marker = sortedMarkers[i];
                    data.SpectrumPeakRows.Add(new[] { (i + 1).ToString(), marker.Position.X.ToString("F4"), marker.Position.Y.ToString("F0") });
                }
            }

            // Post-processing settings
            if (_ppService != null && _ppService.Operations != null)
            {
                var ops = _ppService.Operations.ToList();

                Func<IPostProcessingOperation, string, string> getConfigValue = (op, propName) =>
                {
                    try
                    {
                        if (op.ConfigView != null)
                        {
                            var dataContext = (op.ConfigView as System.Windows.FrameworkElement)?.DataContext;
                            if (dataContext != null)
                            {
                                var prop = dataContext.GetType().GetProperty(propName);
                                if (prop != null)
                                {
                                    var val = prop.GetValue(dataContext);
                                    if (val is double d) return d.ToString("G");
                                    return val?.ToString() ?? "";
                                }
                            }
                        }
                    }
                    catch { }
                    return "";
                };

                var removeSpikes = ops.FirstOrDefault(o => o.ObjectKey == "RemoveSpikes");
                if (removeSpikes != null)
                {
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Remove spikes", removeSpikes.IsActive ? "Active" : "Inactive", true, removeSpikes.IsActive));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Median Multiplier", getConfigValue(removeSpikes, "MedianMultiplier")));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Width Threshold", getConfigValue(removeSpikes, "WidthThreshold")));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Min Width", getConfigValue(removeSpikes, "MinWidth")));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Max Width", getConfigValue(removeSpikes, "MaxWidth")));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Peak-Pick Threshold", getConfigValue(removeSpikes, "PPThreshold")));
                }

                var normalize = ops.FirstOrDefault(o => o.ObjectKey == "Normalize");
                if (normalize != null)
                {
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Normalize", normalize.IsActive ? "Active" : "Inactive", true, normalize.IsActive));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Normalize To", getConfigValue(normalize, "NormalizeTo")));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Shift To Zero", getConfigValue(normalize, "ShiftToZero")));
                }

                var medianFilter = ops.FirstOrDefault(o => o.ObjectKey == "MedianFilter");
                if (medianFilter != null)
                {
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Median Filter", medianFilter.IsActive ? "Active" : "Inactive", true, medianFilter.IsActive));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Window Size", getConfigValue(medianFilter, "MedianWindowSize")));
                }

                var sgSmoother = ops.FirstOrDefault(o => o.ObjectKey == "SavitzkyGolaySmoother");
                if (sgSmoother != null)
                {
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Savitzky-Golay Smoother", sgSmoother.IsActive ? "Active" : "Inactive", true, sgSmoother.IsActive));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Polynomial Degree (M)", getConfigValue(sgSmoother, "M")));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Points Left (nL)", getConfigValue(sgSmoother, "nL")));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Points Right (nR)", getConfigValue(sgSmoother, "nR")));
                }

                var gaussianFilter = ops.FirstOrDefault(o => o.ObjectKey == "GaussianFilter");
                if (gaussianFilter != null)
                {
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Gaussian Filter", gaussianFilter.IsActive ? "Active" : "Inactive", true, gaussianFilter.IsActive));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Function Width (kernel size)", getConfigValue(gaussianFilter, "KernelSize")));
                    data.SpectrumPostProcessing.Add(new PdfKeyValueRow("Gaussian Width (sigma)", getConfigValue(gaussianFilter, "Sigma")));
                }

                var eicSG = ops.FirstOrDefault(o => o.ObjectKey == "EicSavitzkyGolaySmoother");
                if (eicSG != null)
                {
                    data.EicPostProcessing.Add(new PdfKeyValueRow("Savitzky�Golay Filter", eicSG.IsActive ? "Active" : "Inactive", true, eicSG.IsActive));
                    data.EicPostProcessing.Add(new PdfKeyValueRow("Polynomial Degree", getConfigValue(eicSG, "M")));
                    data.EicPostProcessing.Add(new PdfKeyValueRow("Points Left (NL)", getConfigValue(eicSG, "nL")));
                    data.EicPostProcessing.Add(new PdfKeyValueRow("Points Right (NR)", getConfigValue(eicSG, "nR")));
                }

                var eicGaussian = ops.FirstOrDefault(o => o.ObjectKey == "EicGaussianFilter");
                if (eicGaussian != null)
                {
                    data.EicPostProcessing.Add(new PdfKeyValueRow("Gaussian Filter", eicGaussian.IsActive ? "Active" : "Inactive", true, eicGaussian.IsActive));
                    data.EicPostProcessing.Add(new PdfKeyValueRow("Function Width (kernel size)", getConfigValue(eicGaussian, "KernelSize")));
                    data.EicPostProcessing.Add(new PdfKeyValueRow("Gaussian Width (sigma)", getConfigValue(eicGaussian, "Sigma")));
                }
            }

            // Manual peak integration
            if (ChromVM != null && ChromVM.IsIntegrationActive &&
                !double.IsNaN(ChromVM.IntegrationStartTime) && !double.IsNaN(ChromVM.IntegrationEndTime))
            {
                data.IntegrationStatus = "Performed";
                data.IntegrationStartTime = ChromVM.IntegrationStartTime.ToString("F4") + " Min";
                data.IntegrationEndTime = ChromVM.IntegrationEndTime.ToString("F4") + " Min";
                data.IntegrationRT = double.IsNaN(ChromVM.IntegrationRT) ? "N/A" : ChromVM.IntegrationRT.ToString("F4") + " Min";
                data.IntegrationArea = ChromVM.Area.ToString("F0");
            }
            else
            {
                data.IntegrationStatus = "Not Performed";
                data.IntegrationStartTime = "N/A";
                data.IntegrationEndTime = "N/A";
                data.IntegrationRT = "N/A";
                data.IntegrationArea = "N/A";
            }

            // Signal-to-noise
            if (ChromVM != null && ChromVM.IsSNRCalculated)
            {
                data.SNRStatus = "Performed";
                data.SNRSignalDefinition = string.IsNullOrEmpty(ChromVM.SNRSignalDefinition) ? "N/A" : ChromVM.SNRSignalDefinition;
                data.SNRSignalValue = double.IsNaN(ChromVM.SNRSignalValue) ? "N/A" : ChromVM.SNRSignalValue.ToString("F2");
                data.SNRNoiseDefinition = string.IsNullOrEmpty(ChromVM.SNRNoiseDefinition) ? "N/A" : ChromVM.SNRNoiseDefinition;
                data.SNRNoiseValue = double.IsNaN(ChromVM.SNRNoiseValue) ? "N/A" : ChromVM.SNRNoiseValue.ToString("F2");
                data.SNRRatio = double.IsNaN(ChromVM.SNRatio) ? "N/A" : ChromVM.SNRatio.ToString("F2");
                data.SNRNoiseRegionUsed = string.IsNullOrEmpty(ChromVM.SNRNoiseRegionUsed) ? "N/A" : ChromVM.SNRNoiseRegionUsed;
            }
            else
            {
                data.SNRStatus = "Not Performed";
                data.SNRSignalDefinition = "N/A";
                data.SNRSignalValue = "N/A";
                data.SNRNoiseDefinition = "N/A";
                data.SNRNoiseValue = "N/A";
                data.SNRRatio = "N/A";
                data.SNRNoiseRegionUsed = "N/A";
            }

            // RSD (Relative Standard Deviation) summary — same shared-session shape as Quan
            // below, rendered once per report just before it.
            var rsdSummaries = _rsdService?.ComputeGroupSummaries();
            if (rsdSummaries != null)
            {
                foreach (var s in rsdSummaries)
                {
                    data.RsdRows.Add(new[]
                    {
                        s.Concentration.HasValue ? s.Concentration.Value.ToString("0.######") : "(not entered)",
                        s.Count.ToString(),
                        s.MeanArea.ToString("F0"),
                        s.Count >= 2 ? s.StdDev.ToString("F2") : "N/A",
                        s.Count >= 2 ? s.RsdPercent.ToString("F2") : "N/A",
                        s.Note ?? ""
                    });
                }
            }

            // RSD "Queued Files" grid (one row per file added via Add to RSD)
            if (_rsdService?.Entries != null)
            {
                int ri = 1;
                foreach (var e in _rsdService.Entries)
                {
                    data.RsdQueuedRows.Add(new[]
                    {
                        (ri++).ToString(),
                        e.SampleID ?? "",
                        e.Concentration.HasValue ? e.Concentration.Value.ToString("0.######") : "(not entered)",
                        e.Area.ToString("F0"),
                        e.FilePath ?? "",
                        e.InsertedDate ?? ""
                    });
                }
            }

            // Quantitation summary
            var quanPeaks = _quanService?.Peaks?.ToList();
            if (quanPeaks != null && quanPeaks.Count > 0)
            {
                int qi = 1;
                foreach (var peak in quanPeaks)
                {
                    string expAmt = peak.ExpectedAmount.HasValue ? peak.ExpectedAmount.Value.ToString("F3") : "N/A";
                    string calcAmt = peak.CalculatedAmount.HasValue ? peak.CalculatedAmount.Value.ToString("F3") : "N/A";
                    string recovery = peak.RecoveryPercent.HasValue ? peak.RecoveryPercent.Value.ToString("F2") : "N/A";
                    string diff = peak.Difference.HasValue ? peak.Difference.Value.ToString("F2") : "N/A";

                    data.QuanRows.Add(new[]
                    {
                        (qi++).ToString(), peak.SampleID, peak.SampleType, expAmt,
                        peak.Area.ToString("F0"), calcAmt, recovery, diff, peak.FilePath
                    });
                }

                var fitResult = _quanService.FitResult;
                if (fitResult != null)
                {
                    string sign = fitResult.Offset >= 0 ? "+" : "-";
                    data.QuanEquation = $"y = {fitResult.Slope:G6} * x {sign} {Math.Abs(fitResult.Offset):G6}, R^2 = {fitResult.RSquared:F3}";
                }
                else
                {
                    data.QuanEquation = "N/A (insufficient calibrant points)";
                }

                data.QuanChartImage = DecodeDataUrl(BuildQuanChartImage(quanPeaks, _quanService.FitResult));
            }
            else
            {
                data.QuanEquation = "N/A";
            }

            return data;
        }

        private static byte[] DecodeDataUrl(string dataUrl)
        {
            if (string.IsNullOrEmpty(dataUrl)) return null;
            int commaIndex = dataUrl.IndexOf(',');
            if (commaIndex < 0) return null;
            try
            {
                return Convert.FromBase64String(dataUrl.Substring(commaIndex + 1));
            }
            catch
            {
                return null;
            }
        }

        private static byte[] LoadLogoBytes()
        {
            string[] logoPossiblePaths = new string[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons", "Perspectives", "MT_logo_12x4_wb_pp-scaled.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChromExplorer.Assets", "Icons", "Perspectives", "MT_logo_12x4_wb_pp-scaled.png"),
            };

            foreach (var lp in logoPossiblePaths)
            {
                if (File.Exists(lp))
                    return File.ReadAllBytes(lp);
            }

            return null;
        }

        private string BuildQuanChartImage(System.Collections.Generic.List<ChromExplorer.Modules.Quan.Model.ChromatogramPeakInfo> peaks, ChromExplorer.Modules.Quan.Model.FitResult fitResult)
        {
            try
            {
                var calibPeaks = peaks
                    .Where(p => p != null && p.ExpectedAmount.HasValue &&
                                string.Equals((p.SampleType ?? "").Trim(), "Calibrant", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.Area)
                    .ToList();

                if (calibPeaks.Count == 0)
                    return "";

                double width = 620, height = 380;
                double marginLeft = 65, marginRight = 25, marginTop = 65, marginBottom = 45;
                double plotW = width - marginLeft - marginRight;
                double plotH = height - marginTop - marginBottom;

                double xMin = calibPeaks.Min(p => p.Area);
                double xMax = calibPeaks.Max(p => p.Area);
                double yMin = calibPeaks.Min(p => p.ExpectedAmount.Value);
                double yMax = calibPeaks.Max(p => p.ExpectedAmount.Value);

                if (fitResult != null)
                {
                    // Make sure the fit line's endpoints are included in the visible range
                    yMin = Math.Min(yMin, Math.Min(fitResult.GetY(xMin), fitResult.GetY(xMax)));
                    yMax = Math.Max(yMax, Math.Max(fitResult.GetY(xMin), fitResult.GetY(xMax)));
                }

                if (xMax - xMin < 1e-9) { xMax += 1; xMin -= 1; }
                if (yMax - yMin < 1e-9) { yMax += 1; yMin -= 1; }

                double xPad = (xMax - xMin) * 0.08;
                double yPad = (yMax - yMin) * 0.1;
                xMin -= xPad; xMax += xPad;
                yMin -= yPad; yMax += yPad;

                var xTicks = GetNiceTicks(xMin, xMax, 8);
                var yTicks = GetNiceTicks(yMin, yMax, 8);
                if (xTicks.Count > 0) { xMin = Math.Min(xMin, xTicks[0]); xMax = Math.Max(xMax, xTicks[xTicks.Count - 1]); }
                if (yTicks.Count > 0) { yMin = Math.Min(yMin, yTicks[0]); yMax = Math.Max(yMax, yTicks[yTicks.Count - 1]); }

                Func<double, double> mapX = ax => marginLeft + (ax - xMin) / (xMax - xMin) * plotW;
                Func<double, double> mapY = ay => marginTop + plotH - (ay - yMin) / (yMax - yMin) * plotH;

                var canvas = new System.Windows.Controls.Canvas
                {
                    Width = width,
                    Height = height,
                    Background = System.Windows.Media.Brushes.White
                };

                // Title
                var title = new System.Windows.Controls.TextBlock
                {
                    Text = "Calibration curve",
                    FontSize = 15,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.Black
                };
                title.Measure(new System.Windows.Size(width, 30));
                System.Windows.Controls.Canvas.SetLeft(title, (width - title.DesiredSize.Width) / 2);
                System.Windows.Controls.Canvas.SetTop(title, 8);
                canvas.Children.Add(title);

                // Subtitle (equation)
                if (fitResult != null)
                {
                    string sign = fitResult.Offset >= 0 ? "+" : "-";
                    string eq = $"y = {fitResult.Slope:G6} * x {sign} {Math.Abs(fitResult.Offset):G6}, R^2 = {fitResult.RSquared:F3}";
                    var subtitle = new System.Windows.Controls.TextBlock
                    {
                        Text = eq,
                        FontSize = 11,
                        Foreground = System.Windows.Media.Brushes.Black
                    };
                    subtitle.Measure(new System.Windows.Size(width, 20));
                    System.Windows.Controls.Canvas.SetLeft(subtitle, (width - subtitle.DesiredSize.Width) / 2);
                    System.Windows.Controls.Canvas.SetTop(subtitle, 30);
                    canvas.Children.Add(subtitle);
                }

                // Gridlines + tick labels
                foreach (var xt in xTicks)
                {
                    double gx = mapX(xt);
                    canvas.Children.Add(new System.Windows.Shapes.Line
                    {
                        X1 = gx,
                        Y1 = marginTop,
                        X2 = gx,
                        Y2 = marginTop + plotH,
                        Stroke = System.Windows.Media.Brushes.LightGray,
                        StrokeThickness = 1
                    });
                    var lbl = new System.Windows.Controls.TextBlock { Text = FormatTick(xt), FontSize = 9, Foreground = System.Windows.Media.Brushes.DimGray };
                    lbl.Measure(new System.Windows.Size(80, 16));
                    System.Windows.Controls.Canvas.SetLeft(lbl, gx - lbl.DesiredSize.Width / 2);
                    System.Windows.Controls.Canvas.SetTop(lbl, marginTop + plotH + 4);
                    canvas.Children.Add(lbl);
                }

                foreach (var yt in yTicks)
                {
                    double gy = mapY(yt);
                    canvas.Children.Add(new System.Windows.Shapes.Line
                    {
                        X1 = marginLeft,
                        Y1 = gy,
                        X2 = marginLeft + plotW,
                        Y2 = gy,
                        Stroke = System.Windows.Media.Brushes.LightGray,
                        StrokeThickness = 1
                    });
                    var lbl = new System.Windows.Controls.TextBlock { Text = FormatTick(yt), FontSize = 9, Foreground = System.Windows.Media.Brushes.DimGray };
                    lbl.Measure(new System.Windows.Size(50, 16));
                    System.Windows.Controls.Canvas.SetLeft(lbl, marginLeft - lbl.DesiredSize.Width - 6);
                    System.Windows.Controls.Canvas.SetTop(lbl, gy - 7);
                    canvas.Children.Add(lbl);
                }

                // Axes (drawn on top of gridlines)
                canvas.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = marginLeft,
                    Y1 = marginTop + plotH,
                    X2 = marginLeft + plotW,
                    Y2 = marginTop + plotH,
                    Stroke = System.Windows.Media.Brushes.Black,
                    StrokeThickness = 1
                });
                canvas.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = marginLeft,
                    Y1 = marginTop,
                    X2 = marginLeft,
                    Y2 = marginTop + plotH,
                    Stroke = System.Windows.Media.Brushes.Black,
                    StrokeThickness = 1
                });

                if (fitResult != null)
                {
                    double y1 = fitResult.GetY(xMin);
                    double y2 = fitResult.GetY(xMax);
                    canvas.Children.Add(new System.Windows.Shapes.Line
                    {
                        X1 = mapX(xMin),
                        Y1 = mapY(y1),
                        X2 = mapX(xMax),
                        Y2 = mapY(y2),
                        Stroke = System.Windows.Media.Brushes.Orange,
                        StrokeThickness = 2
                    });
                }

                foreach (var p in calibPeaks)
                {
                    double cx = mapX(p.Area);
                    double cy = mapY(p.ExpectedAmount.Value);
                    var dot = new System.Windows.Shapes.Rectangle
                    {
                        Width = 8,
                        Height = 8,
                        Fill = System.Windows.Media.Brushes.Green
                    };
                    System.Windows.Controls.Canvas.SetLeft(dot, cx - 4);
                    System.Windows.Controls.Canvas.SetTop(dot, cy - 4);
                    canvas.Children.Add(dot);
                }

                // Axis unit labels
                var xLabel = new System.Windows.Controls.TextBlock { Text = "Area", FontSize = 11, Foreground = System.Windows.Media.Brushes.Black };
                xLabel.Measure(new System.Windows.Size(width, 20));
                System.Windows.Controls.Canvas.SetLeft(xLabel, marginLeft + plotW / 2 - xLabel.DesiredSize.Width / 2);
                System.Windows.Controls.Canvas.SetTop(xLabel, height - 20);
                canvas.Children.Add(xLabel);

                var yLabel = new System.Windows.Controls.TextBlock
                {
                    Text = "Amount",
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Black,
                    RenderTransform = new System.Windows.Media.RotateTransform(-90)
                };
                yLabel.Measure(new System.Windows.Size(height, 20));
                System.Windows.Controls.Canvas.SetLeft(yLabel, 2);
                System.Windows.Controls.Canvas.SetTop(yLabel, marginTop + plotH / 2 + yLabel.DesiredSize.Width / 2);
                canvas.Children.Add(yLabel);

                canvas.Measure(new System.Windows.Size(width, height));
                canvas.Arrange(new System.Windows.Rect(0, 0, width, height));
                canvas.UpdateLayout();

                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)width, (int)height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(canvas);

                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));

                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                log.Warn("BuildQuanChartImage failed", ex);
                return "";
            }
        }

        private static System.Collections.Generic.List<double> GetNiceTicks(double min, double max, int desiredCount)
        {
            var ticks = new System.Collections.Generic.List<double>();
            if (max <= min || desiredCount < 2)
            {
                ticks.Add(min);
                ticks.Add(max);
                return ticks;
            }

            double range = max - min;
            double rawStep = range / desiredCount;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
            double residual = rawStep / magnitude;

            double niceResidual;
            if (residual > 5) niceResidual = 10;
            else if (residual > 2) niceResidual = 5;
            else if (residual > 1) niceResidual = 2;
            else niceResidual = 1;

            double step = niceResidual * magnitude;
            double start = Math.Floor(min / step) * step;

            for (double v = start; v <= max + step * 0.5; v += step)
            {
                if (v >= min - step * 0.001)
                    ticks.Add(Math.Round(v, 10));
            }

            return ticks;
        }

        private static string FormatTick(double value)
        {
            if (Math.Abs(value) >= 1000)
                return value.ToString("F0");
            if (Math.Abs(value - Math.Round(value)) < 1e-6)
                return value.ToString("F0");
            return value.ToString("G4");
        }

        private void InitializeCommands()
        {
            TestCommand = new DelegateCommand<object>(OnTestCommandExecute);
            IncrementSelectedCommand = new DelegateCommand<object>(OnIncrementSelectedIndex);
            DecrementSelectedCommand = new DelegateCommand<object>(OnDecrementSelectedIndex);
            GenerateReportCommand = new DelegateCommand<object>(OnGenerateReportExecute);
            OpenInNistCommand = new DelegateCommand<object>(OnOpenInNist);
            ApplySavedMethodCommand = new DelegateCommand<object>(OnApplySavedMethod);
        }

        // "Apply Saved Method": loads a .cmethod file created from the Settings popup's "Create
        // Method" button and pushes it straight into the graph, without needing that popup open.
        // Peak Picking and PostProcessing go through the same PeakPickerService/IPostProcessingService
        // singleton instances the Settings tabs' own "Apply" buttons use, so this ends up doing
        // exactly what clicking "Apply" on each tab would do. Nist Library paths are restored into
        // the shared app-config settings the same way that tab's own "Apply" does. Peak Integration
        // is re-applied by firing the same PeakIntegrationEvent that tab's own "Apply" fires. SNR
        // isn't re-applied here — it's a read-only calculation report tied to whichever peak was
        // selected when "Calculate" was clicked, not a standing graph filter, so there's nothing to
        // re-apply without that exact selection; its last-calculated values are still saved in the
        // file for reference.
        private void OnApplySavedMethod(object obj)
        {
            var dlg = new OpenFileDialog
            {
                DefaultExt = ChromMethodFileService.FileExtension,
                Filter = ChromMethodFileService.FileDialogFilter
            };

            if (!FileStorage.ShowOpenDialog(dlg, StorageCategory.DataProcessingMethods, "data processing method"))
                return;

            // Only Approved methods may be used. A Rejected one is still loadable so the analyst
            // can edit it and save the revision under a new name (never over the original);
            // anything else (not reviewed yet, deactivated, unknown file) is refused.
            var gateState = SavedFileRegistry.GetReviewState(SavedFileRegistry.TypeDataProcessingMethod, dlg.FileName);
            if (!gateState.IsApproved && !gateState.IsRejected)
            {
                string why = !gateState.Found ? "This file is not a saved Data Processing Method known to this system."
                    : !gateState.IsReviewed ? "This method has not been reviewed yet."
                    : !gateState.IsActive ? "This method has been deactivated."
                    : "This method is not approved.";
                MessageBox.Show(why + "\n\nOnly Approved methods can be applied.", "Apply Saved Method",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InsertAuditLog("Method Apply Blocked - Not Approved",
                    $"Attempted to apply a Data Processing Method that is not approved | {why}", Path.GetFileName(dlg.FileName));
                return;
            }

            try
            {
                var method = ChromMethodFileService.Load(dlg.FileName);
                if (method == null)
                {
                    MessageBox.Show("Could not read method file.", "Apply Saved Method", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // A Rejected method is only loaded into the settings for editing: nothing is
                // run on, saved for, or drawn on the graph (no peak picking, post-processing,
                // integration or SNR) until the analyst applies their revised settings.
                bool applyToGraph = gateState.IsApproved;
                ApplyPeakPickingMethod(method.PeakPicking, applyToGraph);
                ApplyPostProcessingMethod(method.PostProcessingOperations, applyToGraph);
                if (applyToGraph)
                {
                    ApplyNistLibraryMethod(method.NistLibrary);
                    ApplyPeakIntegrationMethod(method.PeakIntegration);
                    ApplySNRMethod(method.SNR, method.PeakIntegration);
                    InsertAuditLog("Method Applied", $"Method '{method.MethodName}' applied to graph", dlg.FileName);
                }
                else
                {
                    LoadMethodIntoSettingsTabsOnly(method);
                }

                // Remember which file these settings came from so Create Method (Settings tab)
                // proposes a new "_revN" name if this one was Rejected and gets edited.
                SavedFileRegistry.SetOpenedFile(SavedFileRegistry.TypeDataProcessingMethod, dlg.FileName);
                var appliedState = SavedFileRegistry.GetReviewState(SavedFileRegistry.TypeDataProcessingMethod, dlg.FileName);
                if (appliedState.IsRejected)
                {
                    InsertAuditLog("Rejected Method Applied for Edit",
                        $"A Rejected Data Processing Method was applied | Rejected by: {appliedState.ReviewedBy} | Remark: {appliedState.Remark}",
                        Path.GetFileName(dlg.FileName));
                    SavedFileRegistry.ShowRejectedNotice(Path.GetFileName(dlg.FileName), appliedState.ReviewedBy, appliedState.Remark,
                        "You can edit these settings now. Go to Settings and use Create Method to save the revised method under a NEW file name — the original file is never overwritten.");
                }
            }
            catch (Exception ex)
            {
                log.Error("Apply Saved Method failed", ex);
                try { InsertAuditLog("Method Applied - Failed", "Saved method could not be applied | Error: " + ex.Message, Path.GetFileName(dlg.FileName)); } catch { }
                MessageBox.Show("Could not apply the method: " + ex.Message, "Apply Saved Method", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Fills the Peak Integration, SNR and NIST Library tabs from a Rejected method so the
        // analyst can edit them — values only: nothing is integrated, calculated or saved.
        private void LoadMethodIntoSettingsTabsOnly(ChromMethodFile method)
        {
            if (method.PeakIntegration != null)
            {
                _eventAggregator?.GetEvent<PeakIntegrationMethodLoadedEvent>().Publish(new PeakIntegrationArgs
                {
                    StartTime = method.PeakIntegration.StartTime,
                    EndTime = method.PeakIntegration.EndTime,
                    IsRemove = false
                });
            }

            var snr = method.SNR;
            if (snr != null)
            {
                _eventAggregator?.GetEvent<SNRMethodAppliedEvent>().Publish(new SNRMethodAppliedArgs
                {
                    SignalDefinition = snr.SignalDefinition,
                    NoiseDefinition = snr.NoiseDefinition,
                    Multiplier = snr.Multiplier,
                    IsAutomaticDetection = snr.IsAutomaticDetection,
                    DesiredLength = snr.DesiredLength,
                    MinimumLength = snr.MinimumLength,
                    SpecificTimeRange = snr.SpecificTimeRange,
                    AutoCompute = snr.AutoCompute,
                    SignalValue = snr.SignalValue,
                    NoiseValue = snr.NoiseValue,
                    SNRatio = snr.SNRatio,
                    NoiseRegionUsed = snr.NoiseRegionUsed
                });
            }

            var nist = method.NistLibrary;
            if (nist != null)
            {
                NistLibrarySettings.Default.NistExecutable = nist.NistExecutablePath;
                NistLibrarySettings.Default.FirstLocatorFile = nist.FirstLocatorFile;
                NistLibrarySettings.Default.SecondLocatorFile = nist.SecondLocatorFile;
                NistLibrarySettings.Default.WorkingDir = nist.WorkingDir;
            }
        }

        private void ApplyPeakPickingMethod(PeakPickingMethodSettings settings, bool applyToGraph = true)
        {
            if (settings == null || _peakPickerService == null)
                return;

            var algorithm = _peakPickerService.Algorithms?
                .FirstOrDefault(a => a.ObjectKey == settings.AlgorithmObjectKey);

            if (algorithm != null)
            {
                _peakPickerService.SelectedAlgorithm = algorithm;
                MethodSettingsReflector.TrySetPeakPickerHalfSizes(
                    algorithm, settings.MinPeakHalfSize, settings.PeakHalfSize);
            }

            if (Enum.TryParse(settings.ThresholdType, out ThresholdTypes thresholdType))
                _peakPickerService.ThresholdType = thresholdType;

            if (Enum.TryParse(settings.Analyzer, out PeakAnalyzers analyzer))
                _peakPickerService.Analyzer = analyzer;

            if (Enum.TryParse(settings.LabelOrientation, out MarkerOrientation orientation))
                _peakPickerService.LabelOrientation = orientation;

            if (Enum.TryParse(settings.LabelPlacement, out MarkerPlacement placement))
                _peakPickerService.LabelPlacement = placement;

            _peakPickerService.Threshold = settings.Threshold;
            _peakPickerService.LabelFormat = settings.LabelFormat;

            if (applyToGraph)
                _peakPickerService.RaisePerformPeakickingEvent();
        }

        private void ApplyPostProcessingMethod(System.Collections.Generic.List<PostProcessingOperationSettings> operations, bool applyToGraph = true)
        {
            if (operations == null || operations.Count == 0 || _ppService == null)
                return;

            foreach (var saved in operations)
            {
                var op = _ppService.Operations?.FirstOrDefault(o => o.ObjectKey == saved.ObjectKey);
                if (op == null)
                    continue;

                MethodSettingsReflector.ApplyConfigViewParameters(op, saved.Parameters);
                op.IsActive = saved.IsActive;
                if (applyToGraph)
                    op.SaveSettings();
            }

            if (!applyToGraph)
                return;

            _ppService.SaveMsSettings();
            _ppService.SaveEicSettings();
            _ppService.RaisePostProcessingApplied();
        }

        // Fires the same PeakIntegrationEvent the Peak Integration tab's own "Apply" button fires,
        // so a saved range actually gets marked/integrated on the graph instead of just sitting
        // inertly in the file. Skipped when EndTime <= StartTime — an unset/zero range would
        // otherwise clear or misapply integration on every load.
        private void ApplyPeakIntegrationMethod(PeakIntegrationMethodSettings settings)
        {
            if (settings == null || settings.EndTime <= settings.StartTime || _eventAggregator == null)
                return;

            _eventAggregator.GetEvent<PeakIntegrationEvent>().Publish(new PeakIntegrationArgs
            {
                StartTime = settings.StartTime,
                EndTime = settings.EndTime,
                IsRemove = false
            });
        }

        // Unlike Peak Picking/PostProcessing, SNR has no "current settings" service to push into —
        // its calculation only ever runs against whatever peak/spectrum is selected at the moment.
        // So instead of just replaying the saved (possibly stale, possibly never-calculated) result,
        // this re-runs the actual SNR calculation against the file that's open right now, using the
        // saved SNR inputs and the saved Peak Integration range as the signal window — the same
        // calculation the SNR tab's own "Calculate S/N" performs, just without needing that tab
        // open. Falls back to republishing the saved values (so the tab shows something sensible if
        // opened afterward) when a fresh calculation isn't possible — e.g. no peaks picked yet.
        private void ApplySNRMethod(SNRMethodSettings settings, PeakIntegrationMethodSettings integration)
        {
            if (settings == null)
                return;

            double signalValue = settings.SignalValue;
            double noiseValue = settings.NoiseValue;
            double snRatio = settings.SNRatio;
            string noiseRegionUsed = settings.NoiseRegionUsed;
            bool recalculated = false;
            string skipReason = null;
            SNRResult calculatedResult = null;

            var points = Chromatogram?.Points;

            if (SNRService == null)
                skipReason = "SNR service is not available.";
            else if (points?.Xvals == null || points.Yvals == null || points.Xvals.Length == 0)
                skipReason = "No chromatogram data is loaded.";
            else if (_peakPickerService == null)
                skipReason = "Peak picker service is not available.";

            if (skipReason == null)
            {
                var peaks = _peakPickerService.GetPeaks(points, points.Xvals.Min(), points.Xvals.Max());
                if (peaks == null || peaks.Count == 0)
                {
                    skipReason = "No peaks detected — run peak picking on this file first.";
                }
                else
                {
                    ChromExplorer.DataModel.PeakPicking.PeakInfo targetPeak = null;
                    var selectedIndex = Chromatogram.SelectedIndex;
                    if (selectedIndex >= 0 && selectedIndex < points.Xvals.Length)
                    {
                        var selectedTime = points.Xvals[selectedIndex];
                        targetPeak = peaks.OrderBy(p => Math.Abs(p.Position - selectedTime)).FirstOrDefault();
                    }
                    targetPeak = targetPeak ?? peaks.OrderByDescending(p => p.Intensity).First();

                    bool hasIntegrationRange = integration != null && integration.EndTime > integration.StartTime;

                    var calcSettings = new SNRCalculationSettings
                    {
                        SignalDefinition = settings.SignalDefinition,
                        NoiseDefinition = settings.NoiseDefinition,
                        Multiplier = settings.Multiplier,
                        IsAutomaticDetection = settings.IsAutomaticDetection,
                        DesiredLength = settings.DesiredLength,
                        MinimumLength = settings.MinimumLength,
                        SpecificTimeRange = settings.SpecificTimeRange,
                        IsSignalIntegrationActive = hasIntegrationRange,
                        SignalIntegrationStartTime = hasIntegrationRange ? integration.StartTime : 0,
                        SignalIntegrationEndTime = hasIntegrationRange ? integration.EndTime : 0
                    };

                    var result = SNRService.CalculateSNR(points, peaks, targetPeak, calcSettings);
                    if (result != null && result.Success)
                    {
                        signalValue = result.SignalValue;
                        noiseValue = result.NoiseValue;
                        snRatio = result.SNRatio;
                        noiseRegionUsed = result.NoiseRegionUsed;
                        recalculated = true;
                        calculatedResult = result;
                    }
                    else
                    {
                        skipReason = result?.ErrorMessage ?? "SNR calculation failed.";
                    }
                }
            }

            // This is the event that actually drives the graph's own Output Display and the
            // Generate Report PDF (ChromatogramPlotViewModel.OnSNRCalculated / SNRSignalValue /
            // SNRatio / etc.) — publishing only SNRMethodAppliedEvent (below, for the Settings SNR
            // tab) was not enough, since that tab is a separate view model the graph/report never
            // read from. Only published on a real recalculation, matching what the manual
            // "Calculate S/N" button does (it doesn't publish on failure either, leaving whatever
            // was last shown in place).
            if (recalculated && calculatedResult != null)
            {
                _eventAggregator?.GetEvent<SNRCalculatedEvent>().Publish(calculatedResult);
            }

            _eventAggregator?.GetEvent<SNRMethodAppliedEvent>().Publish(new SNRMethodAppliedArgs
            {
                SignalDefinition = settings.SignalDefinition,
                NoiseDefinition = settings.NoiseDefinition,
                Multiplier = settings.Multiplier,
                IsAutomaticDetection = settings.IsAutomaticDetection,
                DesiredLength = settings.DesiredLength,
                MinimumLength = settings.MinimumLength,
                SpecificTimeRange = settings.SpecificTimeRange,
                AutoCompute = settings.AutoCompute,
                SignalValue = signalValue,
                NoiseValue = noiseValue,
                SNRatio = snRatio,
                NoiseRegionUsed = noiseRegionUsed
            });

            if (recalculated)
            {
                MessageBox.Show(
                    $"SNR calculated for this file:\nSignal: {signalValue:0.###}\nNoise: {noiseValue:0.###}\nS/N Ratio: {snRatio:0.###}",
                    "Apply Saved Method — SNR", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                log.Warn("Apply Saved Method: SNR not recalculated — " + skipReason);
                MessageBox.Show(
                    "SNR was not recalculated for this file: " + skipReason,
                    "Apply Saved Method — SNR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplyNistLibraryMethod(NistLibraryMethodSettings settings)
        {
            if (settings == null)
                return;

            NistLibrarySettings.Default.NistExecutable = settings.NistExecutablePath;
            NistLibrarySettings.Default.FirstLocatorFile = settings.FirstLocatorFile;
            NistLibrarySettings.Default.SecondLocatorFile = settings.SecondLocatorFile;
            NistLibrarySettings.Default.WorkingDir = settings.WorkingDir;
            NistLibrarySettings.Default.Save();
        }

        private void OnOpenInNist(object obj)
        {
            if (Chromatogram?.CurrentSpectrum == null) return;
            try
            {
                var handler = new NistLibrarySearchHandler();
                var spectrum = new NistTextSpectrum();

                // "Please Select"/"All" both have ParentIon == null — only a REAL Data Channel
                // pick should scope what gets sent to NIST down to that channel's own mass
                // range; otherwise send everything in the current spectrum, same as before.
                var selectedChannel = SelectedCompoundOption;
                bool hasChannelFilter = selectedChannel?.ParentIon != null;

                var peaks = new System.Collections.Generic.List<ChromExplorer.DataModel.PeakPicking.PeakInfo>();
                var sp = Chromatogram.CurrentSpectrum;
                if (sp?.Points != null)
                {
                    for (int i = 0; i < sp.Points.Length; i++)
                    {
                        if (sp.Points.Yvals[i] <= 0)
                            continue;

                        var mass = sp.Points.Xvals[i];
                        if (hasChannelFilter && (mass < selectedChannel.MFrom || mass > selectedChannel.MTo))
                            continue;

                        peaks.Add(new ChromExplorer.DataModel.PeakPicking.PeakInfo { Position = mass, Intensity = sp.Points.Yvals[i] });
                    }
                }

                spectrum.Init(Chromatogram, peaks);
                handler.InteractiveSearch(spectrum, append: false);

                var channelDesc = hasChannelFilter ? selectedChannel.Label : "All";
                var fileName = string.IsNullOrEmpty(Chromatogram?.Filename) ? "" : Path.GetFileName(Chromatogram.Filename);
                InsertAuditLog("Open in NIST", $"Data Channel: {channelDesc}", fileName);
            }
            catch (Exception ex)
            {
                log.Warn("OpenInNist failed", ex);
            }
        }

        private void OnDecrementSelectedIndex(object obj)
        {
            if (Chromatogram != null)
            {
                if (Chromatogram.SelectedIndex > 0)
                    Chromatogram.SelectedIndex--;

                //if(Chromatogram.SelectedIndex < 0)
                //{
                //    Chromatogram.SelectedIndex = 0;
                //}
            }
        }

        // TODO [Log]
        private void OnIncrementSelectedIndex(object obj)
        {
            if (Chromatogram != null)
            {
                if (Chromatogram.SelectedIndex < Chromatogram.Points.Length - 1)
                    Chromatogram.SelectedIndex++;

                //if(Chromatogram.SelectedIndex >= Chromatogram.Points.Length)
                //{
                //    Chromatogram.SelectedIndex = Chromatogram.Points.Length - 1;
                //}
                /*var oldValue = Chromatogram.SelectedIndex;
                var newValue = oldValue++;

                if (oldValue < 0 || oldValue >= Chromatogram.Points.Length)
                    newValue = Chromatogram.Points.Length - 1;

                Chromatogram.SelectedIndex = newValue;
                 * */
            }
        }

        private void OnTestCommandExecute(object args)
        {
            log.Debug("OnTestCommandExecute(object args) called");
        }

        // TODO [Log]
        private void OnProviderChanged(Type t)
        {
            //UpdateChrom();
        }

        // TODO [Log]
        private void OnChromatogramChanged(GenericChromatogram obj)
        {
            UpdateChrom();
        }

        private void UpdateChrom()
        {
            if (_factory.Current != null && _factory.Current.Chromatogram != null)
            {
                Chromatogram = _factory.Current.Chromatogram;

                MinIndex = 0;
                MaxIndex = Chromatogram.ChromPoints.Count - 1;

                Chromatogram.SelectedIndex = 0;

                // Auto-run the same "select the whole chromatogram" step a user would otherwise
                // have to manually drag-select — this is what actually triggers TIC integration
                // (GenericChromatogram.SelectedIndexes setter) and the full aggregate spectrum,
                // instead of leaving the view showing only the first single scan until the user
                // does that themselves.
                //
                // Sized to Points.Length (post-processed), not ChromPoints.Count (pre-processing)
                // — post-processing (baseline removal, smoothing, etc.) can trim the point count,
                // and building this range from the larger pre-processing count would index
                // Points.Yvals out of bounds inside UpdateSIC.
                var selectableCount = Chromatogram.Points?.Length ?? 0;

                if (selectableCount > 0)
                {
                    Chromatogram.SelectedIndexes = Enumerable.Range(0, selectableCount).ToList();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // One entry in the compound (Parent Ion) dropdown. ParentIon == null covers both the
    // "Please Select" placeholder and "All" — they're told apart by IsSelectable.
    public class CompoundOption
    {
        public double? ParentIon { get; set; }
        public string Label { get; set; }
        public bool IsSelectable { get; set; } = true;
        public double MFrom { get; set; }
        public double MTo { get; set; }

        public override string ToString()
        {
            return Label;
        }
    }
}