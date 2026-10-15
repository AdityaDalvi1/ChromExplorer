namespace ChromExplorer.Calibrations
{
    public enum CalibrationTypes
    {
        Linear,
        Quadratic,
        Cubic,
        Table
    }

    public interface ICalibration
    {
        double GetCalibratedValue(double uncalibrated);
        double GetMeasuredValue(double calibrated);

        CalibrationTypes Type { get; }
    }
}
