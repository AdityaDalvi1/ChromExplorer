using System;

namespace ChromExplorer.Calibrations
{
    public class CubicCalibration : ICalibration
    {
        private double _a, _b, _c;
        private double _q;
        private double _rPart;

        public CubicCalibration(double a, double b, double c, double d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;

            this.Min = double.NegativeInfinity;
            this.Max = double.PositiveInfinity;
            this.DefaultValue = default(double);
            BuildCoeffs();
        }

        private void BuildCoeffs()
        {
            _a = this.B / this.A;
            _b = this.C / this.A;

            _q = (_a * _a - 3.0 * _b) / 9.0;
            _rPart = (2.0 * _a * _a * _a - 9.0 * _a * _b) / 54.0;
        }

        public double A { get; private set; }
        public double B { get; private set; }
        public double C { get; private set; }
        public double D { get; private set; }

        public double Min { get; set; }
        public double Max { get; set; }

        public double DefaultValue { get; set; }

        #region Implementation of ICalibration

        public double GetCalibratedValue(double uncalibrated)
        {
            var p2 = uncalibrated * uncalibrated;
            var p3 = p2 * uncalibrated;

            return this.A * p3 + this.B * p2 + this.C * uncalibrated + this.D;
        }

        public double GetMeasuredValue(double calibrated)
        {
            if (A == 0.0 && B != 0.0)
            {
                return GetQuadratic(calibrated);
            }

            if (A == 0.0 && B == 0.0)
            {
                return GetLinear(calibrated);
            }

            double R = _rPart + (this.D - calibrated) / this.A * 27.0 / 54.0;

            double Q3 = _q * _q * _q;
            double S = _q * _q * _q - R * R;

            if (S > 0.0)
            {
                double phi = 1.0 / 3.0 * Math.Acos(R / Math.Sqrt(Q3));

                double x1 = -2.0 * Math.Sqrt(_q) * Math.Cos(phi) - _a / 3.0;
                double x2 = -2.0 * Math.Sqrt(_q) * Math.Cos(phi + 2.0 / 3.0 * Math.PI) - _a / 3.0;
                double x3 = -2.0 * Math.Sqrt(_q) * Math.Cos(phi - 2.0 / 3.0 * Math.PI) - _a / 3.0;

                return GetRootInRange(x1, x2, x3);
            }

            if (S < 0.0)
            {
                if (_q > 0.0)
                {
                    double phi = 1.0 / 3.0 * Arch(Math.Abs(R) / Math.Sqrt(Q3));
                    double x1 = -2.0 * Math.Sign(R) * Math.Sqrt(_q) * Math.Cosh(phi) - _a / 3.0;

                    return GetRootInRange(x1, x1, x1);
                }

                if (_q < 0.0)
                {
                    double phi = 1.0 / 3.0 * Arsh(Math.Abs(R) / Math.Sqrt(Math.Abs(Q3)));
                    double x1 = -2.0 * Math.Sign(R) * Math.Sqrt(Math.Abs(_q)) * Math.Sinh(phi) - _a / 3.0;

                    return GetRootInRange(x1, x1, x1);
                }
            }

            if (S == 0.0)
            {
                double x1 = -2.0 * Math.Sign(R) * Math.Sqrt(_q) - _a / 3.0;
                double x2 = Math.Sign(R) * Math.Sqrt(_q) - _a / 3.0;

                return GetRootInRange(x1, x2, x1);
            }


            return this.DefaultValue;
        }

        public CalibrationTypes Type
        {
            get { return CalibrationTypes.Cubic; }
        }

        #endregion

        private double GetQuadratic(double calibrated)
        {
            double D = this.C * this.C - 4.0 * this.B * (this.D - calibrated);

            if (D >= 0)
            {
                double x1 = (-1.0 * this.C + Math.Sqrt(D)) / 2.0 / this.B;
                double x2 = (-1.0 * this.C - Math.Sqrt(D)) / 2.0 / this.B;

                return GetRootInRange(x1, x2, x1);
            }

            return this.DefaultValue;
        }

        private double GetLinear(double calibrated)
        {
            return (calibrated - this.D) / C;
        }

        private double GetRootInRange(double x1, double x2, double x3)
        {
            if (x1 >= this.Min && x1 <= this.Max)
            {
                return x1;
            }

            if (x2 >= this.Min && x2 <= this.Max)
            {
                return x2;
            }

            if (x3 >= this.Min && x3 <= this.Max)
            {
                return x3;
            }

            return this.DefaultValue;
        }

        private static double Arch(double x)
        {
            return Math.Log(x + Math.Sqrt(x * x - 1.0));
        }

        private static double Arsh(double x)
        {
            return Math.Log(x + Math.Sqrt(x * x + 1.0));

        }
    }
}
