using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChromExplorer.Plotter.Axes;

namespace ChromExplorer.Plotter
{

    public class HorizontalAxisControl : Control
    {
        static HorizontalAxisControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HorizontalAxisControl), new FrameworkPropertyMetadata(typeof(HorizontalAxisControl)));
        }

        private NumericTickProvider tickProvider;
        private CoordinateTransform transform;

        private Typeface labelTypeface = new Typeface("Arial");
        private double fontSize = 12;
        private Brush fontBrush = Brushes.Black;
        private double fontOffset = 2;
        private bool drawMinorTicks = true;
        private int minorTickCount = 9;
        private int middleTickIndex = 4;
        private double minorTickSize = 3;
        private double middleTickSize = 5;

        private double majorTickSize = 10;


        public HorizontalAxisControl()
        {
            this.Height = 32;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            
            this.TickPen = new Pen(Brushes.Black, 1);
        }

        public Pen TickPen { get; set; }

        public void DrawAxis(NumericTickProvider tickProvider, CoordinateTransform transform)
        {
            this.tickProvider = tickProvider;
            this.transform = transform;

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (transform == null || tickProvider.Positions == null)
                return;

            bool first = true;
            foreach (double pos in tickProvider.Positions)
            {
                double x = transform.DataToScreen(new Point(pos, 0.0)).X;

                if (x >= 0 && x <= this.ActualWidth)
                {
                    Point start = new Point(x, 0.0);
                    Point end = new Point(x, majorTickSize);
                    drawingContext.DrawLine(this.TickPen, start, end);


                    FormattedText text = new FormattedText(tickProvider.GetFormattedString(pos), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        labelTypeface, fontSize, fontBrush);
                    Point textPosition = new Point(end.X - text.Width / 2.0, end.Y + fontOffset);
                    drawingContext.DrawText(text, textPosition);

                    

                }

                DrawMinorTicks(drawingContext, pos);

                if (first)
                {
                    first = false;
                    DrawMinorTicksBeforeFirst(drawingContext, pos);
                }
            }
        }

        private void DrawMinorTicksBeforeFirst(DrawingContext drawingContext, double pos)
        {
            if (drawMinorTicks)
            {
                double minorTicksOffset = tickProvider.Distance / (minorTickCount + 1);

                for (int i = 0; i < minorTickCount; i++)
                {
                    double tickPos = pos - minorTicksOffset * (i + 1);
                    double tickX = transform.DataToScreen(new Point(tickPos, 0.0)).X;

                    if (tickX >= 0 && tickX <= this.ActualWidth)
                    {
                        Point tickStart = new Point(tickX, 0.0);

                        double tickSize = minorTickSize;
                        if (i == middleTickIndex)
                            tickSize = middleTickSize;

                        Point tickEnd = new Point(tickX, tickSize);
                        drawingContext.DrawLine(this.TickPen, tickStart, tickEnd);
                    }
                }
            }
        }

        private void DrawMinorTicks(DrawingContext drawingContext, double pos)
        {
            if (drawMinorTicks)
            {
                double minorTicksOffset = tickProvider.Distance / (minorTickCount + 1);

                for (int i = 0; i < minorTickCount; i++)
                {
                    double tickPos = pos + minorTicksOffset * (i + 1);
                    double tickX = transform.DataToScreen(new Point(tickPos, 0.0)).X;

                    if (tickX >= 0 && tickX <= this.ActualWidth - this.Margin.Right)
                    {
                        Point tickStart = new Point(tickX, 0.0);

                        double tickSize = minorTickSize;
                        if (i == middleTickIndex)
                            tickSize = middleTickSize;

                        Point tickEnd = new Point(tickX, tickSize);
                        drawingContext.DrawLine(this.TickPen, tickStart, tickEnd);
                    }
                }
            }
        }
    }
}
