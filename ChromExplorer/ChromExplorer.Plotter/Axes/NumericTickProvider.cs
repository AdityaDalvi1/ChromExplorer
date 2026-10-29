using System;
using System.Collections.Generic;
using System.Text;

namespace ChromExplorer.Plotter.Axes
{
    public class NumericTickProvider
    {
        private string _format = "0";
        //private string _format = "0.###E+0";

        public double Min { get; private set; }
        public double Max { get; private set; }
        public double Distance { get; private set; }

        public double[] Positions { get; private set; }

        public NumericTickProvider()
        {
            
        }

        public void Update(double min, double max, int ticks)
        {
            this.Positions = GenerateTicksPositions(min, max, ticks);

            if (this.Positions.Length >= 2)
            {
                this.Min = this.Positions[0];
                this.Max = this.Positions[this.Positions.Length - 1];
            }
            else
            {
                this.Min = 0.0;
                this.Max = 1.0;
                this.Distance = 1.0;
            }

        }

        public string GetFormattedString(double value)
        {
            return String.Format(_format, value);
        }
 
        // Zoomed-view variant of Update. GenerateTicksPositions rounds the span UP to a "nice"
        // number before dividing it into steps, so on a short plot (few ticks) the step can end up
        // about twice the real visible span and the only major tick inside the view is the one at
        // the bottom edge, leaving the axis without intensity labels. Here the step is chosen from
        // the actual span and the plot's pixel height: the smallest "nice" step (1, 2, 5 x 10^n)
        // whose major ticks are at least minSpacingPx apart on screen, so the rotated labels never
        // crowd each other. If that leaves fewer than two ticks inside [min, max] the spacing is
        // relaxed a little (never below minSpacingFloorPx) so a second label can appear. Only the
        // choice of step differs: positions still run from floor(min/d)*d to ceil(max/d)*d exactly
        // as before, so minor ticks, number formatting and the data-to-screen transform are
        // unaffected.
        public void UpdateWithinRange(double min, double max, int ticks, double heightPx,
            double minSpacingPx, double minSpacingFloorPx)
        {
            double span = max - min;

            if (double.IsNaN(span) || double.IsInfinity(span) || span <= 0.0 ||
                double.IsNaN(heightPx) || heightPx <= 0.0)
            {
                Update(min, max, ticks);
                return;
            }

            double d = 0.0;

            for (double spacing = minSpacingPx; ; spacing = Math.Max(spacing - 4.0, minSpacingFloorPx))
            {
                d = NiceStepAtLeast(span * spacing / heightPx);

                if (CountInside(min, max, d) >= 2 || spacing <= minSpacingFloorPx)
                    break;
            }

            this.Positions = BuildPositions(min, max, d);

            if (this.Positions.Length >= 2)
            {
                this.Min = this.Positions[0];
                this.Max = this.Positions[this.Positions.Length - 1];
            }
            else
            {
                this.Min = 0.0;
                this.Max = 1.0;
                this.Distance = 1.0;
            }
        }

        // Smallest value of the form 1, 2 or 5 x 10^n that is >= value.
        private static double NiceStepAtLeast(double value)
        {
            double exponent = Math.Floor(Math.Log10(value));
            double magnitude = Math.Pow(10.0, exponent);
            double fraction = value / magnitude;

            double nice;
            if (fraction <= 1.0 + 1e-9)
                nice = 1.0;
            else if (fraction <= 2.0 + 1e-9)
                nice = 2.0;
            else if (fraction <= 5.0 + 1e-9)
                nice = 5.0;
            else
                nice = 10.0;

            return nice * magnitude;
        }

        private static int CountInside(double min, double max, double d)
        {
            if (d <= 0.0 || double.IsNaN(d) || double.IsInfinity(d))
                return 0;

            double count = Math.Floor(max / d) - Math.Ceiling(min / d) + 1;

            return count < 0 ? 0 : (int)Math.Min(count, 1000);
        }

        public double[] GenerateTicksPositions(double min, double max, int ticks)
        {
            // ticks<=1 makes (ticks-1) zero or negative, sending the step size to a degenerate
            // value and producing an enormous number of 1-unit-spaced positions instead of a
            // handful of nice ones (can happen if the control's ActualHeight/Width is still 0
            // during an early layout pass, since callers derive ticks from that).
            if (ticks < 2)
                ticks = 2;

            double range = TickHelper.NiceNumber(max - min, false);
            double d = TickHelper.NiceNumber(range / (double)(ticks - 1), true);

            return BuildPositions(min, max, d);
        }

        private double[] BuildPositions(double min, double max, double d)
        {
            List<double> result = new List<double>();

            double graph_min = Math.Floor(min / d) * d;
            double graph_max = Math.Ceiling(max / d) * d;

            int n_frac = (int)Math.Max(- Math.Floor(Math.Log10(d)), 0.0);
            BuildFormatString(n_frac);

            for (double v = graph_min; v <= graph_max + 0.5 * d; v += d)
            {
                result.Add(v);
                if(v > 99999)
                {
                    _format = "{0:#.##E+0}";
                }
            }

            this.Distance = d;
            return result.ToArray();
        }


        private void BuildFormatString(int n_frac)
        {
            if (n_frac < 0)
                return;

            StringBuilder sb = new StringBuilder();

            sb.Append("{0:0.");

            for (int i = 0; i < n_frac; i++)
            {
                sb.Append("0");
            }

            sb.Append("}");

            _format = sb.ToString();
        }
    }
}
