using System;
using System.Collections.Generic;
using System.Windows.Documents;
using ChromExplorer.DataModel.Chromatogram;

namespace ChromExplorer.OxyPlot.Demo
{
    public interface IChromFilter
    {
        bool Accept(ChromatogramPoint p);
    }

    public class MsPowerFilter : IChromFilter
    {
        private readonly int _msPower = -1;
        private readonly double _startTime;
        private readonly double _stopTime;
        
        public MsPowerFilter(int power = 0, double startTime = 0.0, double stopTime = Double.MaxValue)
        {
            _msPower = power;
            _startTime = startTime;
            _stopTime = stopTime;
        }
        public bool Accept(ChromatogramPoint p)
        {
            if (p.SpectrumMeta == null)
                return false;

            if (_msPower == -1)
                return true;

            return p.SpectrumMeta.MsPower == _msPower && IsTimeInRange(p);
        }

        private bool IsTimeInRange(ChromatogramPoint p)
        {
            return p.Time >= _startTime && p.Time < _stopTime;
        }
    }

    public class TimeFilter : IChromFilter
    {
        private readonly double _startTime;
        private readonly double _stopTime;

        public TimeFilter(double startTime = 0.0, double stopTime = Double.MaxValue)
        {
            _startTime = startTime;
            _stopTime = stopTime;
        }

        public bool Accept(ChromatogramPoint p)
        {
            return p.Time >= _startTime && p.Time < _stopTime;
        }
    }

    public class ParentIonFilter : IChromFilter
    {
        private const double tolerance = 0.0001;
        private readonly double _mzFrom;
        private readonly double _mzTo;

        public ParentIonFilter(double parentIon)
        {
            _mzFrom = parentIon - tolerance;
            _mzTo = parentIon + tolerance;
        }
        public ParentIonFilter(double mzFrom, double mzTo)
        {
            _mzFrom = mzFrom;
            _mzTo = mzTo;
        }

        public bool Accept(ChromatogramPoint p)
        {
            if (p.SpectrumMeta == null)
                return false;
            return p.SpectrumMeta.ParentIon >= _mzFrom && p.SpectrumMeta.ParentIon < _mzTo;
        }
    }

    public class FilterCollection : List<IChromFilter>, IChromFilter
    {
        public FilterCollection() : base()
        {

        }

        public bool Accept(ChromatogramPoint p)
        {
            foreach (var filter in this)
            {
                if (!filter.Accept(p))
                    return false;
            }

            return true;
        }
    }





}