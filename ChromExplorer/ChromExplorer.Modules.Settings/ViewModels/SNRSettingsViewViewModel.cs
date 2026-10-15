using ChromExplorer.DataModel.PeakPicking;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Services.PickPeakerService.Services;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Windows;

namespace ChromExplorer.Modules.Settings.ViewModels
{
    [Export(typeof(SNRSettingsViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class SNRSettingsViewViewModel : INotifyPropertyChanged, IPartImportsSatisfiedNotification, IDataErrorInfo
    {
        [Import] private ISNRService _snrService;
        [Import] private PeakPickerService _peakPickerService;
        [Import] private ChromDataProviderFactory _dataProviderFactory;
        [Import] private IEventAggregator _eventAggregator;

        private bool _isSignalIntegrationActive;
        private double _signalIntegrationStartTime = double.NaN;
        private double _signalIntegrationEndTime = double.NaN;

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;";
        public double TimeRangeStart
        {
            get => _signalIntegrationStartTime;
            set
            {
                if (value.Equals(_signalIntegrationStartTime)) return;
                _signalIntegrationStartTime = value;
                OnPropertyChanged(nameof(TimeRangeStart));
            }
        }

        public double TimeRangeEnd
        {
            get => _signalIntegrationEndTime;
            set
            {
                if (value.Equals(_signalIntegrationEndTime)) return;
                _signalIntegrationEndTime = value;
                OnPropertyChanged(nameof(TimeRangeEnd));
            }
        }

        private double _autoRmsNoiseStart = double.NaN;
        private double _autoRmsNoiseEnd = double.NaN;

        public double AutoRmsNoiseStart
        {
            get => _autoRmsNoiseStart;
            set
            {
                if (value.Equals(_autoRmsNoiseStart)) return;
                _autoRmsNoiseStart = value;
                OnPropertyChanged(nameof(AutoRmsNoiseStart));
                UpdateNoiseWidthFromAutoRmsRange();
            }
        }

        public double AutoRmsNoiseEnd
        {
            get => _autoRmsNoiseEnd;
            set
            {
                if (value.Equals(_autoRmsNoiseEnd)) return;
                _autoRmsNoiseEnd = value;
                OnPropertyChanged(nameof(AutoRmsNoiseEnd));
                UpdateNoiseWidthFromAutoRmsRange();
            }
        }

        private double _noiseWidth;
        public double NoiseWidth
        {
            get => _noiseWidth;
            set
            {
                if (value.Equals(_noiseWidth)) return;
                _noiseWidth = value;
                OnPropertyChanged(nameof(NoiseWidth));
            }
        }

        private void UpdateNoiseWidthFromAutoRmsRange()
        {
            var duration = AutoRmsNoiseEnd - AutoRmsNoiseStart;
            if (double.IsNaN(duration) || duration <= 0)
                return;

            NoiseWidth = Math.Max(0, duration - 0.001);
        }

        public bool IsAutoRMS => NoiseDefinition == "AutoRMS";

        public bool IsNotAutoRMS => !IsAutoRMS;

        public bool IsRmsNoise => NoiseDefinition == "RMS";

        public bool IsSpecificDetectionAndNotAutoRms => IsSpecificDetection && IsNotAutoRMS;

        private string _signalDefinition;
        private string _noiseDefinition;
        private double _multiplier = 1.0;
        private bool _isAutomaticDetection = true;
        private double _desiredLength = 1.0;
        private double _minimumLength = 0.1;
        private string _specificTimeRange = "e.g., 1-2, 4-5";
        private bool _autoCompute;

        private double _signalValue;
        private double _noiseValue;
        private double _snRatio;
        private string _noiseRegionUsed;

        public SNRSettingsViewViewModel()
        {
            SignalDefinitions = new List<string> { "Height", "Area" };
            SignalDefinition = "Height";

            NoiseDefinitions = new List<string> { "AutoRMS", "Peak-to-Peak", "RMS" };
            NoiseDefinition = "Peak-to-Peak";

            CalculateCommand = new DelegateCommand(OnCalculate);
            ResetCommand = new DelegateCommand(OnReset);
        }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            if (_eventAggregator != null)
            {
                _eventAggregator.GetEvent<PeakIntegrationEvent>().Subscribe(OnPeakIntegration);
                _eventAggregator.GetEvent<SNRMethodAppliedEvent>().Subscribe(OnSNRMethodApplied);
            }
        }

        // "Apply Saved Method" (graph footer) can't re-run the SNR calculation — it needs the exact
        // peak/spectrum selection that was active at calculate-time — so it republishes the saved
        // inputs and result instead; mirror them here exactly as Calculate would have left them.
        private void OnSNRMethodApplied(SNRMethodAppliedArgs args)
        {
            if (args == null)
                return;

            SignalDefinition = args.SignalDefinition;
            NoiseDefinition = args.NoiseDefinition;
            Multiplier = args.Multiplier;
            IsAutomaticDetection = args.IsAutomaticDetection;
            DesiredLength = args.DesiredLength;
            MinimumLength = args.MinimumLength;
            SpecificTimeRange = args.SpecificTimeRange;
            AutoCompute = args.AutoCompute;

            SignalValue = args.SignalValue;
            NoiseValue = args.NoiseValue;
            SNRatio = args.SNRatio;
            NoiseRegionUsed = args.NoiseRegionUsed;
        }

        private void OnPeakIntegration(PeakIntegrationArgs args)
        {
            if (args == null)
                return;

            if (args.IsRemove)
            {
                _isSignalIntegrationActive = false;
                TimeRangeStart = double.NaN;
                TimeRangeEnd = double.NaN;
                AutoRmsNoiseStart = double.NaN;
                AutoRmsNoiseEnd = double.NaN;
            }
            else
            {
                _isSignalIntegrationActive = true;
                TimeRangeStart = args.StartTime;
                TimeRangeEnd = args.EndTime;
                AutoRmsNoiseStart = args.StartTime;
                AutoRmsNoiseEnd = args.EndTime;
            }
        }

        public List<string> SignalDefinitions { get; private set; }
        public List<string> NoiseDefinitions { get; private set; }

        public string SignalDefinition
        {
            get => _signalDefinition;
            set { _signalDefinition = value; OnPropertyChanged("SignalDefinition"); }
        }

        public string NoiseDefinition
        {
            get => _noiseDefinition;
            set
            {
                _noiseDefinition = value;
                if (_noiseDefinition != "RMS")
                {
                    Multiplier = 1.0;
                }
                OnPropertyChanged("NoiseDefinition");
                OnPropertyChanged(nameof(IsAutoRMS));
                OnPropertyChanged(nameof(IsNotAutoRMS));
                OnPropertyChanged(nameof(IsRmsNoise));
                OnPropertyChanged(nameof(IsSpecificDetectionAndNotAutoRms));
                OnPropertyChanged(nameof(NoiseWidth));
            }
        }

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(NoiseWidth) && IsAutoRMS)
                {
                    var duration = AutoRmsNoiseEnd - AutoRmsNoiseStart;
                    if (double.IsNaN(duration) || duration <= 0)
                        return "Invalid time range";

                    if (NoiseWidth <= 0)
                        return "Noise width must be greater than 0";

                    if (NoiseWidth >= duration)
                        return "Noise width must be less than the time range";
                }

                return null;
            }
        }

        public double Multiplier
        {
            get => _multiplier;
            set { _multiplier = value; OnPropertyChanged("Multiplier"); }
        }

        public bool IsAutomaticDetection
        {
            get => _isAutomaticDetection;
            set
            {
                _isAutomaticDetection = value;
                OnPropertyChanged("IsAutomaticDetection");
                OnPropertyChanged("IsSpecificDetection");
                OnPropertyChanged(nameof(IsSpecificDetectionAndNotAutoRms));
            }
        }

        public bool IsSpecificDetection
        {
            get => !_isAutomaticDetection;
            set
            {
                IsAutomaticDetection = !value;
            }
        }

        public double DesiredLength
        {
            get => _desiredLength;
            set { _desiredLength = value; OnPropertyChanged("DesiredLength"); }
        }

        public double MinimumLength
        {
            get => _minimumLength;
            set { _minimumLength = value; OnPropertyChanged("MinimumLength"); }
        }

        public string SpecificTimeRange
        {
            get => _specificTimeRange;
            set { _specificTimeRange = value; OnPropertyChanged("SpecificTimeRange"); }
        }

        public bool AutoCompute
        {
            get => _autoCompute;
            set { _autoCompute = value; OnPropertyChanged("AutoCompute"); }
        }

        public double SignalValue
        {
            get => _signalValue;
            set { _signalValue = value; OnPropertyChanged("SignalValue"); }
        }

        public double NoiseValue
        {
            get => _noiseValue;
            set { _noiseValue = value; OnPropertyChanged("NoiseValue"); }
        }

        public double SNRatio
        {
            get => _snRatio;
            set { _snRatio = value; OnPropertyChanged("SNRatio"); }
        }

        public string NoiseRegionUsed
        {
            get => _noiseRegionUsed;
            set { _noiseRegionUsed = value; OnPropertyChanged("NoiseRegionUsed"); }
        }

        public DelegateCommand CalculateCommand { get; private set; }
        public DelegateCommand ResetCommand { get; private set; }

        private void OnCalculate()
        {
            MessageBox.Show("OnCalculate triggered!");

            if (IsAutoRMS)
            {
                var noiseWidthError = this[nameof(NoiseWidth)];
                if (!string.IsNullOrWhiteSpace(noiseWidthError))
                {
                    MessageBox.Show(noiseWidthError, "Invalid AutoRMS Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (_snrService == null || _dataProviderFactory == null || _dataProviderFactory.Current == null || _dataProviderFactory.Current.Chromatogram == null)
            {
                string msg = "SNR calculation cannot start:";
                if (_snrService == null) msg += "\n- SNR Service is missing";
                if (_dataProviderFactory == null) msg += "\n- Data Provider Factory is missing";
                else if (_dataProviderFactory.Current == null) msg += "\n- No active Data Provider";
                else if (_dataProviderFactory.Current.Chromatogram == null) msg += "\n- No Chromatogram loaded";

                MessageBox.Show(msg);
                return;
            }

            var chrom = _dataProviderFactory.Current.Chromatogram;
            var points = chrom.Points;
            if (points == null || points.Xvals == null || points.Yvals == null)
            {
                MessageBox.Show("No chromatogram data available.");
                return;
            }

            // Get peaks from the PeakPickerService
            var peaks = _peakPickerService.GetPeaks(points, points.Xvals.Min(), points.Xvals.Max());
            if (peaks == null || peaks.Count == 0)
            {
                MessageBox.Show("No peaks detected. Please perform peak picking first.");
                return;
            }

            // Prefer the currently selected point on the chromatogram (graph click) if available.
            // Fall back to the largest peak if selection is not available.
            PeakInfo targetPeak = null;
            int selectedIndex = chrom.SelectedIndex;
            if (selectedIndex >= 0 && points.Xvals != null && selectedIndex < points.Xvals.Length)
            {
                double selectedTime = points.Xvals[selectedIndex];
                targetPeak = peaks
                    .OrderBy(p => Math.Abs(p.Position - selectedTime))
                    .FirstOrDefault();
            }

            if (targetPeak == null)
            {
                targetPeak = peaks.OrderByDescending(p => p.Intensity).First();
            }

            // DEBUG: EXACTLY like UpdatePeakMarkers()!
            var x = points.Xvals;
            var y = points.Yvals;

            // Find closest peak index to targetPeak.Position
            int peakIndex = 0;
            double minDist = double.MaxValue;
            for (int i = 0; i < x.Length; i++)
            {
                double dist = Math.Abs(x[i] - targetPeak.Position);
                if (dist < minDist)
                {
                    minDist = dist;
                    peakIndex = i;
                }
            }

            // Find left valley
            int leftIndex = peakIndex;
            while (leftIndex > 0 && y[leftIndex - 1] < y[leftIndex])
            {
                leftIndex--;
            }

            // Find right valley
            int rightIndex = peakIndex;
            while (rightIndex < y.Length - 1 && y[rightIndex + 1] < y[rightIndex])
            {
                rightIndex++;
            }

            double debugArea = points.Integrate(x[leftIndex], x[rightIndex]);
            MessageBox.Show("DEBUG INFO:\nTarget Peak Position: " + targetPeak.Position +
                            "\nPeak Index (closest): " + peakIndex +
                            "\nLeft Index: " + leftIndex +
                            "\nRight Index: " + rightIndex +
                            "\nArea (points.Integrate()): " + debugArea);

            var settings = new SNRCalculationSettings
            {
                SignalDefinition = this.SignalDefinition,
                NoiseDefinition = this.NoiseDefinition,
                Multiplier = this.Multiplier,
                IsAutomaticDetection = this.IsAutomaticDetection,
                DesiredLength = this.DesiredLength,
                MinimumLength = this.MinimumLength,
                SpecificTimeRange = this.SpecificTimeRange,

                IsSignalIntegrationActive = _isSignalIntegrationActive,
                SignalIntegrationStartTime = TimeRangeStart,
                SignalIntegrationEndTime = TimeRangeEnd,
                AutoRmsNoiseStartTime = AutoRmsNoiseStart,
                AutoRmsNoiseEndTime = AutoRmsNoiseEnd,
                AutoRmsNoiseWidth = NoiseWidth
            };

            var result = _snrService.CalculateSNR(points, peaks, targetPeak, settings);
            if (result.Success)
            {
                // FIRST assign values
                SignalValue = result.SignalValue;
                NoiseValue = result.NoiseValue;
                SNRatio = result.SNRatio;
                NoiseRegionUsed = result.NoiseRegionUsed;

                string parameter =
                    "Signal Definition : " + SignalDefinition +
                    ", Noise Definition : " + NoiseDefinition;

                // ALWAYS SAVE MULTIPLIER
                parameter +=
                    ", Multiplier : " + Multiplier.ToString("0.000");

                // AutoRMS Details
                if (NoiseDefinition == "AutoRMS")
                {
                    parameter +=
                        ", Time Range : " + AutoRmsNoiseStart.ToString("0.000") +
                        " to " + AutoRmsNoiseEnd.ToString("0.000") +
                        ", Noise Width : " + NoiseWidth.ToString("0.000");
                }

                // Specific Noise Region
                if (IsSpecificDetection &&
                    !string.IsNullOrWhiteSpace(SpecificTimeRange) &&
                    SpecificTimeRange != "e.g., 1-2, 4-5")
                {
                    parameter +=
                        ", Specific Noise Region : " + SpecificTimeRange;
                }

                // Output values
                parameter +=
                    ", Signal Value : " + SignalValue.ToString("0.000") +
                    ", Noise Value : " + NoiseValue.ToString("0.000") +
                    ", S/N Ratio : " + SNRatio.ToString("0.000");

                // Noise region used
                if (!string.IsNullOrWhiteSpace(NoiseRegionUsed))
                {
                    parameter +=
                        ", Noise Region Used : " + NoiseRegionUsed;
                }

                InsertLog(
                    "SNR Calculation",
                    "SNR Calculated Successfully",
                    parameter
                );

                _eventAggregator?.GetEvent<SNRCalculatedEvent>().Publish(result);

                MessageBox.Show(
                    "SNR calculated successfully:\nSignal: " +
                    SignalValue +
                    "\nNoise: " +
                    NoiseValue +
                    "\nRatio: " +
                    SNRatio
                );
            }
            //        if (result.Success)
            //        {
            //            string parameter =
            //"Signal Definition : " + SignalDefinition +
            //", Noise Definition : " + NoiseDefinition;

            //            if (NoiseDefinition == "RMS")
            //            {
            //                parameter +=
            //                    ", Multiplier : " + Multiplier.ToString("0.000");
            //            }

            //            if (NoiseDefinition == "AutoRMS")
            //            {
            //                parameter +=
            //                    ", Time Range : " + AutoRmsNoiseStart.ToString("0.000") +
            //                    " to " + AutoRmsNoiseEnd.ToString("0.000") +
            //                    ", Noise Width : " + NoiseWidth.ToString("0.000");
            //            }

            //            if (IsSpecificDetection &&
            //                !string.IsNullOrWhiteSpace(SpecificTimeRange) &&
            //                SpecificTimeRange != "e.g., 1-2, 4-5")
            //            {
            //                parameter +=
            //                    ", Specific Noise Region : " + SpecificTimeRange;
            //            }

            //            parameter +=
            //                ", Signal Value : " + SignalValue.ToString("0.000") +
            //                ", Noise Value : " + NoiseValue.ToString("0.000") +
            //                ", S/N Ratio : " + SNRatio.ToString("0.000");

            //            if (!string.IsNullOrWhiteSpace(NoiseRegionUsed))
            //            {
            //                parameter +=
            //                    ", Noise Region Used : " + NoiseRegionUsed;
            //            }

            //            InsertLog(
            //                "SNR Calculation",
            //                "SNR Calculated Successfully",
            //                parameter
            //            );

            //            SignalValue = result.SignalValue;
            //            NoiseValue = result.NoiseValue;
            //            SNRatio = result.SNRatio;
            //            NoiseRegionUsed = result.NoiseRegionUsed;

            //            _eventAggregator?.GetEvent<SNRCalculatedEvent>().Publish(result);
            //            MessageBox.Show("SNR calculated successfully:\nSignal: " + SignalValue + "\nNoise: " + NoiseValue + "\nRatio: " + SNRatio);
            //        }
            else
            {
                string parameter =
        "Signal Definition : " + SignalDefinition +
        ", Noise Definition : " + NoiseDefinition +
        ", Error : " + result.ErrorMessage;

                InsertLog(
                    "SNR Calculation",
                    "SNR Calculation Failed",
                    parameter
                );
                MessageBox.Show(result.ErrorMessage);
            }
        }

        private void OnReset()
        {
            SignalDefinition = "Height";
            NoiseDefinition = "Peak-to-Peak";
            Multiplier = 1.0;
            IsAutomaticDetection = true;
            DesiredLength = 1.0;
            MinimumLength = 0.1;
            SpecificTimeRange = "e.g., 1-2, 4-5";
            AutoCompute = false;

            AutoRmsNoiseStart = double.NaN;
            AutoRmsNoiseEnd = double.NaN;

            // Reset output values
            SignalValue = 0;
            NoiseValue = 0;
            SNRatio = 0;
            NoiseRegionUsed = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
    }
}
