using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.Formats;
using ChromExplorer.DataModel.PeakPicking;
using ChromExplorer.DataModel.Properties;
using ChromExplorer.NistLibrary;
using ChromExplorer.Plotter;
using ChromExplorer.Plotter.Markers;
using ChromExplorer.Services.PickPeakerService.Services;
using log4net;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Win32;

namespace ChromExplorer.Modules.Viewer.ViewModels
{
    public class SpectrumPlotViewModel : INotifyPropertyChanged
    {
        // ReSharper disable once InconsistentNaming
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        private bool _multiSelectEnabled;
        private SimplePlotter _plotter;
        private DataRect _visible;
        private readonly GenericChromatogram _chrom;
        private readonly bool _excludeParentIonFromWileyList = true;
        private readonly MarkerCollection _markers;
        private readonly double _msTolerance = 0.1;
        private readonly PeakPicker _peakPicker;
        private readonly PeakPickerService _peakPickerService;

        public SpectrumPlotViewModel(GenericChromatogram chrom, PeakPickerService peakPickerService)
        {
            _peakPickerService = peakPickerService;
            _markers = new MarkerCollection();
            _chrom = chrom;

            ClearSelectionCommand = new DelegateCommand<object>(OnClearSelection);
            ExportSpectrumCommand = new DelegateCommand<object>(OnExportSpectrum);
            ExportPeakListCommand = new DelegateCommand<object>(OnExportPeakList);
            ExportNistTextCommand = new DelegateCommand<object>(OnExportNistText);
            CopyMascotCommand = new DelegateCommand<object>(OnCopyMascot);
            CopyWileyCommand = new DelegateCommand<object>(OnCopyWiley);
            NistLibrarySearchCommand = new DelegateCommand<object>(OnNistLibrarySearch);


            PropertyChanged += SpectrumPlotViewModelPropertyChanged;

            _chrom.PropertyChanged += ChromatogramPropertyChanged;

            _peakPicker = new PeakPicker();
        }

        private void OnNistLibrarySearch(object obj)
        {
            log.Debug("OnNistLibrarySearch called");

            var handler = new NistLibrarySearchHandler();
            var spectrum = new NistTextSpectrum();
            spectrum.Init(Chromatogram, UpdatePeakMarkers(Visible));

            handler.InteractiveSearch(spectrum, obj != null);

        }

        private void InsertLog(string action, string description, string parameter, string fileName = "")
        {
            int userId = 0;
            if (Application.Current?.Properties?["UserId"] != null)
                userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

            using (var con = new SQLiteConnection(_connectionString))
            {
                con.Open();

                var sql = @"INSERT INTO AuditLogM
                          (action, description, parameter, file_name, is_active, inserted_by, inserted_date)
                          VALUES
                          (@action, @desc, @param, @fileName, 1, @uid, datetime('now','localtime'))";

                using (var cmd = new SQLiteCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@action", action);
                    cmd.Parameters.AddWithValue("@desc", description);
                    cmd.Parameters.AddWithValue("@param", parameter ?? string.Empty);
                    cmd.Parameters.AddWithValue("@fileName", fileName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void OnExportNistText(object obj)
        {
            if (Chromatogram == null || Chromatogram.CurrentSpectrum == null)
            {
                return;
            }

            try
            {
                var dlg = new SaveFileDialog
                {
                    DefaultExt = ".msp",
                    Filter = "NIST Text (.msp)|*.msp",
                    InitialDirectory = Path.GetDirectoryName(Chromatogram.Filename),
                    FileName = Path.GetFileNameWithoutExtension(Chromatogram.Title)
                };

                var result = ChromExplorer.Infrastructure.FileStorage.ShowSaveDialog(dlg, ChromExplorer.Infrastructure.StorageCategory.Exports, "spectrum export");

                if (result == true)
                {
                    if (!ChromExplorer.Infrastructure.FileSaveGuard.CanSaveTo(dlg.FileName, "NIST text export"))
                        return;

                    Chromatogram.SetAppStatus(true, "Exporting...");
                    using (TextWriter tw = new StreamWriter(dlg.FileName))
                    {
                        var exporter = new NistTextSpectrum();
                        exporter.Init(Chromatogram, UpdatePeakMarkers(Visible));
                        exporter.Write(tw);
                    }

                    InsertLog("NIST Text", $"NIST Text Exported | Saved to: {dlg.FileName}", "Spectrum : " + Chromatogram.Title, Path.GetFileName(dlg.FileName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Chromatogram.SetAppStatus(false, "Ready");
            }
        }

        public GenericChromatogram Chromatogram
        {
            get { return _chrom; }
        }

        public MarkerCollection Markers
        {
            get { return _markers; }
        }

        public bool MultiSelectEnabled
        {
            get { return _multiSelectEnabled; }
            set
            {
                if (value.Equals(_multiSelectEnabled)) return;
                _multiSelectEnabled = value;
                OnPropertyChanged("MultiSelectEnabled");
            }
        }

        public DelegateCommand<object> ClearSelectionCommand { get; set; }
        public DelegateCommand<object> ExportSpectrumCommand { get; set; }
        public DelegateCommand<object> ExportPeakListCommand { get; set; }
        public DelegateCommand<object> ExportNistTextCommand { get; set; }
        public DelegateCommand<object> CopyMascotCommand { get; set; }
        public DelegateCommand<object> CopyWileyCommand { get; private set; }
        public DelegateCommand<object> NistLibrarySearchCommand { get; set; }
        

        // Every m/z peak detected in the CURRENT spectrum — lets a reviewer pick a specific mass
        // from a dropdown instead of having to drag-select it on the plot by hand. Selecting one
        // zooms the mass spectrum plot itself in on that mass (see ApplyMassSelection below) —
        // deliberately scoped to the spectrum view only, not the chromatogram above it. Same
        // disabled-placeholder pattern as Data Channel's own CompoundOption/"Please Select"
        // (ChromatogramViewViewModel.cs) — reuses that same ComboBoxItemStyle (binds IsSelectable
        // generically) in the XAML.
        private readonly ObservableCollection<MassOption> _peakMassOptions = new ObservableCollection<MassOption>();
        public ObservableCollection<MassOption> PeakMassOptions => _peakMassOptions;

        private MassOption _selectedMassOption;

        public MassOption SelectedMassOption
        {
            get { return _selectedMassOption; }
            set
            {
                if (Equals(value, _selectedMassOption)) return;
                _selectedMassOption = value;
                OnPropertyChanged("SelectedMassOption");
                ApplyMassSelection(value?.Mass);
            }
        }

        // Zooms the MASS SPECTRUM plot itself (this view's own Visible window) in on the chosen
        // mass, then extracts that mass as an EIC on the top plot (see ExtractEicForMass), which
        // auto-integrates the mass's chromatographic peak.
        private void ApplyMassSelection(double? mass)
        {
            if (!mass.HasValue || Plotter == null)
                return;

            // SimplePlotter.Visible's own change callback (the thing that would normally apply
            // an externally-set range) is commented out in SimplePlotter.xaml.cs — setting
            // Visible from here does nothing. SetRange is the real, working public entry point
            // (same one the "Rescale" menu item and Data Channel switching already use).
            const double halfWindow = 10.0;
            Plotter.SetRange(mass.Value - halfWindow, mass.Value + halfWindow);

            ExtractEicForMass(mass.Value);
        }

        // Rebuilds the top plot as the EIC of the chosen mass, the same thing a drag-select of that
        // mass on the spectrum does. The resulting "Points" change makes ChromatogramPlotViewModel
        // re-run its automatic peak integration, so the peak(s) of this mass get RT/Area labels.
        private void ExtractEicForMass(double mass)
        {
            const double eicHalfWidth = 0.4;

            var xvals = Chromatogram?.CurrentSpectrum?.Points?.Xvals;
            if (xvals == null)
                return;

            var indexes = new List<int>();
            for (int i = 0; i < xvals.Length; i++)
            {
                if (xvals[i] >= mass - eicHalfWidth && xvals[i] <= mass + eicHalfWidth)
                    indexes.Add(i);
            }

            if (indexes.Count > 0)
                Chromatogram.SelectedSpectrumIndexes = indexes;
        }

        public DataRect Visible
        {
            get { return _visible; }
            set
            {
                if (Equals(value, _visible)) return;
                _visible = value;
                OnPropertyChanged("Visible");
            }
        }

        public SimplePlotter Plotter
        {
            get { return _plotter; }
            set
            {
                if (Equals(value, _plotter)) return;
                _plotter = value;
                OnPropertyChanged("Plotter");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnCopyWiley(object obj)
        {
            var sb = new StringBuilder();
            var sorted = Markers.OrderByDescending(x => x.Position.Y);
            foreach (var marker in sorted)
            {
                var skip = _excludeParentIonFromWileyList &&
                           Math.Abs(marker.Position.X - Chromatogram.CurrentSpectrum.ParentIon) < _msTolerance;

                if (!skip)
                {
                    sb.AppendFormat("{0:0.0000}\t{1:0.0000}", marker.Position.X, marker.Position.Y);
                    sb.AppendLine();
                }
            }


            Clipboard.SetData(DataFormats.Text, sb.ToString());
        }

        private void OnCopyMascot(object exactMasses)
        {
            var exact = false;

            Boolean.TryParse(exactMasses.ToString(), out exact);


            Clipboard.SetData(DataFormats.Text, GetMascotString(exact));
        }

        private string GetMascotString(bool exact)
        {
            if (Chromatogram == null || Chromatogram.CurrentSpectrum == null)
            {
                return "BEGIN IONS\nPepmass=0.0\nEND IONS";
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("BEGIN IONS");
                sb.AppendFormat("Pepmass={0}\r\n", Chromatogram.CurrentSpectrum.ParentIon.ToString("F2"));
                if (exact)
                {
                    for (var i = 0; i < Chromatogram.CurrentSpectrum.Points.Length; i++)
                    {
                        if (Chromatogram.CurrentSpectrum.Points.Yvals[i] > 0)
                        {
                            sb.AppendFormat("{0}\t{1}\r\n", Chromatogram.CurrentSpectrum.Points.Xvals[i].ToString("F2")
                                , Chromatogram.CurrentSpectrum.Points.Yvals[i].ToString("F2"));
                        }
                    }
                }
                else
                {
                    var firstMass = -1;
                    var firstInt = 0.0;
                    for (var i = 0; i < Chromatogram.CurrentSpectrum.Points.Length; i++)
                    {
                        if (Chromatogram.CurrentSpectrum.Points.Yvals[i] > 0)
                        {
                            var mass = (int) Math.Round(Chromatogram.CurrentSpectrum.Points.Xvals[i]);

                            if (firstMass == -1)
                            {
                                firstMass = mass;
                                firstInt = Chromatogram.CurrentSpectrum.Points.Yvals[i];
                            }
                            else
                            {
                                if (mass != firstMass)
                                {
                                    sb.AppendFormat("{0}\t{1}\r\n", firstMass, firstInt.ToString("F2"));
                                    firstMass = mass;
                                    firstInt = Chromatogram.CurrentSpectrum.Points.Yvals[i];
                                }
                                else
                                {
                                    firstInt += Chromatogram.CurrentSpectrum.Points.Yvals[i];
                                }
                            }
                        }

                        if (i == Chromatogram.CurrentSpectrum.Points.Length - 1 && firstMass > 0)
                        {
                            sb.AppendFormat("{0}\t{1}\r\n", firstMass, firstInt.ToString("F2"));
                        }
                    }
                }

                sb.AppendLine("END IONS");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return "BEGIN IONS\nPepmass=0.0\nEND IONS";
        }

        private void OnExportSpectrum(object obj)
        {
            if (Chromatogram == null || Chromatogram.CurrentSpectrum == null)
            {
                return;
            }

            try
            {
                var dlg = new SaveFileDialog
                {
                    DefaultExt = ".dat",
                    Filter = "ASCII Spectrum Data (.dat)|*.dat",
                    InitialDirectory = Path.GetDirectoryName(Chromatogram.Filename),
                    FileName = Path.GetFileNameWithoutExtension(Chromatogram.Title)
                };

                var result = ChromExplorer.Infrastructure.FileStorage.ShowSaveDialog(dlg, ChromExplorer.Infrastructure.StorageCategory.Exports, "spectrum export");

                if (result == true)
                {
                    if (!ChromExplorer.Infrastructure.FileSaveGuard.CanSaveTo(dlg.FileName, "spectrum export"))
                        return;

                    Chromatogram.SetAppStatus(true, "Exporting...");
                    using (TextWriter tw = new StreamWriter(dlg.FileName))
                    {
                        for (var i = 0; i < Chromatogram.CurrentSpectrum.Points.Length; i++)
                        {
                            tw.WriteLine("{0}\t{1}", Chromatogram.CurrentSpectrum.Points.Xvals[i]
                                , Chromatogram.CurrentSpectrum.Points.Yvals[i]);
                        }

                        tw.Flush();
                    }

                    InsertLog("Spectrum", $"Spectrum Exported | Saved to: {dlg.FileName}", "Spectrum : " + Chromatogram.Title, Path.GetFileName(dlg.FileName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Chromatogram.SetAppStatus(false, "Ready");
            }
        }

        private void OnExportPeakList(object obj)
        {
            if (Chromatogram == null || Chromatogram.CurrentSpectrum == null || Markers == null)
            {
                return;
            }

            try
            {
                var dlg = new SaveFileDialog
                {
                    DefaultExt = ".pkl",
                    Filter = "Peak List Data (.pkl)|*.pkl",
                    InitialDirectory = Path.GetDirectoryName(Chromatogram.Filename),
                    FileName = Path.GetFileNameWithoutExtension(Chromatogram.Title)
                };

                var result = ChromExplorer.Infrastructure.FileStorage.ShowSaveDialog(dlg, ChromExplorer.Infrastructure.StorageCategory.Exports, "spectrum export");

                if (result == true)
                {
                    if (!ChromExplorer.Infrastructure.FileSaveGuard.CanSaveTo(dlg.FileName, "peak list export"))
                        return;

                    Chromatogram.SetAppStatus(true, "Exporting...");
                    using (TextWriter tw = new StreamWriter(dlg.FileName))
                    {
                        var sorted = Markers.OrderBy(x => x.Position.X);
                        foreach (var marker in sorted)
                        {
                            tw.WriteLine("{0}\t{1}\t{2}", marker.Position.X, marker.Position.Y, marker.Text);
                        }

                        tw.Flush();
                    }

                    InsertLog("Peak List", $"Peak List Exported | Saved to: {dlg.FileName}", "Spectrum : " + Chromatogram.Title, Path.GetFileName(dlg.FileName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Chromatogram.SetAppStatus(false, "Ready");
            }
        }

        private void OnClearSelection(object obj)
        {
            //if(!MultiSelectEnabled)
            {
                _chrom.SelectedSpectrumIndexes = null;
            }
        }

        private void SpectrumPlotViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Visible")
            {
                UpdateForNewVisible(Visible);
            }
        }

        private void ChromatogramPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Resetting zoom here (rather than only on this ViewModel's own construction/
            // DataContextChanged in SpectrumPlotView) is what actually catches a Data Channel
            // switch reliably: GenericChromatogram.CurrentSpectrum only gets its real new value
            // once the reload (ReloadForCompound -> provider.BindToFile) genuinely finishes,
            // which can happen after this ViewModel/its DataContextChanged already fired — a
            // fixed dispatcher-priority deferral there isn't a reliable enough proxy for "the new
            // spectrum's data has actually arrived."
            //
            // This PropertyChanged notification isn't guaranteed to arrive on the UI thread —
            // confirmed by a live crash here (InvalidOperationException: "The calling thread
            // cannot access this object because a different thread owns it", raised from a
            // background worker thread while reloading for a Data Channel switch). Plotter is a
            // WPF control, so the actual Restore() call must be marshaled onto the UI thread.
            if (e.PropertyName != "CurrentSpectrum" || Plotter == null)
                return;

            // Same file, same scan(s), just re-processed (post-processing / "Apply Saved Method"):
            // the plotter already keeps its X range when its data is swapped in place, so skipping
            // the reset here is what keeps the user's zoomed-in mass view.
            if (_chrom != null && _chrom.IsRebuildingSpectrumInPlace)
                return;

            if (Plotter.CheckAccess())
                Plotter.Restore();
            else
                Plotter.Dispatcher.BeginInvoke(new Action(() => Plotter?.Restore()));
        }

        // TODO [Log(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error)]
        private void UpdateForNewVisible(DataRect visible)
        {
            UpdatePeakMarkers(visible);
        }

        private void DirtyUpdatePeakMarkers(DataRect visible)
        {
            Markers.Clear();

            if (visible == null)
                return;

            if (Chromatogram == null)
                return;

            if (Chromatogram.CurrentSpectrum == null)
                return;

            var setting = new PeakPickingSettings
            {
                MinPeakHalfSize = 5,
                PeakHalfSize = 10
            };

            _peakPicker.Settings = setting;

            Markers.Clear();

            var xvals = Chromatogram.CurrentSpectrum.Points.Xvals;
            var yvals = Chromatogram.CurrentSpectrum.Points.Yvals;


            var percentage = true;

            //if (SpectrumViewerSettings.Default.ThresholdType == ThresholdTypeEnum.Percent)
            //{
            //    percentage = true;
            //}

            var min = double.NegativeInfinity;
            var max = double.PositiveInfinity;

            if (Visible != null)
            {
                min = Visible.XMin;
                max = Visible.XMax;
            }

            var peaks = _peakPicker.FindPeaks<CentroidPeakAnalyzer>(xvals, yvals, min, max, 10, percentage);


            var count = 0;
            foreach (var p in peaks)
            {
                Markers.Add(new SimpleMarker(new Point(p.Position, p.Intensity), string.Format("{0:0.00}", p.Position))
                {
                    Placement = _peakPickerService.LabelPlacement,
                    Orientation = _peakPickerService.LabelOrientation
                }
                    );
                count++;

                if (count > 100)
                    break;
            }
        }

        private List<PeakInfo> UpdatePeakMarkers(DataRect visible)
        {
            Markers.Clear();
            _peakMassOptions.Clear();

            if (_peakPickerService == null)
                return new List<PeakInfo>();

            if (Chromatogram == null)
                return new List<PeakInfo>();

            if (Chromatogram.CurrentSpectrum == null)
                return new List< PeakInfo > ();

            var min = double.NegativeInfinity;
            var max = double.PositiveInfinity;

            if (visible != null)
            {
                min = visible.XMin;
                max = visible.XMax;
            }

            var peaks = _peakPickerService.GetPeaks(Chromatogram.CurrentSpectrum.Points, min, max);

            var count = 0;
            foreach (var p in peaks)
            {
                try
                {
                    Markers.Add(new SimpleMarker(new Point(p.Position, p.Intensity),
                        string.Format(_peakPickerService.LabelFormat, p.Position))
                    {
                        Placement = _peakPickerService.LabelPlacement,
                        Orientation = _peakPickerService.LabelOrientation
                    });
                }
                catch (Exception)
                {
                }

                count++;

                if (count > 100)
                    break;
            }

            _peakMassOptions.Add(new MassOption { Mass = null, Label = "Please Select", IsSelectable = false });
            foreach (var p in peaks.OrderBy(pp => pp.Position))
            {
                var mass = Math.Round(p.Position, 2);
                _peakMassOptions.Add(new MassOption { Mass = mass, Label = mass.ToString("0.00") });
            }

            _selectedMassOption = _peakMassOptions[0];
            OnPropertyChanged("SelectedMassOption");

            return peaks;
        }

        public void PerformPeakPicking()
        {
            UpdatePeakMarkers(Visible);

            if (Plotter != null)
                Plotter.Replot();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Same shape as ChromatogramViewViewModel.CompoundOption ("Data Channel") — a disabled
    // "Please Select" placeholder plus one real, selectable entry per detected mass.
    public class MassOption
    {
        public double? Mass { get; set; }
        public string Label { get; set; }
        public bool IsSelectable { get; set; } = true;

        public override string ToString()
        {
            return Label;
        }
    }
}