using System.Windows;
using System.Windows.Media;

namespace ChromExplorer.Plotter.Guestures
{
    internal class RectangleSelectionVisual : FrameworkElement
    {

        public RectangleSelectionVisual(Point firstPointP, Point secondPointP)
        {
            this.brush = new SolidColorBrush(Color.FromArgb(60, 100, 100, 100));
            this.pen = new Pen(Brushes.Black, 1.0);

            this._firstPoint = firstPointP;
            this._secondPoint = secondPointP;

            _visualForRect = new DrawingVisual();
            this.AddVisualChild(_visualForRect);
            this.AddLogicalChild(_visualForRect);

        }

        private Pen pen;
        public Pen Pen
        {
            get { return pen; }
            set { pen = value; }
        }

        private Brush brush;
        public Brush Brush
        {
            get { return brush; }
            set { brush = value; }
        }

        Point _firstPoint;
        Point _secondPoint;

        public Point FirstPoint
        {
            get { return _firstPoint; }
            set { _firstPoint = value; }
        }

        /// <summary>
        /// Get/Set the second point in the rectangle (could be before or after first point).
        /// </summary>
        public Point SecondPoint
        {
            get { return _secondPoint; }
            set
            {
                _secondPoint = value;
                DrawOnTheContext();
            }
        }

        DrawingVisual _visualForRect;

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        /// <summary>
        /// Provide a required override for the GetVisualChild method.
        /// </summary>
        /// <param name="index">Index of the child</param>
        /// <returns>The child visual</returns>
        protected override Visual GetVisualChild(int index)
        {
            
            return this._visualForRect;
        }

        /// <summary>
        /// Get the actual Rectangle of the rubber band.
        /// </summary>
        internal Rect SelectedRect
        {
            get { return new Rect(_firstPoint, SecondPoint); }
        }

        void DrawOnTheContext()
        {
            Rect bounds = SelectedRect;
            Point topLeftCorner = bounds.TopLeft;
            Point bottomRightCorner = bounds.BottomRight;
            Point topRightCorner = bounds.TopRight;
            Point bottomLeftCorner = bounds.BottomLeft;

            if (topLeftCorner.X != bottomRightCorner.X || topLeftCorner.Y != bottomRightCorner.Y)
            {
                using (DrawingContext drawingContext = this._visualForRect.RenderOpen())
                {
                    drawingContext.DrawRectangle(Brush, pen, bounds);
                }
            }
        }
    }
}
