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
 
        public double[] GenerateTicksPositions(double min, double max, int ticks)
        {
            List<double> result = new List<double>();

            // ticks<=1 makes (ticks-1) zero or negative, sending the step size to a degenerate
            // value and producing an enormous number of 1-unit-spaced positions instead of a
            // handful of nice ones (can happen if the control's ActualHeight/Width is still 0
            // during an early layout pass, since callers derive ticks from that).
            if (ticks < 2)
                ticks = 2;

            double range = TickHelper.NiceNumber(max - min, false);
            double d = TickHelper.NiceNumber(range / (double)(ticks - 1), true);

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
