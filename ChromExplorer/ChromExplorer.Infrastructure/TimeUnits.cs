using System.Collections.Generic;

namespace ChromExplorer.Infrastructure
{
    public enum TimeUnits
    {
        Ms,
        Sec,
        Min,
        Hour,
    }

    public class TimeMultipliers
    {
        private static readonly Dictionary<TimeUnits, double> Multiplers;

        static TimeMultipliers()
        {
            Multiplers = new Dictionary<TimeUnits, double>
                             {
                                 {TimeUnits.Ms, 1}, 
                                 {TimeUnits.Sec, 1.0/1000.0}, 
                                 {TimeUnits.Min, 1.0/1000.0/60.0},
                                 {TimeUnits.Hour, 1.0/1000.0/60.0/60.0}
                             };
        }

        public static double GetMultyplier(TimeUnits units)
        {
            return Multiplers[units];
        }
    }
}
