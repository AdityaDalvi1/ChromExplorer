using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.PeakPicking;
using ChromExplorer.DataModel.Properties;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Events;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.Quan.Model;
using ChromExplorer.Modules.Quan.Services;
using ChromExplorer.Plotter.Markers;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;

namespace ChromExplorer.Modules.Viewer.ViewModels
{
    public class QuanConfirmation : Confirmation
    {
        public string SampleID { get; set; }
        public double? ExpectedAmount { get; set; }
        public double Area { get; set; }
        public string SampleType { get; set; }
    }

    /// <summary>Popup content for "Add to RSD" ? deliberately just Sample ID (editable, so the
    /// same value can be typed for every replicate injection of one sample ? RSD groups by this)
    /// and the already-computed peak Area (read-only). No ExpectedAmount/SampleType like
    /// QuanConfirmation ? RSD is a precision check across replicates, not a calibration.</summary>
    public class RsdConfirmation : Confirmation
    {
        public string SampleID { get; set; }
        public double? Concentration { get; set; }
        public double Area { get; set; }
    }

    public class ChromatogramPlotViewModel : INotifyPropertyChanged
    {
        readonly InteractionRequest<QuanConfirmation> _confirmationFromViewModelInteractionRequest;
        readonly InteractionRequest<RsdConfirmation> _confirmationFromRsdInteractionRequest;
        private readonly QuanService _quanService;
        private readonly RsdService _rsdService;
        private readonly IEventAggregator _eventAggregator;

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        private double _area;
        private DataRect _visibleRect;
        private bool _multiSelectEnabled;
        private MarkerCollection _markers;
        private MarkerCollection _originalMarkers;

        private double _integrationStartTime = double.NaN;
        private double _integrationEndTime = double.NaN;
        private bool _isIntegrationActive = false;

        public ChromatogramPlotViewModel(GenericChromatogram chrom, QuanService service, RsdService rsdService, IEventAggregator eventAggregator)
        {
            _quanService = service;
            _rsdService = rsdService;
            _eventAggregator = eventAggregator;
            _confirmationFromViewModelInteractionRequest = new InteractionRequest<QuanConfirmation>();
            _confirmationFromRsdInteractionRequest = new InteractionRequest<RsdConfirmation>();
            Chromatogram = chrom;
            MultiSelectEnabled = false;
            ExportChromCommand = new DelegateCommand<object>(OnExportChrom);
            IncrementSelectedCommand = new DelegateCommand<object>(OnIncrementSelectedIndex);
            QuanCommand = new DelegateCommand(OnQuan);
            RsdCommand = new DelegateCommand(OnRsd);

            _markers = new MarkerCollection();

            if (_eventAggregator != null)
            {
                _eventAggregator.GetEvent<PeakIntegrationEvent>().Subscribe(OnPeakIntegration);
                _eventAggregator.GetEvent<SNRCalculatedEvent>().Subscribe(OnSNRCalculated);
            }

            if (Chromatogram != null)
            {
                Chromatogram.PropertyChanged += OnChromatogramPropertyChanged;
                UpdatePeakMarkers();
            }
        }

        private void OnPeakIntegration(PeakIntegrationArgs args)
        {
            if (args.IsRemove)
            {
                IsIntegrationActive = false;
                IntegrationStartTime = double.NaN;
                IntegrationEndTime = double.NaN;

                // Restore original markers if they were saved
                if (_originalMarkers != null)
                {
                    Markers = _originalMarkers;
                    _originalMarkers = null;
                }
                else
                {
                    UpdatePeakMarkers();
                }
            }
            else
            {
                IsIntegrationActive = true;
                IntegrationStartTime = args.StartTime;
                IntegrationEndTime = args.EndTime;

                if (_visibleRect == null)
                    _visibleRect = new DataRect();

                _visibleRect.XMin = args.StartTime;
                _visibleRect.XMax = args.EndTime;

                // Save original markers before clearing them
                if (_originalMarkers == null)
                {
                    _originalMarkers = Markers;
                }

                PerformManualIntegration(args.StartTime, args.EndTime);
            }
        }

        private void OnSNRCalculated(SNRResult result)
        {
            if (result.Success)
            {
                IsNoiseRegionActive = true;
                NoiseRegionStartTime = result.NoiseRegionStartTime;
                NoiseRegionEndTime = result.NoiseRegionEndTime;

                IsSNRCalculated = true;
                SNRSignalValue = result.SignalValue;
                SNRNoiseValue = result.NoiseValue;
                SNRatio = result.SNRatio;
                SNRNoiseRegionUsed = result.NoiseRegionUsed;
                SNRSignalDefinition = result.SignalDefinition;
                SNRNoiseDefinition = result.NoiseDefinition;
            }
            else
            {
                IsNoiseRegionActive = false;
                NoiseRegionStartTime = double.NaN;
                NoiseRegionEndTime = double.NaN;

                IsSNRCalculated = false;
                SNRSignalValue = double.NaN;
                SNRNoiseValue = double.NaN;
                SNRatio = double.NaN;
                SNRNoiseRegionUsed = result.ErrorMessage;
                SNRSignalDefinition = null;
                SNRNoiseDefinition = null;
            }
        }

        private void PerformManualIntegration(double start, double end)
        {
            if (Chromatogram?.Points == null) return;

            try
            {
                var points = Chromatogram.Points;
                Area = points.Integrate(start, end);
                var area = Area;

                // Find RT (max intensity in range)
                double maxIntensity = double.MinValue;
                double rt = start;
                for (int i = 0; i < points.Xvals.Length; i++)
                {
                    if (points.Xvals[i] >= start && points.Xvals[i] <= end)
                    {
                        if (points.Yvals[i] > maxIntensity)
                        {
                            maxIntensity = points.Yvals[i];
                            rt = points.Xvals[i];
                        }
                    }
                }

                var newMarkers = new MarkerCollection();
                var label = string.Format("RT: {0:F3} min\nArea: {1:F0}", rt, area);
                var marker = new SimpleMarker(new Point(rt, maxIntensity), label)
                {
                    Placement = MarkerPlacement.Top,
                    Orientation = MarkerOrientation.Horizontal,
                    ScreenOffset = 5.0
                };
                newMarkers.Add(marker);

                IntegrationRT = rt;

                Markers = newMarkers; // This will hide all other markers
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        private static double ComputePeakIntegrationArea(double[] xvals, double[] yvals, double startTime, double endTime)
        {
            if (xvals == null || yvals == null) return 0;
            if (xvals.Length != yvals.Length || xvals.Length < 2) return 0;

            double start = Math.Min(startTime, endTime);
            double end = Math.Max(startTime, endTime);


            int startIndex = -1;
            int endIndex = -1;
            for (int i = 0; i < xvals.Length; i++)
            {
                if (startIndex == -1 && xvals[i] >= start) startIndex = i;
                if (xvals[i] <= end) endIndex = i;
            }

            if (startIndex < 0 || endIndex < 0 || endIndex - startIndex < 1)
                return 0;

            double yStart = GetInterpolatedYFromArrays(xvals, yvals, start);
            double yEnd = GetInterpolatedYFromArrays(xvals, yvals, end);


            double integral = 0.0;

            double prevX = start;
            double prevYAbove = 0.0;

            for (int i = startIndex; i <= endIndex; i++)
            {
                double x = xvals[i];
                if (x < start || x > end) continue;

                double y = yvals[i];
                double t = (end - start) > 1e-12 ? (x - start) / (end - start) : 0;
                double yLine = yStart + t * (yEnd - yStart);
                double yAbove = Math.Max(0.0, y - yLine);

                integral += (prevYAbove + yAbove) / 2.0 * (x - prevX);
                prevX = x;
                prevYAbove = yAbove;
            }


            integral += (prevYAbove + 0.0) / 2.0 * (end - prevX);

            return integral;
        }

        private static double GetInterpolatedYFromArrays(double[] xvals, double[] yvals, double x)
        {
            if (xvals == null || yvals == null) return 0;
            if (xvals.Length != yvals.Length || xvals.Length < 2) return 0;

            int idx = Array.BinarySearch(xvals, x);
            if (idx >= 0) return yvals[idx];
            idx = ~idx;

            if (idx <= 0) return yvals[0];
            if (idx >= xvals.Length) return yvals[xvals.Length - 1];

            double x0 = xvals[idx - 1];
            double x1 = xvals[idx];
            double y0 = yvals[idx - 1];
            double y1 = yvals[idx];

            double dx = x1 - x0;
            if (Math.Abs(dx) < 1e-12) return y0;
            double tt = (x - x0) / dx;
            return y0 + tt * (y1 - y0);
        }

        public double IntegrationStartTime
        {
            get { return _integrationStartTime; }
            set { _integrationStartTime = value; OnPropertyChanged(nameof(IntegrationStartTime)); }
        }

        public double IntegrationEndTime
        {
            get { return _integrationEndTime; }
            set { _integrationEndTime = value; OnPropertyChanged(nameof(IntegrationEndTime)); }
        }

        public bool IsIntegrationActive
        {
            get { return _isIntegrationActive; }
            set { _isIntegrationActive = value; OnPropertyChanged(nameof(IsIntegrationActive)); }
        }

        private double _integrationRT = double.NaN;

        public double IntegrationRT
        {
            get { return _integrationRT; }
            set { _integrationRT = value; OnPropertyChanged(nameof(IntegrationRT)); }
        }

        private double _noiseRegionStartTime = double.NaN;
        private double _noiseRegionEndTime = double.NaN;
        private bool _isNoiseRegionActive = false;

        public double NoiseRegionStartTime
        {
            get { return _noiseRegionStartTime; }
            set { _noiseRegionStartTime = value; OnPropertyChanged(nameof(NoiseRegionStartTime)); }
        }

        public double NoiseRegionEndTime
        {
            get { return _noiseRegionEndTime; }
            set { _noiseRegionEndTime = value; OnPropertyChanged(nameof(NoiseRegionEndTime)); }
        }

        public bool IsNoiseRegionActive
        {
            get { return _isNoiseRegionActive; }
            set { _isNoiseRegionActive = value; OnPropertyChanged(nameof(IsNoiseRegionActive)); }
        }

        private bool _isSNRCalculated = false;
        private double _snrSignalValue = double.NaN;
        private double _snrNoiseValue = double.NaN;
        private double _snRatio = double.NaN;
        private string _snrNoiseRegionUsed;
        private string _snrSignalDefinition;
        private string _snrNoiseDefinition;

        public bool IsSNRCalculated
        {
            get { return _isSNRCalculated; }
            set { _isSNRCalculated = value; OnPropertyChanged(nameof(IsSNRCalculated)); }
        }

        public double SNRSignalValue
        {
            get { return _snrSignalValue; }
            set { _snrSignalValue = value; OnPropertyChanged(nameof(SNRSignalValue)); }
        }

        public double SNRNoiseValue
        {
            get { return _snrNoiseValue; }
            set { _snrNoiseValue = value; OnPropertyChanged(nameof(SNRNoiseValue)); }
        }

        public double SNRatio
        {
            get { return _snRatio; }
            set { _snRatio = value; OnPropertyChanged(nameof(SNRatio)); }
        }

        public string SNRNoiseRegionUsed
        {
            get { return _snrNoiseRegionUsed; }
            set { _snrNoiseRegionUsed = value; OnPropertyChanged(nameof(SNRNoiseRegionUsed)); }
        }

        public string SNRSignalDefinition
        {
            get { return _snrSignalDefinition; }
            set { _snrSignalDefinition = value; OnPropertyChanged(nameof(SNRSignalDefinition)); }
        }

        public string SNRNoiseDefinition
        {
            get { return _snrNoiseDefinition; }
            set { _snrNoiseDefinition = value; OnPropertyChanged(nameof(SNRNoiseDefinition)); }
        }

        private void OnChromatogramPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Points")
            {
                UpdatePeakMarkers();
            }
        }

        private void UpdatePeakMarkers()
        {
            if (Chromatogram?.Points == null || Chromatogram.Points.Length < 3)
            {
                Markers = new MarkerCollection();
                return;
            }

            try
            {
                var points = Chromatogram.Points;
                var x = points.Xvals;
                var y = points.Yvals;
                var maxIntensity = y.Any() ? y.Max() : 0;
                var threshold = maxIntensity * 0.05; // 5% threshold to avoid labeling noise

                var candidatePeaks = new List<Tuple<int, double>>(); // Index, Intensity

                // 1. Find all local maxima above the threshold
                for (int i = 1; i < y.Length - 1; i++)
                {
                    if (y[i] > y[i - 1] && y[i] > y[i + 1] && y[i] > threshold)
                    {
                        candidatePeaks.Add(new Tuple<int, double>(i, y[i]));
                    }
                }

                // 2. Sort candidates by intensity (descending)
                var sortedPeaks = candidatePeaks.OrderByDescending(p => p.Item2).ToList();

                var finalPeaks = new List<Tuple<int, double>>();
                const int minPeakDistance = 15; // Minimum distance in data points between labeled peaks

                // 3. Filter peaks to keep the most prominent ones that are not too close
                foreach (var peak in sortedPeaks)
                {
                    bool isTooClose = false;
                    foreach (var finalPeak in finalPeaks)
                    {
                        if (Math.Abs(peak.Item1 - finalPeak.Item1) < minPeakDistance)
                        {
                            isTooClose = true;
                            break;
                        }
                    }

                    if (!isTooClose)
                    {
                        finalPeaks.Add(peak);
                    }

                    // Limit the total number of labels to avoid clutter
                    if (finalPeaks.Count >= 10)
                    {
                        break;
                    }
                }

                var newMarkers = new MarkerCollection();

                // 4. Create markers for the final selected peaks
                foreach (var peak in finalPeaks)
                {
                    int i = peak.Item1;

                    // Find left valley (start of peak)
                    int leftIndex = i;
                    while (leftIndex > 0 && y[leftIndex - 1] < y[leftIndex])
                    {
                        leftIndex--;
                    }

                    // Find right valley (end of peak)
                    int rightIndex = i;
                    while (rightIndex < y.Length - 1 && y[rightIndex + 1] < y[rightIndex])
                    {
                        rightIndex++;
                    }

                    double area = points.Integrate(x[leftIndex], x[rightIndex]);
                    var label = string.Format("RT: {0:F3} min\nArea: {1:F0}", x[i], area);

                    var marker = new SimpleMarker(new Point(x[i], y[i]), label)
                    {
                        Placement = MarkerPlacement.Top,
                        Orientation = MarkerOrientation.Horizontal,
                        ScreenOffset = 5.0
                    };
                    newMarkers.Add(marker);
                }

                Markers = newMarkers;
            }
            catch (Exception ex)
            {
                // Log error if possible, but don't crash
            }
        }

        public MarkerCollection Markers
        {
            get { return _markers; }
            set
            {
                if (Equals(value, _markers)) return;
                _markers = value;
                OnPropertyChanged(nameof(Markers));
            }
        }


        public DelegateCommand<object> ExportChromCommand { get; set; }
        public DelegateCommand<object> IncrementSelectedCommand { get; private set; }

        public DelegateCommand QuanCommand { get; set; }
        public DelegateCommand RsdCommand { get; set; }

        public IInteractionRequest ConfirmationFromViewModelInteractionRequest
        {
            get { return _confirmationFromViewModelInteractionRequest; }
        }

        public IInteractionRequest ConfirmationFromRsdViewModelInteractionRequest
        {
            get { return _confirmationFromRsdInteractionRequest; }
        }

        public DataRect VisibleRect
        {
            get { return _visibleRect; }
            set
            {
                _visibleRect = value;
                CalculateArea(_visibleRect);
            }
        }

        public double Area
        {
            get { return _area; }
            set
            {
                if (value.Equals(_area)) return;
                _area = value;
                OnPropertyChanged(nameof(Area));
            }
        }


        public GenericChromatogram Chromatogram { get; }

        public bool MultiSelectEnabled
        {
            get { return _multiSelectEnabled; }
            set
            {
                if (value == _multiSelectEnabled) return;
                _multiSelectEnabled = value;
                OnPropertyChanged(nameof(MultiSelectEnabled));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnQuan()
        {
            if (Chromatogram == null)
                return;

            var units = "sec";
            units = Chromatogram.ChromPoints?.FirstOrDefault()?.TimeUnits;

            _confirmationFromViewModelInteractionRequest.Raise(
               new QuanConfirmation { Title = "Add to Quan", SampleID = Chromatogram.Title, Area = Area, ExpectedAmount = null, SampleType = "Unknown" },
               confirmation =>
               {
                   try
                   {


                       if (confirmation.Confirmed)
                       {
                           string parameter =
            "Sample ID : " + confirmation.SampleID +
            ", Sample Type : " + confirmation.SampleType +
            ", Expected Amount : " +
            (confirmation.ExpectedAmount.HasValue
                ? confirmation.ExpectedAmount.Value.ToString("0.000")
                : "NULL") +
            ", Area : " + confirmation.Area.ToString("0.000");

                           InsertLog("Quantation", "Quantation Added", parameter);

                           if (_quanService != null)
                           {
                               var info = new ChromatogramPeakInfo();
                               info.FilePath = Chromatogram?.Description;
                               info.SampleID = confirmation.SampleID;
                               info.ExpectedAmount = confirmation.ExpectedAmount;
                               info.Area = confirmation.Area;
                               info.SampleType = string.IsNullOrWhiteSpace(confirmation.SampleType) ? "Unknown" : confirmation.SampleType;
                               info.TimeUnits = units;
                               info.TimeFrom = _visibleRect.XMin;
                               info.TimeTo = _visibleRect.XMax;


                               var selectedSpectra = Chromatogram.SelectedSpectrumIndexes;
                               if (selectedSpectra != null && selectedSpectra.Count >= 1)
                               {

                                   var mFrom = Chromatogram.CurrentSpectrum.Points.Points[selectedSpectra.First()];
                                   var mTo = Chromatogram.CurrentSpectrum.Points.Points[selectedSpectra.Last()];

                                   info.MassRangeFrom = mFrom.X;
                                   info.MassRangeTo = mTo.X;

                               }
                               //if(Chromatogram.S)
                               //info.TimeIndexRange


                               _quanService.Add(info);
                           }
                           //
                           //RaiseNotificationFromViewModelInteractionRequest("You just clicked OK");
                       }
                       else
                       {
                           InsertLog("Quantation", "Quantation Cancelled", "");
                       }
                   }
                   catch (Exception ex)
                   {
                       InsertLog("Quantation", "Quantation Cancelled", ex.Message);
                   }
               });
        }

        // Mirrors OnQuan() above ? same popup/InsertLog/service-Add shape, just a much smaller
        // DTO (Sample ID + Area only) since RSD is a precision check across replicate injections
        // of one sample, not a calibration curve.
        private void OnRsd()
        {
            if (Chromatogram == null)
                return;

            _confirmationFromRsdInteractionRequest.Raise(
               new RsdConfirmation { Title = "Add to RSD", SampleID = Chromatogram.Title, Area = Area },
               confirmation =>
               {
                   try
                   {
                       if (confirmation.Confirmed)
                       {
                           string parameter =
                               "Sample ID : " + confirmation.SampleID +
                               ", Concentration : " + (confirmation.Concentration.HasValue ? confirmation.Concentration.Value.ToString("0.###") : "N/A") +
                               ", Area : " + confirmation.Area.ToString("0.000");

                           InsertLog("RSD", "RSD Entry Added", parameter);

                           if (_rsdService != null)
                           {
                               var info = new RsdPeakInfo
                               {
                                   FilePath = Chromatogram?.Description,
                                   SampleID = string.IsNullOrWhiteSpace(confirmation.SampleID) ? Chromatogram?.Title : confirmation.SampleID,
                                   Concentration = confirmation.Concentration,
                                   Area = confirmation.Area,
                               };

                               _rsdService.Add(info);
                           }
                       }
                       else
                       {
                           InsertLog("RSD", "RSD Entry Cancelled", "");
                       }
                   }
                   catch (Exception ex)
                   {
                       InsertLog("RSD", "RSD Entry Cancelled", ex.Message);
                   }
               });
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
        private void CalculateArea(DataRect visibleRect)
        {
            if (Chromatogram?.Points == null)
                return;

            if (IsIntegrationActive && !double.IsNaN(IntegrationStartTime) && !double.IsNaN(IntegrationEndTime))
            {
                Area = Chromatogram.Points.Integrate(IntegrationStartTime, IntegrationEndTime);
                return;
            }

            if (visibleRect == null)
                return;

            Area = Chromatogram.Points.Integrate(visibleRect.XMin, visibleRect.XMax);
        }

        private void OnIncrementSelectedIndex(object obj)
        {
            var ch = Chromatogram;
        }

        private void OnExportChrom(object obj)
        {
            if (Chromatogram?.Points == null)
            {
                return;
            }

            try
            {
                var dlg = new SaveFileDialog
                {
                    DefaultExt = ".idx",
                    Filter = "ASCII Chromatogram Data (.idx)|*.idx",
                    InitialDirectory = Path.GetDirectoryName(Chromatogram.Filename),
                    FileName = Path.GetFileNameWithoutExtension(Chromatogram.Title)
                };

                var result = ChromExplorer.Infrastructure.FileStorage.ShowSaveDialog(dlg, ChromExplorer.Infrastructure.StorageCategory.Exports, "chromatogram export");

                if (result == true)
                {
                    if (!ChromExplorer.Infrastructure.FileSaveGuard.CanSaveTo(dlg.FileName, "chromatogram export"))
                        return;

                    string parameter = "Chromatogram : " + Chromatogram.Title;

                    InsertLog("Chromatogram", $"Chromatogram Exported | Saved to: {dlg.FileName}", parameter, Path.GetFileName(dlg.FileName));

                    Chromatogram.SetAppStatus(true, "Exporting...");
                    using (TextWriter tw = new StreamWriter(dlg.FileName))
                    {
                        for (var i = 0; i < Chromatogram.Points.Length; i++)
                        {
                            tw.WriteLine("{0}\t{1}", Chromatogram.Points.Xvals[i]
                                , Chromatogram.Points.Yvals[i]);
                        }

                        tw.Flush();
                    }
                }
                else
                {
                    InsertLog("Chromatogram", "Chromatogram Export Cancelled", "");
                }
            }
            catch (Exception ex)
            {
                InsertLog("Chromatogram", "Chromatogram Export Failed", ex.Message);

                MessageBox.Show("Error", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Chromatogram.SetAppStatus(false, "Ready");
            }
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}