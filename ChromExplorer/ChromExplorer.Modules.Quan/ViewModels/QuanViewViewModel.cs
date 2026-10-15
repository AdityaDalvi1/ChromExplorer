using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows;
using ChromExplorer.Modules.Quan.Model;
using ChromExplorer.Modules.Quan.Services;
using log4net;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Xceed.Wpf.Toolkit;

namespace ChromExplorer.Modules.Quan.ViewModels
{
    [Export(typeof(QuanViewViewModel))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class QuanViewViewModel : INotifyPropertyChanged
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly QuanService _quanService;
        private ScatterSeries _calibrationPoints;
        private LineSeries _calibrationCurve;
        private LinearAxis _xAxis;
        private ScatterSeries _calculatedPoints;
        private bool _isErrorBarsEnabled;
        private readonly List<Series> _errorBarSeries;
        private bool _hasUnsavedChanges;
        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        [ImportingConstructor]
        public QuanViewViewModel(QuanService quanService)
        {
            _quanService = quanService;
            Log.Debug("QuanViewViewModel created");

            Peaks = new ObservableCollection<ChromatogramPeakInfo>(_quanService.Peaks);
            Peaks.CollectionChanged += Peaks_CollectionChanged;

            _quanService.ChromatogramPeakAdded += quanService_ChromatogramPeakAdded;
            _quanService.PropertyChanged += QuanServiceOnPropertyChanged;

            DeleteSelectedCommand = new DelegateCommand<ChromatogramPeakInfo>(OnDeleteSelected);
            SaveFileCommand = new DelegateCommand(OnSaveFile);
            OpenFileCommand = new DelegateCommand(OnOpenFile);
            NewFileCommand = new DelegateCommand(OnNewFile);
            CalculateCommand = new DelegateCommand(OnCalculate);
            ExportFileCommand = new DelegateCommand(OnExportFile);

            Model = new PlotModel();
            _errorBarSeries = new List<Series>();
            SetupPlot();
            UpdatePlot();

        }

        private void Peaks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _hasUnsavedChanges = true;
        }

        private void InsertLog(string action, string description)
        {
            try
            {
                int userId = 0;
                if (Application.Current?.Properties?["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    string sql = @"INSERT INTO AuditLogM
                          (action, description, is_active, inserted_by, inserted_date)
                          VALUES
                          (@action, @desc, 1, @uid, datetime('now','localtime'))";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@desc", description);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        // "New" starts a fresh, empty Quan list. If there are unsaved changes it asks to save
        // first (with a chance to cancel the save dialog and abort New entirely), then asks a
        // second time to confirm actually clearing the list, per the requested two-step flow.
        private void OnNewFile()
        {
            if (_hasUnsavedChanges)
            {
                var saveChoice = System.Windows.MessageBox.Show(
                    "You have unsaved changes in the current list. Do you want to save them first?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (saveChoice == MessageBoxResult.Cancel)
                {
                    InsertLog("Quan New List", "User selected 'Cancel' at the unsaved-changes prompt — new list not opened");
                    return;
                }

                if (saveChoice == MessageBoxResult.Yes)
                {
                    InsertLog("Quan New List", "User selected 'Yes' at the unsaved-changes prompt — saving before opening a new tab");

                    var dlg = new SaveFileDialog { DefaultExt = ".mqd", Filter = "MassTech Quan Data (.mqd)|*.mqd" };
                    if (!ChromExplorer.Infrastructure.FileStorage.ShowSaveDialog(dlg, ChromExplorer.Infrastructure.StorageCategory.Quantitation, "Quan data file"))
                    {
                        InsertLog("Quan New Tab", "Cancelled — user did not complete the save dialog");
                        return;
                    }

                    if (!ChromExplorer.Infrastructure.FileSaveGuard.CanSaveTo(dlg.FileName, "Quan data file"))
                        return;

                    _quanService?.SaveData(dlg.FileName);
                    InsertLog("Quan List Saved", $"Saved to: {dlg.FileName}");
                    _hasUnsavedChanges = false;
                }
                else
                {
                    InsertLog("Quan New List", "User selected 'No' at the unsaved-changes prompt — proceeding without saving");
                }
            }

            var confirm = System.Windows.MessageBox.Show(
                "Are you sure you want to open a new tab? Any data not saved will be lost.",
                "Confirm New List",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                InsertLog("Quan New Tab", "User selected 'No' at the final confirmation — new tab not opened");
                return;
            }

            InsertLog("Quan New Tab", "User selected 'Yes' at the final confirmation — opening a new tab");

            _quanService?.Clear();
            Peaks.Clear();
            _hasUnsavedChanges = false;
            UpdatePlot();

            InsertLog("Quan New Tab", "New (empty) Quan tab open.");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsErrorBarsEnabled
        {
            get { return _isErrorBarsEnabled; }
            set
            {
                if (_isErrorBarsEnabled == value)
                    return;

                if (value)
                {
                    if (!ValidateErrorBars())
                    {
                        _isErrorBarsEnabled = false;
                        OnPropertyChanged(nameof(IsErrorBarsEnabled));
                        return;
                    }
                }

                _isErrorBarsEnabled = value;
                OnPropertyChanged(nameof(IsErrorBarsEnabled));
                UpdatePlot();
            }
        }

        private bool ValidateErrorBars()
        {
            var calibrants = _quanService.Peaks
                .Where(p => p != null &&
                            p.ExpectedAmount != null &&
                            p.CalculatedAmount != null &&
                            string.Equals((p.SampleType ?? string.Empty).Trim(), "Calibrant", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var hasReplicateGroup = calibrants
                .GroupBy(p => p.ExpectedAmount.Value)
                .Any(g => g.Count() >= 2);

            if (!hasReplicateGroup)
            {
                System.Windows.MessageBox.Show("Error bars require the same concentration loaded from 2 or more files", "Error Bars",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }

        private void OnExportFile()
        {
            var dlg = new SaveFileDialog { DefaultExt = ".csv", Filter = "Comma delimited (.csv)|*.csv" };

            var result = ChromExplorer.Infrastructure.FileStorage.ShowSaveDialog(dlg, ChromExplorer.Infrastructure.StorageCategory.Exports, "Quan CSV export");

            if (result == true)
            {
                if (!ChromExplorer.Infrastructure.FileSaveGuard.CanSaveTo(dlg.FileName, "Quan CSV export"))
                    return;

                _quanService?.ExportCsvData(dlg.FileName);
                InsertLog("Quan Data Exported", $"Exported to: {dlg.FileName}");
            }
        }


        private void OnCalculate()
        {
            _quanService.Recalculate();
        }

        private void QuanServiceOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(_quanService.FitResult))
            {
                UpdatePlot();
            }
        }

        public PlotModel Model { get; }

        public DelegateCommand<ChromatogramPeakInfo> DeleteSelectedCommand { get; private set; }
        public DelegateCommand SaveFileCommand { get; private set; }
        public DelegateCommand OpenFileCommand { get; private set; }
        public DelegateCommand NewFileCommand { get; private set; }
        public DelegateCommand CalculateCommand { get; private set; }
        public DelegateCommand ExportFileCommand { get; private set; }

        public ObservableCollection<ChromatogramPeakInfo> Peaks { get; set; }

        private void SetupPlot()
        {
            Model.Title = "Calibration curve";


            _xAxis = new LinearAxis
            {
                Title = "Area",
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineThickness = 1

            };


            var yAxis = new LinearAxis
            {
                Title = "Amount",
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineThickness = 1
            };

            Model.Axes.Add(_xAxis);
            Model.Axes.Add(yAxis);

            _calibrationPoints = new ScatterSeries();
            Model.Series.Add(_calibrationPoints);


            _calculatedPoints = new ScatterSeries();
            _calculatedPoints.MarkerType = MarkerType.Circle;
            _calculatedPoints.MarkerFill = OxyColors.Blue;
            Model.Series.Add(_calculatedPoints);


            _calibrationCurve = new LineSeries();
            Model.Series.Add(_calibrationCurve);


        }

        void UpdatePlot()
        {
            if (!double.IsNaN(_quanService.XMax) &&
                !double.IsNaN(_quanService.XMin) &&
                !double.IsInfinity(_quanService.XMax) &&
                !double.IsInfinity(_quanService.XMin))
            {
                var extraSpace = (_quanService.XMax - _quanService.XMin) * 0.1;
                _xAxis.Minimum = _quanService.XMin - extraSpace;
                _xAxis.Maximum = _quanService.XMax + extraSpace;
            }

            _calibrationPoints.Points.Clear();
            _calculatedPoints.Points.Clear();

            ClearErrorBars();

            foreach (var peak in _quanService.Peaks)
            {
                var sampleType = (peak.SampleType ?? string.Empty).Trim();
                if (peak.ExpectedAmount != null && string.Equals(sampleType, "Calibrant", StringComparison.OrdinalIgnoreCase))
                    _calibrationPoints.Points.Add(new ScatterPoint(peak.Area, peak.ExpectedAmount.Value));

                //if(peak.CalculatedAmount != null && peak.ExpectedAmount == null)
                //    _calculatedPoints.Points.Add(new ScatterPoint(peak.Area, peak.CalculatedAmount.Value) );
            }

            //if(_calibrationCurve != null && Model.Series.Contains(_calibrationCurve))
            //    Model.Series.Remove(_calibrationCurve);

            // Model.InvalidatePlot(true);

            _calibrationCurve.Points.Clear();
            if (_quanService.FitResult != null)
            {
                _quanService.UpdateRange();

                int points = 100;
                double to = _quanService.XMax;
                double from = _quanService.XMin;


                if (!double.IsNaN(_xAxis.Minimum) && !double.IsNaN(_xAxis.Maximum))
                {
                    from = _xAxis.Minimum;
                    to = _xAxis.Maximum;
                }

                double step = (to - from) / points;

                for (int i = 0; i < points; i++)
                {
                    double x = from + step * i;
                    double y = _quanService.FitResult.GetY(x);
                    _calibrationCurve.Points.Add(new DataPoint(x, y));
                }
                //_calibrationCurve = new FunctionSeries(_quanService.FitResult.GetY, _xAxis.ActualMinimum, _xAxis.ActualMaximum, 100, "Fit"); 
                //Model.Series.Add(_calibrationCurve);
            }

            if (IsErrorBarsEnabled)
            {
                AddErrorBars();
            }

            Model.Subtitle = string.Empty;
            if (_quanService.FitResult != null)
            {
                var sign = _quanService.FitResult.Offset >= 0 ? "+" : "-";
                Model.Subtitle =
                    $"y = {_quanService.FitResult.Slope} * x {sign} {Math.Abs(_quanService.FitResult.Offset)}, R^2 = {_quanService.FitResult.RSquared:0.000}";

            }

            Model.InvalidatePlot(true);
        }

        private void ClearErrorBars()
        {
            if (_errorBarSeries == null)
                return;

            if (_errorBarSeries.Count == 0)
                return;

            foreach (var s in _errorBarSeries)
            {
                if (Model.Series.Contains(s))
                    Model.Series.Remove(s);
            }

            _errorBarSeries.Clear();
        }

        private void AddErrorBars()
        {
            var calibrants = _quanService.Peaks
                .Where(p => p != null &&
                            p.ExpectedAmount != null &&
                            p.CalculatedAmount != null &&
                            string.Equals((p.SampleType ?? string.Empty).Trim(), "Calibrant",
                                StringComparison.OrdinalIgnoreCase))
                .ToList();

            // ✅ Only groups with 2 or more files - NO fallback fake SD
            var groups = calibrants
                .GroupBy(p => p.ExpectedAmount.Value)
                .Where(g => g.Count() >= 2)
                .ToList();

            if (groups.Count == 0)
                return;

            var barColor = OxyColors.LimeGreen;

            foreach (var g in groups)
            {
                var calcValues = g.Select(p => p.CalculatedAmount.Value).ToList();

                // ✅ Real SD only - no fallback
                var mean = calcValues.Average();
                var sd = Math.Sqrt(
                    calcValues.Sum(v => (v - mean) * (v - mean)) / calcValues.Count);

                var capWidth = (_quanService.XMax - _quanService.XMin);
                if (double.IsNaN(capWidth) || double.IsInfinity(capWidth) || capWidth <= 0)
                    capWidth = 1;
                capWidth *= 0.01;

                foreach (var p in g)
                {
                    var x = p.Area;
                    var y = p.ExpectedAmount.Value;

                    var yLow = y - sd;
                    var yHigh = y + sd;

                    var bar = new LineSeries
                    {
                        Color = barColor,
                        StrokeThickness = 2,
                        LineStyle = LineStyle.Solid
                    };
                    bar.Points.Add(new DataPoint(x, yLow));
                    bar.Points.Add(new DataPoint(x, yHigh));

                    var capLow = new LineSeries { Color = barColor, StrokeThickness = 2 };
                    capLow.Points.Add(new DataPoint(x - capWidth, yLow));
                    capLow.Points.Add(new DataPoint(x + capWidth, yLow));

                    var capHigh = new LineSeries { Color = barColor, StrokeThickness = 2 };
                    capHigh.Points.Add(new DataPoint(x - capWidth, yHigh));
                    capHigh.Points.Add(new DataPoint(x + capWidth, yHigh));

                    Model.Series.Add(bar);
                    Model.Series.Add(capLow);
                    Model.Series.Add(capHigh);

                    _errorBarSeries.Add(bar);
                    _errorBarSeries.Add(capLow);
                    _errorBarSeries.Add(capHigh);
                }
            }
        }

        private void OnOpenFile()
        {
            var dlg = new OpenFileDialog { DefaultExt = ".mqd", Filter = "MassTech Quan Data (.mqd)|*.mqd" };

            var result = ChromExplorer.Infrastructure.FileStorage.ShowOpenDialog(dlg, ChromExplorer.Infrastructure.StorageCategory.Quantitation, "Quan data file");

            if (result == true)
            {
                _quanService?.LoadData(dlg.FileName);
                Peaks.Clear();
                if (_quanService?.Peaks != null)
                    foreach (var peak in _quanService.Peaks)
                    {
                        Peaks.Add(peak);
                    }
                _hasUnsavedChanges = false;
                InsertLog("Quan List Opened", $"Opened: {dlg.FileName}");
            }

            UpdatePlot();
        }

        private void OnSaveFile()
        {
            var dlg = new SaveFileDialog { DefaultExt = ".mqd", Filter = "MassTech Quan Data (.mqd)|*.mqd" };

            var result = ChromExplorer.Infrastructure.FileStorage.ShowSaveDialog(dlg, ChromExplorer.Infrastructure.StorageCategory.Quantitation, "Quan data file");

            if (result == true)
            {
                if (!ChromExplorer.Infrastructure.FileSaveGuard.CanSaveTo(dlg.FileName, "Quan data file"))
                    return;

                _quanService?.SaveData(dlg.FileName);
                _hasUnsavedChanges = false;
                InsertLog("Quan List Saved", $"Saved to: {dlg.FileName}");
            }
        }

        private void OnDeleteSelected(ChromatogramPeakInfo peak)
        {
            if (peak == null)   
                return;

            _quanService.Remove(peak);
            Peaks.Remove(peak);
            UpdatePlot();
        }

        private void quanService_ChromatogramPeakAdded(object sender, ChromatogramPeakAddedEventArgs e)
        {
            Peaks.Add(e.Peak);
            UpdatePlot();
        }
    }
}