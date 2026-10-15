using System;

namespace ChromExplorer.Plotter.Axes
{
    public class TickHelper
    {
        public static double NiceNumber(double value, bool round)
        {
            try
            {
                int exponent = (int)Math.Floor(Math.Log10(value));
                double fraction = value / Math.Pow(10.0, exponent);
                double rounded_fraction = 0.0;

                if (round)
                {
                    if (fraction < 1.5)
                        rounded_fraction = 1.0;
                    else if (fraction < 3.0)
                        rounded_fraction = 2.0;
                    else if (fraction < 7.0)
                        rounded_fraction = 5.0;
                    else
                        rounded_fraction = 10.0;

                }
                else
                {
                    if (fraction <= 1.0)
                        rounded_fraction = 1.0;
                    else if (fraction <= 2.0)
                        rounded_fraction = 2.0;
                    else if (fraction <= 5.0)
                        rounded_fraction = 5.0;
                    else
                        rounded_fraction = 10.0;
                }

                return rounded_fraction * Math.Pow(10.0, exponent);
            }
            catch (Exception ex)
            {
                //TODO Processing here
            }

            return value;
        }
    }
}
