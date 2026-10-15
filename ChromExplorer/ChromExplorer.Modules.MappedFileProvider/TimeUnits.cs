using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChromExplorer.Modules.MappedFileProvider
{
    public enum TimeUnits
    {
        Ms,
        Sec,
        Min,
    }

    public class TimeMultipliers
    {
        private static Dictionary<TimeUnits, double> _multiplers;

        static TimeMultipliers()
        {
            _multiplers = new Dictionary<TimeUnits, double>();

            _multiplers.Add(TimeUnits.Ms, 1);
            _multiplers.Add(TimeUnits.Sec, 1.0 / 1000.0);
            _multiplers.Add(TimeUnits.Min, 1.0 / 1000.0 / 60.0);
        }

        public static double GetMultyplier(TimeUnits units)
        {
            return _multiplers[units];
        }
    }
}
