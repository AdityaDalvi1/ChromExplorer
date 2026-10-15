using System;
using System.Collections.Generic;
using System.IO;
using ChromExplorer.Calibrations;

namespace ChromExplorer.ModasReader
{
    public class CalibrationCache
    {
        private readonly Dictionary<int, ICalibration> _calibrations;

        public CalibrationCache()
        {
            _calibrations = new Dictionary<int, ICalibration>();
        }

        public void Clear()
        {
            _calibrations.Clear();
        }

        public void Add(Calibrations calibration)
        {
            if(calibration == null)
                throw new ArgumentNullException("calibration");

            if (!_calibrations.ContainsKey(calibration.Id))
            {
                _calibrations.Add(calibration.Id, GetCalibration(calibration));
            }
        }

        public ICalibration GetCalibration(int calibrationID)
        {
            return _calibrations.ContainsKey(calibrationID) ? _calibrations[calibrationID] : null;
        }

        public ICalibration GetCalibration(Calibrations calibration)
        {
            if (calibration == null)
            {
                throw new ArgumentNullException("calibration");
            }
           
            double a = 1.0;
            double b = 0.0;
            double c = 0.0;
            double d = 0.0;

            if (calibration.P0 != null)
                a = calibration.P0.Value;

            if (calibration.P1 != null)
                b = calibration.P1.Value;

            if (calibration.P2 != null)
                c = calibration.P2.Value;

            if (calibration.P3 != null)
                d = calibration.P3.Value;

            var points = new List<DataPoint>();
            if(calibration.RawData != null)
            {
                points = ReadBinaryTable(calibration.RawData);
            }

            switch ((CalibrationTypes)calibration.Type)
            {
                case CalibrationTypes.Linear:
                    return new LinearCalibration(a,b);

                case CalibrationTypes.Quadratic:
                    return new QuadraticCalibration(a, b, c);

                case CalibrationTypes.Cubic:
                    return new CubicCalibration(a, b, c, d);

                case CalibrationTypes.Table:
                    return new TableCalibration(points);
            }

            return null;
        }

        private void Add(int id, ICalibration calibration)
        {
            if (!_calibrations.ContainsKey(id))
            {
                _calibrations.Add(id, calibration);
            }
        }

        private static List<DataPoint> ReadBinaryTable(byte[] data)
        {
            if (data == null)
                return null;


            using (var stream = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var retval = new List<DataPoint>();
                    int size = reader.ReadInt32();

                    for (int i = 0; i < size; i++)
                    {
                        var x = reader.ReadDouble();
                        var y = reader.ReadDouble();

                        retval.Add(new DataPoint() { X = x, Y = y });
                    }

                    return retval;
                }
            }

        }
    }
}
