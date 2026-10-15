using ChromExplorer.Modules.Quan.Model;
using ChromExplorer.Modules.Quan.Properties;
using log4net;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.UI.WebControls;
using System.Windows;
using System.Xml.Serialization;
using Xceed.Wpf.Toolkit;

namespace ChromExplorer.Modules.Quan.Services
{


    [Export(typeof(QuanService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class QuanService : INotifyPropertyChanged
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private int _id = 0;
        private readonly List<ChromatogramPeakInfo> _peaks;
        private FitResult _fitResult;


        private readonly string _connectionString = @"Data Source=source.db3;Version=3;";
        [ImportingConstructor]
        public QuanService()
        {
            _peaks = new List<ChromatogramPeakInfo>();
            UpdateRange();
        }

        public void UpdateRange()
        {
            XMin = double.MaxValue;
            XMax = double.MinValue;

            foreach (var peak in GetCalibrationPeaks())
            {
                if (peak.Area > XMax)
                    XMax = peak.Area;

                if (peak.Area < XMin)
                    XMin = peak.Area;
            }

            if (XMin == double.MaxValue)
                XMin = double.NaN;
            if (XMax == double.MinValue)
                XMax = double.NaN;
        }


        public IEnumerable<ChromatogramPeakInfo> Peaks => _peaks;

        public double XMin { get; private set; }
        public double XMax { get; private set; }


        public void Add(ChromatogramPeakInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.SampleType))
                info.SampleType = "Unknown";

            _peaks.Add(info);

            info.PropertyChanged += OnPeakPropertyChanged;

            if (CanCalibrate)
            {
                Calibrate();
            }
            else
            {
                FitResult = null;
            }

            UpdateValues();

            RenumberIds();

            UpdateRecoveryValues();

            OnChromatogramPeakAdded(new ChromatogramPeakAddedEventArgs(info));
        }

        private void UpdateValues()
        {
            if (FitResult == null)
                return;

            foreach (var peak in _peaks)
            {
                peak.CalculatedAmount = FitResult.GetY(peak.Area);
            }
        }

        public FitResult FitResult
        {
            get { return _fitResult; }
            set
            {
                if (_fitResult == value)
                    return;
                _fitResult = value;
                OnPropertyChanged(nameof(FitResult));
            }


        }

        private void Calibrate()
        {
            //var peaks = _peaks.Select<>(o => o.ExpectedAmount != null);
            var peaks = GetCalibrationPeaks().ToList();

            var x = new double[peaks.Count];
            var y = new double[peaks.Count];

            for (int i = 0; i < peaks.Count; i++)
            {
                if (peaks[i].ExpectedAmount != null)
                {
                    x[i] = peaks[i].Area;
                    y[i] = peaks[i].ExpectedAmount.Value;
                }
            }

            try
            {
                var p = Fit.Line(x, y);
                var offset = p.Item1;
                var slope = p.Item2;
                var calc = x.Select(v => v * slope + offset);

                var rs = GoodnessOfFit.RSquared(x.Select(v => v * slope + offset), y);

                FitResult = new FitResult() { Offset = offset, RSquared = rs, Slope = slope };
            }
            catch (Exception)
            {


            }
            //var x = new double[];
            //var p = Fit.Line(x, y);
            //var offset = p.Item1;
            //var slope = p.Item2
            //GoodnessOfFit
        }

        public event EventHandler<ChromatogramPeakAddedEventArgs> ChromatogramPeakAdded;

        protected virtual void OnChromatogramPeakAdded(ChromatogramPeakAddedEventArgs e)
        {
            ChromatogramPeakAdded?.Invoke(this, e);
        }

        public void Clear()
        {
            foreach (var peak in _peaks)
                peak.PropertyChanged -= OnPeakPropertyChanged;

            _peaks.Clear();
            FitResult = null;

            UpdateRange();
            UpdateValues();
            RenumberIds();
            UpdateRecoveryValues();
        }

        public void Remove(ChromatogramPeakInfo peak)
        {
            if (peak != null)
                peak.PropertyChanged -= OnPeakPropertyChanged;
            _peaks.Remove(peak);
            if (CanCalibrate)
                Calibrate();
            else
                FitResult = null;

            UpdateRange();
            UpdateValues();
            RenumberIds();
            UpdateRecoveryValues();
        }

        public void SaveData(string fileName)
        {
            try
            {
                using (var writer = new System.IO.StreamWriter(fileName))
                {
                    var serializer = new XmlSerializer(_peaks.GetType());
                    serializer.Serialize(writer, _peaks);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {

                Log.Error(ex);
            }

        }

        public bool CanCalibrate
        {
            get { return GetCalibrationPeaks().Count() >= 2; }
        }

        private IEnumerable<ChromatogramPeakInfo> GetCalibrationPeaks()
        {
            return _peaks.Where(peak =>
                peak != null &&
                peak.ExpectedAmount != null &&
                string.Equals((peak.SampleType ?? string.Empty).Trim(), "Calibrant", StringComparison.OrdinalIgnoreCase));
        }

        public void LoadData(string fileName)
        {
            try
            {
                using (var stream = System.IO.File.OpenRead(fileName))
                {
                    var serializer = new XmlSerializer(typeof(List<ChromatogramPeakInfo>));
                    var peaks = serializer.Deserialize(stream) as List<ChromatogramPeakInfo>;

                    if (peaks == null) return;
                    _peaks.Clear();

                    foreach (var peak in peaks)
                    {
                        if (peak != null && string.IsNullOrWhiteSpace(peak.SampleType))
                            peak.SampleType = "Unknown";
                    }

                    _peaks.AddRange(peaks);

                    foreach (var peak in _peaks)
                    {
                        if (peak != null)
                            peak.PropertyChanged += OnPeakPropertyChanged;
                    }

                    UpdateRange();
                    if (CanCalibrate)
                    {
                        Calibrate();
                        UpdateValues();
                    }

                    RenumberIds();

                    UpdateRecoveryValues();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Recalculate()
        {
            UpdateRange();
            if (CanCalibrate)
            {
                Calibrate();
                UpdateValues();
            }

            RenumberIds();

            UpdateRecoveryValues();

            InsertLog(
            "Calibration Curve",
            "Calibration Curve and Recovery Calculation Done."
        );
        }

        private void RenumberIds()
        {
            for (int i = 0; i < _peaks.Count; i++)
            {
                var p = _peaks[i];
                if (p != null)
                    p.ID = i + 1;
            }

            _id = _peaks.Count;
        }

        private void OnPeakPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChromatogramPeakInfo.CalculatedAmount) ||
                e.PropertyName == nameof(ChromatogramPeakInfo.ExpectedAmount) ||
                e.PropertyName == nameof(ChromatogramPeakInfo.SampleType))
            {
                UpdateRecoveryValues();
            }
        }

        private void UpdateRecoveryValues()
        {
            try
            {
                foreach (var p in _peaks)
                {
                    if (p == null)
                        continue;

                    var t = (p.SampleType ?? string.Empty).Trim();
                    if (string.Equals(t, "Blank", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(t, "Unknown", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(t, "Matrix sample", StringComparison.OrdinalIgnoreCase))
                    {
                        p.RecoveryPercent = null;
                    }
                }

                var matrix = _peaks
                    .Where(p => p != null && string.Equals((p.SampleType ?? string.Empty).Trim(), "Matrix sample", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.ID)
                    .ToList();

                var spikes = _peaks
                    .Where(p => p != null && string.Equals((p.SampleType ?? string.Empty).Trim(), "Spike Sample", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.ID)
                    .ToList();

                if (spikes.Count == 0)
                    return;

                // If there are no matrix rows, spike recovery cannot be computed.
                if (matrix.Count == 0)
                {
                    foreach (var s in spikes)
                        s.RecoveryPercent = null;
                    return;
                }

                for (int i = 0; i < spikes.Count; i++)
                {
                    var spike = spikes[i];
                    var m = matrix[i % matrix.Count];

                    if (spike.ExpectedAmount == null || spike.ExpectedAmount.Value == 0 || spike.CalculatedAmount == null || m.CalculatedAmount == null)
                    {
                        spike.RecoveryPercent = null;
                        continue;
                    }

                    spike.RecoveryPercent = ((spike.CalculatedAmount.Value - m.CalculatedAmount.Value) / spike.ExpectedAmount.Value) * 100.0;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void ExportCsvData(string fileName)
        {
            try
            {

                using (var writer = new System.IO.StreamWriter(fileName))
                {
                    if (FitResult != null)
                    {
                        var sign = FitResult.Offset >= 0 ? "+" : "-";
                        writer.WriteLine(
                            $"y = {FitResult.Slope} * x {sign} {Math.Abs(FitResult.Offset)}, R^2 = {FitResult.RSquared:0.000}");
                    }
                    writer.WriteLine();
                    writer.WriteLine("SampleID, Exp Amt, Area, Calc Amt, %Diff, FileName");
                    foreach (var peak in Peaks)
                    {
                        writer.WriteLine($"{peak.SampleID}, {Watermark(peak.ExpectedAmount)}, {peak.Area}, " +
                                         $"{Watermark(peak.CalculatedAmount)}, {Watermark(peak.Difference)}, {peak.FilePath}");
                    }
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {

                Log.Error(ex);
            }
        }

        private string Watermark(double? v, string watermark = "N/A")
        {
            return v?.ToString() ?? watermark;
        }

        private void InsertLog(string action, string description)
        {
            int userId = 0;
            if (Application.Current?.Properties?["UserId"] != null)
                userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

            using (var con = new SQLiteConnection(_connectionString))
            {
                con.Open();

                var sql = @"INSERT INTO AuditLogM
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
    }
}