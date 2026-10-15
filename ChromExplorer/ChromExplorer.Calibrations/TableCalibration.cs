using System.Collections.Generic;

namespace ChromExplorer.Calibrations
{
    public class TableCalibration : ICalibration
    {
        private readonly List<DataPoint> _points;
        private readonly bool _incomplete = true;

        public TableCalibration(IEnumerable<DataPoint> points)
        {
            _points = new List<DataPoint>(points);

            if (_points.Count >= 2)
                _incomplete = false;

            _points.Sort((p1, p2) => Comparer<double>.Default.Compare(p1.X, p2.X));

        }


        #region Implementation of ICalibration

        public double GetCalibratedValue(double uncalibrated)
        {
            if (_incomplete)
            {
                //log.Warn("Table calibration incomplete");
                return uncalibrated;
            }

            if (_points.Count == 2)
            {
                var line = new LinearCalibration(_points[0].X, _points[0].Y, _points[1].X, _points[1].Y);
                return line.GetCalibratedValue(uncalibrated);
            }

            // Linear extrapolation
            if (uncalibrated <= _points[1].X)
            {
                var line = new LinearCalibration(_points[0].X, _points[0].Y, _points[1].X, _points[1].Y);
                return line.GetCalibratedValue(uncalibrated);
            }

            if (uncalibrated >= _points[_points.Count - 2].X)
            {
                var line = new LinearCalibration(_points[_points.Count - 2].X, _points[_points.Count - 2].Y
                    , _points[_points.Count - 1].X, _points[_points.Count - 1].Y);

                return line.GetCalibratedValue(uncalibrated);
            }

            for (int i = 1; i < _points.Count - 1; i++)
            {
                if (uncalibrated >= _points[i].X && uncalibrated <= _points[i + 1].X)
                {
                    var line = new LinearCalibration(_points[i].X, _points[i].Y, _points[i + 1].X, _points[i + 1].Y);
                    return line.GetCalibratedValue(uncalibrated);
                }
            }


            return 0;
        }

        public double GetMeasuredValue(double calibrated)
        {
            if (_incomplete)
            {
                //log.Warn("Table calibration incomplete");
                return calibrated;
            }

            for (int i = 0; i < _points.Count - 1; i++)
            {
                double min = _points[i].Y;
                double max = _points[i + 1].Y;

                if (min > max)
                {
                    min = _points[i + 1].Y;
                    max = _points[i].Y;
                }

                if (calibrated >= min && calibrated <= max)
                {
                    var line = new LinearCalibration(_points[i].X, _points[i].Y, _points[i + 1].X, _points[i + 1].Y);
                    return line.GetMeasuredValue(calibrated);
                }
            }

            var leftLine = new LinearCalibration(_points[0].X, _points[0].Y, _points[1].X, _points[1].Y);
            double measured = leftLine.GetMeasuredValue(calibrated);

            if (measured <= _points[0].X)
                return measured;

            var rightLine = new LinearCalibration(_points[_points.Count - 2].X, _points[_points.Count - 2].Y
                    , _points[_points.Count - 1].X, _points[_points.Count - 1].Y);
            measured = rightLine.GetMeasuredValue(calibrated);

            if (measured >= _points[_points.Count - 1].X)
                return measured;

            //log.WarnFormat("Segment not found for {0}", calibrated);
            return calibrated;
        }


        public CalibrationTypes Type
        {
            get { return CalibrationTypes.Table; }
        }

        #endregion
    }
}
