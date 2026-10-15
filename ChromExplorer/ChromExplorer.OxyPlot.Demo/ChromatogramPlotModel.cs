using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using log4net;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

namespace ChromExplorer.OxyPlot.Demo
{
    public class ChromatogramPlotModel : PlotModel, IXRangeAware, ISpectrumMarkerAware, INotifyPropertyChanged
    {
        public const string XAxisKey = "xAxis";
        public const string YAxisKey = "yAxis";
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IOxyChromatogramModel _chromatogram;

        private DataRange _currentRangeX = DataRange.Undefined;
        private DataRange _currentRangeY = DataRange.Undefined;

        private MinMaxLineSeries _mainSeries;
        private int _selectedSpectrumIndex = -1;
        private LineAnnotation _selectedSpectrumLine;
        private readonly List<double> _spectrumPos = new List<double>();

        private readonly double _zeroX = 0;
        private readonly double _zeroY = 0;
        private double _ticArea;

        public ChromatogramPlotModel(IOxyChromatogramModel chromatogram)
        {
            log.Debug("ChromatogramPlotModel model has been created");

            _chromatogram = chromatogram;
            _chromatogram.PropertyChanged += _chromatogram_PropertyChanged;
            _chromatogram.SICDescriptors.CollectionChanged += SICDescriptors_CollectionChanged;
            ConfigurePlot();

            XRangeAnnotations = new ObservableCollection<XRangeAnnotation>();

            SetupPlot();

            SingleXRange = true;
            //CreateDummy();
            //SetupSpectrumPos();

            //ResetAllAxes();
            InvalidatePlot(false);
        }

        private void SICDescriptors_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            log.Debug($"SICDescriptors_CollectionChanged: action {e.Action}");

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    var desc = item as SICDescriptor;

                    if (desc != null)
                    {
                        Series.Add(desc.LineSeries);
                    }
                }

                ChromatogramPlotModel_Updating(this, EventArgs.Empty);
                InvalidatePlot(true);
                
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    var desc = item as SICDescriptor;

                    if (desc != null)
                    {
                        Series.Remove(desc.LineSeries);
                    }
                }

                ChromatogramPlotModel_Updating(this, EventArgs.Empty);
                InvalidatePlot(true);
            }
        }

        private void _chromatogram_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "InstrumentChromatogram")
            {
                SetupPlot();
                InvalidatePlot(true);
            }

            if (e.PropertyName == "SICDescriptors")
            {
                log.Debug("SICDescriptors has been changed");
            }
        }

        public bool AutoScaleY { get; set; }
        public bool SingleXRange { get; set; }
        public ObservableCollection<XRangeAnnotation> XRangeAnnotations { get; }

        public double TicArea
        {
            get => _ticArea;
            set
            {
                if (value.Equals(_ticArea)) return;
                _ticArea = value;
                OnPropertyChanged();
            }
        }

        void ISpectrumMarkerAware.MoveTo(double pos)
        {
            log.DebugFormat("MoveTo: pos = {0}", pos);

            ((IXRangeAware)this).RemoveAll();
            var index = _spectrumPos.BinarySearch(pos);

            log.DebugFormat("MoveTo: index = {0}", index);

            if (index < 0) index = ~index;

            if (index == _spectrumPos.Count)
                index = _spectrumPos.Count - 1;

            log.DebugFormat("MoveTo: index = {0} of {1}", index, _spectrumPos.Count);

            if (index >= 0 && index < _spectrumPos.Count && _selectedSpectrumLine != null)
            {
                _selectedSpectrumLine.X = _spectrumPos[index];
                _selectedSpectrumIndex = index;
                _chromatogram.SelectedSpectumIndex = _selectedSpectrumIndex;

                InvalidatePlot(false);
            }
        }

        void ISpectrumMarkerAware.StepLeft()
        {
            ((IXRangeAware)this).RemoveAll();
            if (_spectrumPos.Count == 0)
                return;

            var newIndex = _selectedSpectrumIndex - 1;
            if (newIndex < 0 || newIndex >= _spectrumPos.Count)
                return;

            _selectedSpectrumIndex = newIndex;
            _selectedSpectrumLine.X = _spectrumPos[_selectedSpectrumIndex];
            _chromatogram.SelectedSpectumIndex = _selectedSpectrumIndex;
            InvalidatePlot(false);
        }

        void ISpectrumMarkerAware.StepRight()
        {
            ((IXRangeAware)this).RemoveAll();
            if (_spectrumPos.Count == 0)
                return;

            var newIndex = _selectedSpectrumIndex + 1;
            if (newIndex < 0 || newIndex >= _spectrumPos.Count)
                return;

            _selectedSpectrumIndex = newIndex;
            _selectedSpectrumLine.X = _spectrumPos[_selectedSpectrumIndex];
            _chromatogram.SelectedSpectumIndex = _selectedSpectrumIndex;
            InvalidatePlot(false);
        }

        void IXRangeAware.Add(XRangeAnnotation annotation)
        {
            if (SingleXRange) ((IXRangeAware)this).RemoveAll();

            XRangeAnnotations.Add(annotation);
            Annotations.Add(annotation);
           

            InvalidatePlot(false);
        }

        void IXRangeAware.UpdateRange(XRangeAnnotation annotation)
        {
                
            if (_mainSeries != null)
            {
                TicArea = _mainSeries.Integrate(annotation.MinimumX, annotation.MaximumX);
            }
            else
            {
                TicArea = 0.0;
            }

            if(_chromatogram != null)
                _chromatogram.SetSelectedSpectrumIndexes(annotation.MinimumX, annotation.MaximumX);


            InvalidatePlot(false);
        }

        void IXRangeAware.RemoveAll()
        {
            foreach (var xRangeAnnotation in XRangeAnnotations) Annotations.Remove(xRangeAnnotation);

            XRangeAnnotations.Clear();

            if (_chromatogram != null)
                _chromatogram.SelectedSpectumIndex = _selectedSpectrumIndex;

            InvalidatePlot(true);
        }

        void IXRangeAware.RemoveAt(double pos)
        {
            var toDelete = XRangeAnnotations.FirstOrDefault(xRangeAnnotation =>
                xRangeAnnotation.MinimumX < pos && xRangeAnnotation.MaximumX > pos);

            if (toDelete != null)
            {
                Annotations.Remove(toDelete);
                XRangeAnnotations.Remove(toDelete);
                if (_chromatogram != null)
                    _chromatogram.SelectedSpectumIndex = _selectedSpectrumIndex;

                
                InvalidatePlot(true);
            }
        }

        private void SetupPlot()
        {
            if (_chromatogram == null)
                return;

            if (_mainSeries != null)
                Series.Remove(_mainSeries);

            _mainSeries = new MinMaxLineSeries { Title = "TIC" };
            _mainSeries.StrokeThickness = UISettings.ChromLineThickness;
            _mainSeries.Color = OxyColors.DarkBlue;


            foreach (var p in _chromatogram.InstrumentChromatogram)
                _mainSeries.Points.Add(new DataPoint(p.Time, p.TIC));

            _mainSeries.InvalidateData();

            Series.Add(_mainSeries);
            AutoFitPlot();

            SetupSpectrumPos();

            if (_spectrumPos.Count > 0)
            {
                if (_selectedSpectrumLine == null)
                {
                    _selectedSpectrumLine = new LineAnnotation
                        { Type = LineAnnotationType.Vertical, X = _spectrumPos[0] };
                    _selectedSpectrumIndex = 0;
                    _chromatogram.SelectedSpectumIndex = _selectedSpectrumIndex;
                    Annotations.Add(_selectedSpectrumLine);
                }
            }
        }

        private void SetupSpectrumPos()
        {
            if (_mainSeries != null)
            {
                _spectrumPos.Clear();
                foreach (var point in _mainSeries.Points) _spectrumPos.Add(point.X);
            }
        }

        private void ConfigurePlot()
        {
            Background = OxyColors.White;
            PlotAreaBackground = OxyColors.White;
            AutoScaleY = true;

            Axes.Clear();
            Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                AbsoluteMinimum = 0.0,
                Minimum = 0,
                Key = "xAxis",
                MajorGridlineStyle = LineStyle.Solid,
                Angle = 0
            });

            Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                AbsoluteMinimum = 0.0,
                Minimum = 0,
                Key = "yAxis",
                MajorGridlineStyle = LineStyle.Solid,
                Angle = 270
            });


            ShowTitle(false);

            var l = new Legend
            {
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.RightTop,
                LegendBackground = OxyColor.FromAColor(200, OxyColors.White),
                LegendBorder = OxyColors.Black,
                ShowInvisibleSeries = true
            };

            Legends.Add(l);

            Updating += ChromatogramPlotModel_Updating;
        }

        private void ChromatogramPlotModel_Updating(object sender, EventArgs e)
        {
            if (AutoScaleY) AdjustYScaleToData(true);
        }

        public void AdjustYScaleToData(bool forceZeroY)
        {
            var xAxis = GetAxis(XAxisKey);
            //log.DebugFormat("AdjustYScaleToData() x Range {0} - {1}", xAxis.ActualMinimum, xAxis.ActualMaximum);
            //log.DebugFormat("AdjustYScaleToData() x Range {0} - {1}", xAxis.ActualMinimum, xAxis.ActualMaximum);

            var minY = double.MinValue;
            var maxY = double.MinValue;

            var count = 0;
            foreach (var s in Series)
            {
                if (!s.IsVisible)
                    continue;

                var ss = s as MinMaxLineSeries;

                if (ss != null)
                {
                    var xRange = new DataRange(xAxis.ActualMinimum, xAxis.ActualMaximum);

                    if (xRange.Range == 0.0) xRange = new DataRange(xAxis.Minimum, xAxis.Maximum);

                    var yRange = ss.GetYRange(xRange.Minimum, xRange.Maximum);

                    if (yRange.Minimum < minY)
                        minY = yRange.Minimum;

                    if (yRange.Maximum > maxY)
                        maxY = yRange.Maximum;

                    count++;
                }
            }

            if (count > 0)
            {
                if (forceZeroY)
                    minY = _zeroY;

                if (minY >= maxY) maxY = minY + 1;

                SetYRange(minY, maxY);
            }
        }

        public void ShowTitle(bool show)
        {
            Title = show ? "Chromatogram" : null;
            InvalidatePlot(false);
        }

        public void SetXRange(double min, double max)
        {
            var axis = GetAxis(XAxisKey);
            axis.AbsoluteMinimum = min;
            axis.AbsoluteMaximum = max;

            axis.Minimum = min;
            axis.Maximum = max;
        }

        public void SetYRange(double min, double max)
        {
            var axis = GetAxis(YAxisKey);
            axis.AbsoluteMinimum = min;
            axis.AbsoluteMaximum = max;

            axis.Minimum = min;
            axis.Maximum = max;

            //log.DebugFormat("SetYRange() new range {0} - {1}", min, max);
        }

        public void InvalidateDataRange(bool forceZeroX = true, bool forceZeroY = true)
        {
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;

            var count = 0;
            foreach (var s in Series)
            {
                if (!s.IsVisible)
                    continue;

                var ss = s as MinMaxLineSeries;

                if (ss != null)
                {
                    var xRange = ss.DataRangeX;
                    var yRange = ss.GetYRange(xRange.Minimum, xRange.Maximum);

                    if (xRange.Minimum < minX)
                        minX = xRange.Minimum;

                    if (yRange.Minimum < minY)
                        minY = yRange.Minimum;

                    if (xRange.Maximum > maxX)
                        maxX = xRange.Maximum;

                    if (yRange.Maximum > maxY)
                        maxY = yRange.Maximum;

                    count++;
                }
            }

            if (count > 0)
            {
                if (forceZeroX)
                    minX = _zeroX;

                if (forceZeroY)
                    minY = _zeroY;

                SetXRange(minX, maxX);
                SetYRange(minY, maxY);
            }

            //InvalidatePlot(false);
        }

        public void AutoFitPlot()
        {
            InvalidateDataRange();
            ResetAllAxes();
        }

        private void CreateDummy(int n = 3)
        {
            AddSeriesFromFile("TIC", "TestData//chrom_tic.idx", true);
            AddSeriesFromFile("MTC", "TestData//chrom_99.idx");
            AddSeriesFromFile("BTX", "TestData//chrom_194.idx");
        }

        private void AddSeriesFromFile(string title, string filename, bool setAsMain = false)
        {
            var points = DataSetLoader.Load(filename);
            var s = new MinMaxLineSeries { Title = title };
            s.Points.AddRange(points);
            s.StrokeThickness = 1;

            s.InvalidateData();

            var xRange = s.DataRangeX;
            var yRange = s.GetYRange(xRange.Minimum, xRange.Maximum);

            if (!_currentRangeX.IsDefined()) _currentRangeX = new DataRange(0, xRange.Maximum);

            if (!_currentRangeY.IsDefined()) _currentRangeY = new DataRange(0, yRange.Maximum);

            var maxX = _currentRangeX.Maximum;

            if (!_currentRangeX.Contains(maxX))
            {
                maxX = xRange.Maximum;
                _currentRangeX = new DataRange(0, xRange.Maximum);
            }

            var yMax = _currentRangeY.Maximum;

            if (!_currentRangeY.Contains(yMax))
            {
                yMax = yRange.Maximum;
                _currentRangeY = new DataRange(0, yRange.Maximum);
            }


            SetXRange(0, _currentRangeX.Maximum);
            SetYRange(0, _currentRangeY.Maximum);

            Series.Add(s);

            if (setAsMain)
                _mainSeries = s;
        }


        public void DebugPrint()
        {
            log.DebugFormat("DefaultColors.Count = {0}", DefaultColors.Count);
            log.DebugFormat("Axes.Count = {0}", Axes.Count);

            foreach (var a in Axes) log.DebugFormat("Axes[{0}] = {1}, {2}", a.Key, a.Title, a.Position);

            var count = 0;
            foreach (var s in Series)
            {
                log.DebugFormat("Series[{0}].IsVisible = {1}", count, s.IsVisible);
                count++;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }


        public void DebugSaveAllChromatograms()
        {
            foreach (var series in Series)
            {
                var mms = series as MinMaxLineSeries;

                if (mms != null)
                {
                    log.Info($"Saving series {series.Title}");
                    mms.ExportToCsvFile(series.Title + ".csv");
                }
            }
        }
    }

    public interface ISpectrumMarkerAware
    {
        void MoveTo(double pos);
        void StepLeft();
        void StepRight();
    }
}