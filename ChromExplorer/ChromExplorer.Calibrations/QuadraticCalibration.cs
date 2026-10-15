using System;

namespace ChromExplorer.Calibrations
{
    public class QuadraticCalibration : ICalibration
    {
        public QuadraticCalibration(double a, double b, double c)
        {
            this.A = a;
            this.B = b;
            this.C = c;

            this.Min = double.NegativeInfinity;
            this.Max = double.PositiveInfinity;
            this.DefaultValue = default(double);
        }

        public double A { get; private set; }
        public double B { get; private set; }
        public double C { get; private set; }

        public double Min { get; set; }
        public double Max { get; set; }

        public double DefaultValue { get; set; }


        #region Implementation of ICalibration

        public double GetCalibratedValue(double uncalibrated)
        {
            return A * uncalibrated * uncalibrated + B * uncalibrated + C;
        }

        public double GetMeasuredValue(double calibrated)
        {
            var d = this.B * this.B - 4.0 * this.A * (this.C - calibrated);

            if (d >= 0)
            {
                var sqrtD = Math.Sqrt(d);
                var x1 = -1.0 * this.B + sqrtD;
                var x2 = -1.0 * this.B - sqrtD;

                if (x1 >= this.Min && x1 <= this.Max)
                    return x1;

                if (x2 >= this.Min && x2 <= this.Max)
                    return x2;
            }

            return DefaultValue;
        }

        public CalibrationTypes Type
        {
            get { return CalibrationTypes.Quadratic; }
        }

        #endregion
    }
}
