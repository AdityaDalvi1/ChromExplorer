using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChromExplorer.Plotter.Axes;

namespace ChromExplorer.Plotter
{
    
    public class VerticalAxisControl : Control
    {
        static VerticalAxisControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VerticalAxisControl), new FrameworkPropertyMetadata(typeof(VerticalAxisControl)));
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


        public VerticalAxisControl()
        {
            this.Width = 32;
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.VerticalContentAlignment = VerticalAlignment.Stretch;

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

            foreach (double pos in tickProvider.Positions)
            {
                double y = transform.DataToScreen(new Point(0.0, pos)).Y;

                if (y >= 0 && y <= this.ActualHeight)
                {
                    Point start = new Point(this.ActualWidth - majorTickSize, y);
                    Point end = new Point(this.ActualWidth, y);
                    drawingContext.DrawLine(this.TickPen, start, end);

                    FormattedText text = new FormattedText(tickProvider.GetFormattedString(pos), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        labelTypeface, fontSize, fontBrush);

                    Point textCenter = new Point(this.ActualWidth - majorTickSize - fontOffset - text.Height / 2.0, y);
                    Point textLeft = new Point(textCenter.X - text.Width / 2.0, textCenter.Y - text.Height / 2.0);

                    RotateTransform rt = new RotateTransform(-90, textCenter.X, textCenter.Y);
                    drawingContext.PushTransform(rt);
                    drawingContext.DrawText(text, textLeft);
                    drawingContext.Pop();
                }

                DrawMinorTicks(drawingContext, pos);
            }
        }

        private void DrawMinorTicks(DrawingContext drawingContext, double pos)
        {
            if (!drawMinorTicks)
                return;

            double minorTicksOffset = tickProvider.Distance / (minorTickCount + 1);


            for (int i = 0; i < minorTickCount; i++)
            {
                double tickPos = pos + minorTicksOffset * (i + 1);
                double tickY = transform.DataToScreen(new Point(0.0, tickPos)).Y;


                if (tickY >= 0 && tickY <= this.ActualHeight)
                {
                    double tickSize = minorTickSize;
                    if (i == middleTickIndex)
                        tickSize = middleTickSize;

                    Point tickStart = new Point(this.ActualWidth - tickSize, tickY);
                    Point tickEnd = new Point(this.ActualWidth, tickY);

                    drawingContext.DrawLine(this.TickPen, tickStart, tickEnd);
                }
            }
        }
    }
}
