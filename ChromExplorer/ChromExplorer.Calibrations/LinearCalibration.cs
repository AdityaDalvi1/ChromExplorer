namespace ChromExplorer.Calibrations
{
    public class LinearCalibration : ICalibration
    {
        public LinearCalibration(double a, double b)
        {
            this.A = a;
            this.B = b;
        }

        public LinearCalibration(double x1, double y1, double x2, double y2)
        {
            this.A = (y2 - y1) / (x2 - x1);
            this.B = y1 - x1 * this.A;
        }

        public double A { get; private set; }
        public double B { get; private set; }

        #region Implementation of ICalibration

        public double GetCalibratedValue(double uncalibrated)
        {
            return this.A * uncalibrated + B;
        }

        public double GetMeasuredValue(double calibrated)
        {
            return (calibrated - this.B) / A;
        }

        public CalibrationTypes Type
        {
            get { return CalibrationTypes.Linear; }
        }

        #endregion
    }
}
