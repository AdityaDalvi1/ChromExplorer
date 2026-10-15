using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using log4net;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace ChromExplorer.OxyPlot.Demo
{
    public class SpectrumPlotModel : PlotModel, ISpectrumXRangeAware
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public const string XAxisKey = "xAxis";
        public const string YAxisKey = "yAxis";

        private DataRange _currentRangeX = DataRange.Undefined;
        private DataRange _currentRangeY = DataRange.Undefined;
        private readonly OxyPalette _palette;

        private double _zeroX = 0;
        private double _zeroY = 0;

        private MinMaxLineSeries _mainSeries = null;

        private readonly IOxyChromatogramModel _chromatogram = null;
        private int _rangeAddCount = 0;

        public SpectrumPlotModel(IOxyChromatogramModel chromatogram) : base()
        {
            log.Debug("ChromatogramPlotModel model has been created");

            
            _palette = OxyPalettes.Jet(8);


            _chromatogram = chromatogram;
            ConfigurePlot();

            XRangeAnnotations = new ObservableCollection<SpectrumXRangeAnnotation>();

            CreateSpectrumPlot();


            InvalidatePlot(false);

            _chromatogram.PropertyChanged += _chromatogram_PropertyChanged;

            AutoScaleY = true;
            this.Updating += SpectrumPlotModel_Updating;
        }

        private void SpectrumPlotModel_Updating(object sender, System.EventArgs e)
        {
            if (AutoScaleY)
            {
                AdjustYScaleToData(true);
            }
        }

        public void AdjustYScaleToData(bool forceZeroY)
        {
            var xAxis = GetAxis(XAxisKey);
            //log.DebugFormat("AdjustYScaleToData() x Range {0} - {1}", xAxis.ActualMinimum, xAxis.ActualMaximum);
            //log.DebugFormat("AdjustYScaleToData() x Range {0} - {1}", xAxis.ActualMinimum, xAxis.ActualMaximum);

            double minY = double.MinValue;
            double maxY = double.MinValue;

            int count = 0;
            foreach (var s in Series)
            {
                if (!s.IsVisible)
                    continue;

                var ss = s as MinMaxLineSeries;

                if (ss != null)
                {
                    var xRange = new DataRange(xAxis.ActualMinimum, xAxis.ActualMaximum);

                    if (xRange.Range == 0.0)
                    {
                        xRange = new DataRange(xAxis.Minimum, xAxis.Maximum);
                    }

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

                if (minY >= maxY)
                {
                    maxY = minY + 1;
                }

                SetYRange(minY, maxY);
            }
        }

        private void _chromatogram_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentSpectrum")
            {
                UpdateSpectrum();
            }
        }

        private void UpdateSpectrum()
        {
            if (_mainSeries == null)
                return;

            if (_chromatogram.CurrentSpectrum == null)
                return;

            _mainSeries.Points.Clear();

            foreach (var p in _chromatogram.CurrentSpectrum.Points.Points)
            {
                _mainSeries.Points.Add(new DataPoint(p.X, p.Y));
            }

            _mainSeries.InvalidateData();

            _zeroX = _mainSeries.DataRangeX.Minimum;

            InvalidatePlot(true);

            //Series.Add(_mainSeries);
            //AutoFitPlot();
        }

        public ObservableCollection<SpectrumXRangeAnnotation> XRangeAnnotations { get; private set; }

        public bool AutoScaleY { get; set; }

        public void AutoFitPlot()
        {
            InvalidateDataRange();
            ResetAllAxes();
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
                Key = XAxisKey,
                MajorGridlineStyle = LineStyle.Solid,
                Angle = 0
            });

            Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                AbsoluteMinimum = 0.0,
                Minimum = 0,
                Key = YAxisKey,
                MajorGridlineStyle = LineStyle.Solid,
                Angle = 270

            });
        }

        private void CreateSpectrumPlot()
        {
            if (_chromatogram.CurrentSpectrum == null)
                return;

            _mainSeries = new MinMaxLineSeries { Title = "TIC" };
            _mainSeries.StrokeThickness = UISettings.SpectrumLineThickness;
            _mainSeries.Color = OxyColors.DarkBlue;



            foreach (var p in _chromatogram.CurrentSpectrum.Points.Points)
            {
                _mainSeries.Points.Add(new DataPoint(p.X, p.Y));
            }

            _mainSeries.InvalidateData();

            _zeroX = _mainSeries.DataRangeX.Minimum;

            Series.Add(_mainSeries);
            AutoFitPlot();
        }


        private void InvalidateDataRange(bool forceZeroX = true, bool forceZeroY = true)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            int count = 0;
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

        private void SetXRange(double min, double max)
        {
            var axis = GetAxis(XAxisKey);
            axis.AbsoluteMinimum = min;
            axis.AbsoluteMaximum = max;

            axis.Minimum = min;
            axis.Maximum = max;
        }

        private void SetYRange(double min, double max)
        {
            var axis = GetAxis(YAxisKey);
            axis.AbsoluteMinimum = min;
            axis.AbsoluteMaximum = max;

            axis.Minimum = min;
            axis.Maximum = max;

            //log.DebugFormat("SetYRange() new range {0} - {1}", min, max);
        }



        public void AddRange(SpectrumXRangeAnnotation annotation)
        {
            XRangeAnnotations.Add(annotation);
            Annotations.Add(annotation);
            InvalidatePlot(false);
        }

        public void UpdateRange(SpectrumXRangeAnnotation annotation)
        {
            var color = _palette.Colors[_rangeAddCount % _palette.Colors.Count];
            annotation.Fill = OxyColor.FromAColor(120, color);
            _chromatogram.AddSelectedChromatogram(annotation);
            _rangeAddCount++;

        }

        public void RemoveAllRanges()
        {
            foreach (var xRangeAnnotation in XRangeAnnotations)
            {
                Annotations.Remove(xRangeAnnotation);
                _chromatogram.RemoveSelectedChromatogram(xRangeAnnotation);
            }

            XRangeAnnotations.Clear();

            InvalidatePlot(false);
        }

        public void RemoveRangeAt(double pos)
        {
            var toDelete = XRangeAnnotations.FirstOrDefault(xRangeAnnotation => xRangeAnnotation.MinimumX < pos && xRangeAnnotation.MaximumX > pos);

            if (toDelete != null)
            {
                Annotations.Remove(toDelete);
                XRangeAnnotations.Remove(toDelete);
                _chromatogram.RemoveSelectedChromatogram(toDelete);
                InvalidatePlot(false);
            }
        }

        public void CreateNewRange(SpectrumXRangeAnnotation annotation)
        {
            XRangeAnnotations.Add(annotation);
            Annotations.Add(annotation);
            InvalidatePlot(false);

            var color = _palette.Colors[_rangeAddCount % _palette.Colors.Count];
            annotation.Fill = OxyColor.FromAColor(120, color);
            _chromatogram.CreateSicChromatogram(annotation);
            _rangeAddCount++;
        }
    }
}