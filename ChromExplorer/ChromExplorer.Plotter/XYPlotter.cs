using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChromExplorer.DataModel.Core;
using ChromExplorer.Plotter.Axes;
using ChromExplorer.Plotter.Markers;
using log4net;

namespace ChromExplorer.Plotter
{
    public class XYPlotter : Control
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        class Range
        {
            public double Min;
            public double Max;
        }

        static XYPlotter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(XYPlotter), new FrameworkPropertyMetadata(typeof(XYPlotter)));
        }

        private CoordinateTransform transform;
        private IntervalMinMaxFilter filter;
        private List<ZoomConstrain> constrains;
        private NumericTickProvider xTickProvider;
        private NumericTickProvider yTickProvider;

        private Typeface markerTypeface = new Typeface("Arial");
        private double fontSize = 12;
        private Brush fontBrush = Brushes.Black;

        private Brush selectedBrush = Brushes.White;
        private Pen selectedPen = new Pen(Brushes.Blue, 1);
        private Pen selectedPointsPen = new Pen(Brushes.DarkBlue, 3);
        private int selectedRadius = 4;

        public int horizontalTickSpacing = 60;
        public int verticalTickSpacing = 40;

        private double xmin;
        private double xmax;
        private bool filteringEnable;
        private bool zoommed;
        private bool autoRescaleY = true;
        private double scale;

        private double _integrationStartTime = double.NaN;
        private double _integrationEndTime = double.NaN;
        private bool _isIntegrationActive = false;

        private double _noiseRegionStartTime = double.NaN;
        private double _noiseRegionEndTime = double.NaN;
        private bool _isNoiseRegionActive = false;

        public double NoiseRegionStartTime
        {
            get { return _noiseRegionStartTime; }
            set { _noiseRegionStartTime = value; InvalidateVisual(); }
        }

        public double NoiseRegionEndTime
        {
            get { return _noiseRegionEndTime; }
            set { _noiseRegionEndTime = value; InvalidateVisual(); }
        }

        public bool IsNoiseRegionActive
        {
            get { return _isNoiseRegionActive; }
            set { _isNoiseRegionActive = value; InvalidateVisual(); }
        }

        public double IntegrationStartTime
        {
            get { return _integrationStartTime; }
            set { _integrationStartTime = value; InvalidateVisual(); }
        }

        public double IntegrationEndTime
        {
            get { return _integrationEndTime; }
            set { _integrationEndTime = value; InvalidateVisual(); }
        }

        public bool IsIntegrationActive
        {
            get { return _isIntegrationActive; }
            set { _isIntegrationActive = value; InvalidateVisual(); }
        }
        
        private bool keepVisualRect;
        
        DataRect visibleDataRect;

        public HorizontalAxisControl XAxis { get; set; }
        public VerticalAxisControl YAxis { get; set; }

        public Canvas MarkerCanvas { get; set; }
        private MarkerCollection _markers;
        public MarkerCollection Markers
        {
            get { return _markers; }
            set
            {
                _markers = value;
                //InvalidateVisual();
            }
        }

        public SimplePlotter Container { get; set; }

        public XYPlotter()
        {
            this.LinePen = new Pen(Brushes.Blue, 1);
            this.LinePen2 = new Pen(Brushes.Red, 1);
            
            this.GridPen = new Pen(Brushes.LightGray, 1);
                
            filteringEnable = true;
            filter = new IntervalMinMaxFilter(IntervalMinMaxFilter.Mode.MinMax, 1.0);
            filter.Intervals = 2 * 1024;

            scale = 1.2;
            UpdateConstrains();
            
            xTickProvider = new NumericTickProvider();
            yTickProvider = new NumericTickProvider();

            visibleDataRect = new DataRect() { XMin = 0.0, XMax = 1.0, YMin = 0.0, YMax = 1.0 };
            zoommed = false;
            keepVisualRect = true;
        }

        public bool AutoScaleY
        {
            get
            {
                return autoRescaleY;
            }
            set
            {
                autoRescaleY = value;

                UpdateConstrains();
            }
        }

        private void UpdateConstrains()
        {
            constrains = new List<ZoomConstrain>();
            
            if(autoRescaleY)
                constrains.Add(new LocalMaximumConstrain());
            
            constrains.Add(new FullDataHeightConstrain(scale));
        }

        private SimpleDataSource dataSource;

        public SimpleDataSource DataSource
        {
            get
            {
                return dataSource;
            }

            set
            {
                bool keep = false;
                if (keepVisualRect && dataSource != null)
                {
                    keep = true;
                }

                dataSource = value;

                if (!keep)
                {
                    xmin = dataSource.Bounds.XMin;
                    xmax = dataSource.Bounds.XMax;
                }

                this.zoommed = false;
                
                InvalidateVisual();
            }          
        }

        private SimpleDataSource dataSource2;

        public SimpleDataSource DataSource2
        {
            get
            {
                return dataSource2;
            }

            set
            {
                dataSource2 = value;

                /*
                bool keep = false;
                if (keepVisualRect && dataSource != null)
                {
                    keep = true;
                }

                dataSource2 = value;

                if (!keep)
                {
                    xmin = dataSource2.Bounds.XMin;
                    xmax = dataSource2.Bounds.XMax;
                }

                this.zoommed = false;

                InvalidateVisual();
                 * */
            }
        }

        public void RebuildTicks()
        {
            int ticks = (int)(this.ActualWidth / horizontalTickSpacing);
            int yticks = (int)(this.ActualHeight / verticalTickSpacing);


            xTickProvider.Update(visibleDataRect.XMin, visibleDataRect.XMax, ticks);
            yTickProvider.Update(visibleDataRect.YMin, visibleDataRect.YMax, yticks);

            if (!zoommed)
            {
                visibleDataRect.XMin = xTickProvider.Min;
                visibleDataRect.XMax = xTickProvider.Max;

                // Mirror the X snap above for Y: without this, the transform keeps using the raw
                // (un-rounded) Y bounds while the tick positions are generated for the wider
                // "nice" range, so most/all of them land outside the transform's actual Y span and
                // get culled by VerticalAxisControl's on-screen bounds check — leaving the axis
                // showing almost no intensity labels.
                visibleDataRect.YMin = yTickProvider.Min;
                visibleDataRect.YMax = yTickProvider.Max;
            }
            /*

            if (zoommed)
            {
                            
            }
            else
            {
                if (dataSource != null)
                {
                    xTickProvider.Update(dataSource.Bounds.XMin, dataSource.Bounds.XMax, ticks);
                    yTickProvider.Update(dataSource.Bounds.YMin, dataSource.Bounds.YMax, yticks);
                    
                    this.xmin = xTickProvider.Min;
                    this.xmax = xTickProvider.Max;
                }
            }
             * */
        }

        public DataRect Bounds { get { return dataSource != null ? dataSource.Bounds : new DataRect(); } }
        public DataRect Bounds2 { get { return dataSource2 != null ? dataSource2.Bounds : new DataRect(); } }

        public void Restore()
        {
            xmin = Bounds.XMin;
            xmax = Bounds.XMax;

            this.zoommed = false;
            InvalidateVisual();
        }

        public void Replot()
        {
            InvalidateVisual();
        }

        public void SetRange(double xmin, double xmax)
        {
            this.xmin = xmin;
            this.xmax = xmax;
            this.zoommed = true;

            InvalidateVisual();
        }

        //private readonly Stack<Rect> zooms = new Stack<Rect>();
        public void ZoomToRect(Rect r)
        {
            if (transform != null)
            {

                Point topLeft = transform.ScreenToData(r.TopLeft);
                Point topRight = transform.ScreenToData(r.TopRight);

                double xmin = topLeft.X;
                double xmax = topRight.X;

                if (xmin > xmax)
                {
                    xmin = topRight.X;
                    xmax = topLeft.X;
                }

                SetRange(xmin, xmax);
               
            }
        }

        public void Unzoom()
        {
            Restore();
        }
        
        public Pen LinePen { get; set; }
        public Pen LinePen2 { get; set; }
        
        public Pen GridPen { get; set; }

        private int _selectedIndex = -1;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                InvalidateVisual();
            }
        }

        private IList<int> _selectedIndexes;
        public IList<int> SelectedIndexes
        {
            get { return _selectedIndexes; }
            set
            {
                _selectedIndexes = value;
                InvalidateVisual();
            }
        }

        StreamGeometry streamGeometry = new StreamGeometry();
        StreamGeometry streamGeometry2 = new StreamGeometry();
        Stopwatch stopwatch = new Stopwatch();
        protected override void OnRender(DrawingContext drawingContext)
        {
            var screenPoints = GetScreenPoints(dataSource, new Rect(0, 0, this.ActualWidth, this.ActualHeight));

            DrawGridLines(drawingContext);

            if (_isIntegrationActive && dataSource != null && !double.IsNaN(_integrationStartTime) && !double.IsNaN(_integrationEndTime))
            {
                DrawIntegrationArea(drawingContext);
            }

            if (_isNoiseRegionActive && dataSource != null && !double.IsNaN(_noiseRegionStartTime) && !double.IsNaN(_noiseRegionEndTime))
            {
                DrawNoiseRegion(drawingContext);
            }

            if (screenPoints.Count > 0)
            {
                using (StreamGeometryContext context = streamGeometry.Open())
                {
                    context.BeginFigure(screenPoints[0], false, false);
                    context.PolyLineTo(screenPoints, true, true);
                }

                Brush brush = null;
                Pen pen = LinePen;

                drawingContext.DrawGeometry(brush, pen, streamGeometry);
            }

            var screenPoints2 = GetScreenPoints2(dataSource2, new Rect(0, 0, this.ActualWidth, this.ActualHeight));

            if (screenPoints2.Count > 0)
            {
                using (StreamGeometryContext context = streamGeometry2.Open())
                {
                    context.BeginFigure(screenPoints2[0], false, false);
                    context.PolyLineTo(screenPoints2, true, true);
                }

                Brush brush = null;
                Pen pen = LinePen2;

                drawingContext.DrawGeometry(brush, pen, streamGeometry2);
            }

            DrawSelectedMarker(drawingContext, screenPoints);

            if (this.MarkerCanvas != null)
            {
                DrawMarkers(drawingContext);
            }

            if (this.XAxis != null)
            {
                this.XAxis.DrawAxis(xTickProvider, transform);
            }

            if (this.YAxis != null)
            {
                this.YAxis.DrawAxis(yTickProvider, transform);
            }

            


        }

        private void DrawGridLines(DrawingContext drawingContext)
        {
            if (transform == null || xTickProvider.Positions == null)
                return;
            
            foreach (double pos in xTickProvider.Positions)
            {
                double x = transform.DataToScreen(new Point(pos, 0.0)).X;

                if (x > 0 && x < this.ActualWidth)
                {
                    Point start = new Point(x, 0.0);
                    Point end = new Point(x, this.ActualHeight);

                    drawingContext.DrawLine(this.GridPen, start, end);
                }
            }

            foreach (double pos in yTickProvider.Positions)
            {
                double y = transform.DataToScreen(new Point(0.0, pos)).Y;

                if (y > 0 && y < this.ActualHeight)
                {
                    Point start = new Point(0.0, y);
                    Point end = new Point(this.ActualWidth, y);

                    drawingContext.DrawLine(this.GridPen, start, end);
                }
            }
            
        }

        private List<Point> GetScreenPoints(SimpleDataSource points, Rect screenRect)
        {
            List<Point> result = new List<Point>(2048);

            if (this.dataSource != null)
            {

                visibleDataRect = new DataRect() { XMin = this.xmin, XMax = this.xmax, 
                    YMin = Bounds.YMin < Bounds2.YMin ? Bounds.YMin : Bounds2.YMin,
                    YMax = Bounds.YMax > Bounds2.YMax ? Bounds.YMax : Bounds2.YMax
                };

                List<Point> toPLot = null;

                if (filteringEnable)
                {
                    filter.SetScreenRect(visibleDataRect.ToRect());
                    toPLot = filter.Filter(points);
                }
                else
                {
                    toPLot = points.Points;
                }

                foreach (ZoomConstrain c in constrains)
                {
                    visibleDataRect = dataSource2 != null ? c.Apply(visibleDataRect, toPLot, dataSource2.Points) : c.Apply(visibleDataRect, toPLot);
                }

                RebuildTicks();
                if (Container != null)
                {
                    this.Container.Visible = visibleDataRect;
                }

                transform = new CoordinateTransform(visibleDataRect, screenRect);

                foreach (Point p in toPLot)
                {
                    if (p.X >= this.xmin && p.X <= this.xmax)
                        result.Add(transform.DataToScreen(p));
                }
            }
            else
            {
                RebuildTicks();
                transform = new CoordinateTransform(visibleDataRect, screenRect);
            }
            
            
            return result;
        }

        private List<Point> GetScreenPoints2(SimpleDataSource points, Rect screenRect)
        {
            List<Point> result = new List<Point>(2048);

            if (this.dataSource != null && visibleDataRect != null && transform != null)
            {
                //visibleDataRect = new DataRect() { XMin = this.xmin, XMax = this.xmax, YMin = Bounds.YMin, YMax = Bounds.YMax };

                List<Point> toPLot = null;

                if (filteringEnable)
                {
                    filter.SetScreenRect(visibleDataRect.ToRect());
                    toPLot = filter.Filter(points);
                }
                else
                {
                    toPLot = points.Points;
                }

                //foreach (ZoomConstrain c in constrains)
                //{
                //    visibleDataRect = c.Apply(visibleDataRect, toPLot);
                //}

                //RebuildTicks();
                //if (Container != null)
                //{
                //    this.Container.Visible = visibleDataRect;
                //}

                //transform = new CoordinateTransform(visibleDataRect, screenRect);

                foreach (Point p in toPLot)
                {
                    if (p.X >= this.xmin && p.X <= this.xmax)
                        result.Add(transform.DataToScreen(p));
                }
            }
            else
            {
                //RebuildTicks();
                //transform = new CoordinateTransform(visibleDataRect, screenRect);
            }


            return result;
        }

        private void DrawIntegrationArea(DrawingContext drawingContext)
        {
            if (dataSource == null || transform == null) return;

            double start = System.Math.Min(_integrationStartTime, _integrationEndTime);
            double end = System.Math.Max(_integrationStartTime, _integrationEndTime);

            // Filter points in the integration range
            var xvals = dataSource.Xvals;
            var yvals = dataSource.Yvals;

            int startIndex = -1;
            int endIndex = -1;
            for (int i = 0; i < xvals.Length; i++)
            {
                if (startIndex == -1 && xvals[i] >= start) startIndex = i;
                if (xvals[i] <= end) endIndex = i;
            }

            if (startIndex < 0 || endIndex < 0 || endIndex - startIndex < 1)
                return;

            // Compute endpoint Y values by linear interpolation (to avoid dependency on exact sample alignment).
            double yStart = GetInterpolatedYFromArrays(xvals, yvals, start);
            double yEnd = GetInterpolatedYFromArrays(xvals, yvals, end);

            var topPoints = new List<Point>();
            var baselinePoints = new List<Point>();

            // Start point
            topPoints.Add(transform.DataToScreen(new Point(start, yStart)));
            baselinePoints.Add(transform.DataToScreen(new Point(start, yStart)));

            // Interior sampled points
            for (int i = startIndex; i <= endIndex; i++)
            {
                double x = xvals[i];
                double y = yvals[i];
                if (x < start || x > end) continue;

                // Linear baseline between endpoints, but allow it to follow dips in the actual waveform.
                double t = (end - start) > 1e-12 ? (x - start) / (end - start) : 0;
                double yLine = yStart + t * (yEnd - yStart);
                double yBase = y < yLine ? y : yLine;

                topPoints.Add(transform.DataToScreen(new Point(x, y)));
                baselinePoints.Add(transform.DataToScreen(new Point(x, yBase)));
            }

            // End point
            topPoints.Add(transform.DataToScreen(new Point(end, yEnd)));
            baselinePoints.Add(transform.DataToScreen(new Point(end, yEnd)));

            if (topPoints.Count > 1 && baselinePoints.Count == topPoints.Count)
            {
                StreamGeometry geo = new StreamGeometry();
                using (StreamGeometryContext ctx = geo.Open())
                {
                    // Begin at baseline start, trace the peak (top) curve, then trace the baseline back.
                    ctx.BeginFigure(baselinePoints[0], true, true);

                    for (int i = 0; i < topPoints.Count; i++)
                        ctx.LineTo(topPoints[i], true, false);

                    for (int i = baselinePoints.Count - 1; i >= 0; i--)
                        ctx.LineTo(baselinePoints[i], true, false);
                }

                // Add a clip to ensure no spill outside the plotter area
                drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight)));

                Brush fillBrush = new SolidColorBrush(Color.FromArgb(100, 0, 0, 255)); // Semi-transparent blue
                drawingContext.DrawGeometry(fillBrush, new Pen(Brushes.Blue, 1), geo);

                drawingContext.Pop(); // Remove the clip
            }
        }

        private void DrawNoiseRegion(DrawingContext drawingContext)
        {
            if (dataSource == null || transform == null) return;

            double start = Math.Min(_noiseRegionStartTime, _noiseRegionEndTime);
            double end = Math.Max(_noiseRegionStartTime, _noiseRegionEndTime);

            // Filter points in the noise region
            var xvals = dataSource.Xvals;
            var yvals = dataSource.Yvals;

            List<Point> regionPoints = new List<Point>();
            for (int i = 0; i < xvals.Length; i++)
            {
                if (xvals[i] >= start && xvals[i] <= end)
                {
                    regionPoints.Add(transform.DataToScreen(new Point(xvals[i], yvals[i])));
                }
            }

            if (regionPoints.Count > 1)
            {
                StreamGeometry geo = new StreamGeometry();
                using (StreamGeometryContext ctx = geo.Open())
                {
                    ctx.BeginFigure(regionPoints[0], false, false);
                    foreach (var p in regionPoints)
                    {
                        ctx.LineTo(p, true, false);
                    }
                }

                // Dark blue line
                Pen darkBluePen = new Pen(Brushes.DarkBlue, 3);
                drawingContext.DrawGeometry(null, darkBluePen, geo);
            }
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
            double t = (x - x0) / dx;
            return y0 + t * (y1 - y0);
        }

        private void DrawMarkers(DrawingContext drawingContext)
        {
            if (Markers == null)
                return;

            int peaks = 0;
            foreach (IMarker marker in Markers)
            {
                if (marker.Position.X >= this.visibleDataRect.XMin && marker.Position.X <= this.visibleDataRect.XMax)
                {
                    var pos = transform.DataToScreen(marker.Position);
                    var text = new FormattedText(marker.Text, CultureInfo.InvariantCulture,
                            FlowDirection.LeftToRight,
                            markerTypeface, fontSize, fontBrush);

                    var textPosition = GetTextPosition(marker, text);

                    if (marker.Orientation == MarkerOrientation.Horizontal)
                    {
                        
                        drawingContext.DrawText(text, textPosition);
                    }

                    if (marker.Orientation == MarkerOrientation.Vertical)
                    {
                        var textCenter = GetTextCenter(marker, text);
                        var textLeft = new Point(textCenter.X - text.Width / 2.0, textCenter.Y - text.Height / 2.0);

                        var rt = new RotateTransform(-90, textCenter.X, textCenter.Y);
                        var tr = new TranslateTransform(0, -text.Width / 2.0);
                        drawingContext.PushTransform(tr);
                        drawingContext.PushTransform(rt);
                        
                        drawingContext.DrawText(text, textPosition);

                        drawingContext.Pop();
                        drawingContext.Pop();

                    }

                    if (marker.Placement == MarkerPlacement.Top)
                    {
                        // Draw a line connecting the peak to the label
                        drawingContext.DrawLine(LinePen, pos, new Point(pos.X, pos.Y - marker.ScreenOffset));
                    }
                    else
                    {
                        drawingContext.DrawLine(LinePen, new Point(pos.X, pos.Y - 1), new Point(pos.X, pos.Y + 10));
                    }
                    peaks++;
                }
            }
        }

        private void DrawSelectedMarker(DrawingContext drawingContext, List<Point> screenPoints)
        {
            if (this.transform == null)
                return;

            if (DataSource == null)
                return;

            if (SelectedIndex >= 0 && SelectedIndex < DataSource.Length)
            {
                var point = new Point(DataSource.Xvals[SelectedIndex], DataSource.Yvals[SelectedIndex]);
                DrawPointMarker(drawingContext, point, true);
            }

            if(SelectedIndexes != null)
            {

                var size = SelectedIndexes.Count;
                log.DebugFormat("DrawSelectedMarker: will mark {0} indexes", size);
         
                if (size >= 2)
                {

                    var p1 = new Point(DataSource.Xvals[SelectedIndexes[0]], DataSource.Yvals[SelectedIndexes[0]]);
                    var p2 = new Point(DataSource.Xvals[SelectedIndexes[size - 1]],
                                       DataSource.Yvals[SelectedIndexes[size - 1]]);

                    var pp1 = transform.DataToScreen(p1);
                    var pp2 = transform.DataToScreen(p2);

                    var selectedPoints = (from point in screenPoints
                                          where point.X >= pp1.X && point.X <= pp2.X
                                          select new Point(point.X, point.Y + 1)).ToList();

                    if (selectedPoints.Count > 0)
                    {
                        for (int i = 0; i < selectedPoints.Count - 1; i++)
                        {
                            Brush brush = null;
                            Pen pen = selectedPointsPen;

                            drawingContext.DrawLine(pen, selectedPoints[i], selectedPoints[i + 1]);
                        }
                    }
                }
                //drawingContext.DrawRectangle(selectedBrush, selectedPen, new Rect(pp1.X, 0, pp2.X - pp1.X, 10));

                /*
                foreach (var index in SelectedIndexes)
                {
                    var p = new Point(DataSource.Xvals[index], DataSource.Yvals[index]);
                    DrawPointMarker(drawingContext, p, true);
                }
                 * */
            }

            
        }

        private void DrawPointMarker(DrawingContext drawingContext, Point point, bool dropLine)
        {
            if (point.X >= this.visibleDataRect.XMin && point.X <= this.visibleDataRect.XMax)
            {
                Point pos = transform.DataToScreen(point);
                Point aPoint = transform.DataToScreen(new Point(point.X, visibleDataRect.YMin));

                drawingContext.DrawEllipse(selectedBrush, selectedPen, pos, selectedRadius, selectedRadius);
                drawingContext.DrawLine(selectedPen, new Point(pos.X, pos.Y + selectedRadius), aPoint);
            }
        }

        private Point GetTextPosition(IMarker marker, FormattedText text)
        {
            if (this.transform == null)
                return marker.Position;

            Point pos = transform.DataToScreen(marker.Position);

            switch (marker.Placement)
            {
                case MarkerPlacement.Bottom:
                    pos.X = pos.X - text.Width / 2.0;
                    pos.Y = pos.Y + text.Height + marker.ScreenOffset;
                    break;

                case MarkerPlacement.BottomRight:
                    pos.X = pos.X + marker.ScreenOffset;
                    pos.Y = pos.Y + text.Height + marker.ScreenOffset;
                    break;
 
                case MarkerPlacement.BottomLeft:
                    pos.X = pos.X - text.Width - marker.ScreenOffset;
                    pos.Y = pos.Y + text.Height + marker.ScreenOffset;
                    break;

                case MarkerPlacement.Left:
                    pos.X = pos.X - text.Width - marker.ScreenOffset;
                    pos.Y = pos.Y - text.Height / 2.0;
                    break;

                case MarkerPlacement.Right:
                    pos.X = pos.X + marker.ScreenOffset;
                    pos.Y = pos.Y - text.Height / 2.0;
                    break;

                case MarkerPlacement.Top:
                    pos.X = pos.X - text.Width / 2.0;
                    pos.Y = pos.Y - text.Height - marker.ScreenOffset;
                    break;

                case MarkerPlacement.TopRight:
                    pos.X = pos.X - marker.ScreenOffset;
                    pos.Y = pos.Y - text.Height - marker.ScreenOffset;
                    break;

                case MarkerPlacement.TopLeft:
                    pos.X = pos.X - text.Width - marker.ScreenOffset;
                    pos.Y = pos.Y - text.Height - marker.ScreenOffset;
                    break;
            }

            return pos;
        }

        private Point GetTextCenter(IMarker marker, FormattedText text)
        {
            if (this.transform == null)
                return marker.Position;

            Point pos = transform.DataToScreen(marker.Position);

            switch (marker.Placement)
            {
                case MarkerPlacement.Bottom:
                    pos.X = pos.X;
                    pos.Y = pos.Y + text.Height * 1.5 + marker.ScreenOffset;
                    break;

                case MarkerPlacement.BottomRight:
                    pos.X = pos.X + text.Width / 2.0 + marker.ScreenOffset;
                    pos.Y = pos.Y + text.Height * 1.5 + marker.ScreenOffset;
                    break;

                case MarkerPlacement.BottomLeft:
                    pos.X = pos.X - text.Width / 2.0 - marker.ScreenOffset;
                    pos.Y = pos.Y + text.Height * 1.5 + marker.ScreenOffset;
                    break;

                case MarkerPlacement.Left:
                    pos.X = pos.X - text.Width / 2.0 - marker.ScreenOffset;
                    pos.Y = pos.Y;
                    break;

                case MarkerPlacement.Right:
                    pos.X = pos.X + marker.ScreenOffset + text.Width / 2.0;
                    pos.Y = pos.Y;
                    break;

                case MarkerPlacement.Top:
                    pos.X = pos.X;
                    pos.Y = pos.Y - text.Height / 2.0 - marker.ScreenOffset;
                    break;

                case MarkerPlacement.TopRight:
                    pos.X = pos.X + text.Width / 2.0 - marker.ScreenOffset;
                    pos.Y = pos.Y - text.Height / 2.0 - marker.ScreenOffset;
                    break;

                case MarkerPlacement.TopLeft:
                    pos.X = pos.X - text.Width / 2.0 - marker.ScreenOffset;
                    pos.Y = pos.Y - text.Height / 2.0 - marker.ScreenOffset;
                    break;
            }

            return pos;
        }


        public int GetNearestIndex(Point pos)
        {
            if (transform != null && DataSource != null)
            {
                var p = transform.ScreenToData(pos);


                return DataSource.GetIndexByX(p.X);
            }

            return -1;
        }
    }
}
