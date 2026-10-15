using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using ChromExplorer.DataModel.Chromatogram;
using ChromExplorer.DataModel.Core;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.DataModel.Properties;

namespace ChromExplorer.DataModel.Spectrum
{
    public class MsSpectrum : ISpectrum, INotifyPropertyChanged
    {
        
        private readonly ChromatogramPoint _chromPoint;
        private readonly AGCSpectrumEstimator _estimator = new AGCSpectrumEstimator();
        private double _retentionTime;
        private string _retentionTimeUnits;
        private DateTime _timestamp;
        private string _timestampStr;
        private double _ppd;
        private SimpleDataSource _points;
        private double _signalToNoise;
        private double _parentIon;
        private int _id;
        private int _index;
        private double _tic;
        private double _max;
        private double _min;
        private bool _baselineRemoved;
        private SpectrumType _type;
        private double _signal;
        private double _injectionTime;

        public MsSpectrum(SimpleDataSource points)
        {
            Points = points;
            //UpdateStatistics();
        }

        public MsSpectrum(SimpleDataSource points, int id)
            : this(points)
        {
            ID = id;
        }

        public MsSpectrum(SimpleDataSource points, ChromatogramPoint chromPoint)
        {
            _chromPoint = chromPoint;
            Points = points;
            //UpdateStatistics();
        }

        public double RetentionTime
        {
            get { return _retentionTime; }
            set
            {
                if (value.Equals(_retentionTime)) return;
                _retentionTime = value;
                OnPropertyChanged("RetentionTime");
            }
        }

        public string RetentionTimeUnits
        {
            get { return _retentionTimeUnits; }
            set
            {
                if (value == _retentionTimeUnits) return;
                _retentionTimeUnits = value;
                OnPropertyChanged("RetentionTimeUnits");
            }
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
            set
            {
                if (value.Equals(_timestamp)) return;
                _timestamp = value;
                OnPropertyChanged("Timestamp");
            }
        }

        public string TimestampStr
        {
            get { return _timestampStr; }
            set
            {
                if (value == _timestampStr) return;
                _timestampStr = value;
                OnPropertyChanged("TimestampStr");
            }
        }

        public double PPD
        {
            get { return _ppd; }
            set
            {
                if (value.Equals(_ppd)) return;
                _ppd = value;
                OnPropertyChanged("PPD");
            }
        }

        public SimpleDataSource Points
        {
            get { return _points; }
            set
            {
                if (Equals(value, _points)) return;
                _points = value;
                OnPropertyChanged("Points");
            }
        }

        public double SignalToNoise
        {
            get { return _signalToNoise; }
            set
            {
                if (value.Equals(_signalToNoise)) return;
                _signalToNoise = value;
                OnPropertyChanged("SignalToNoise");
            }
        }

        public double ParentIon
        {
            get { return _parentIon; }
            set
            {
                if (value.Equals(_parentIon)) return;
                _parentIon = value;
                OnPropertyChanged("ParentIon");
            }
        }

        public int ID
        {
            get { return _id; }
            set
            {
                if (value == _id) return;
                _id = value;
                OnPropertyChanged("ID");
            }
        }

        public int Index
        {
            get { return _index; }
            set
            {
                if (value == _index) return;
                _index = value;
                OnPropertyChanged("Index");
            }
        }

        public double TIC
        {
            get { return _tic; }
            set
            {
                if (value.Equals(_tic)) return;
                _tic = value;
                OnPropertyChanged("TIC");
            }
        }

        public double Max
        {
            get { return _max; }
            set
            {
                if (value.Equals(_max)) return;
                _max = value;
                OnPropertyChanged("Max");
            }
        }

        public double Min
        {
            get { return _min; }
            set
            {
                if (value.Equals(_min)) return;
                _min = value;
                OnPropertyChanged("Min");
            }
        }

        public bool BaselineRemoved
        {
            get { return _baselineRemoved; }
            set
            {
                if (value.Equals(_baselineRemoved)) return;
                _baselineRemoved = value;
                OnPropertyChanged("BaselineRemoved");
            }
        }

        public SpectrumType Type
        {
            get { return _type; }
            set
            {
                if (value == _type) return;
                _type = value;
                OnPropertyChanged("Type");
            }
        }

        public double Signal
        {
            get { return _signal; }
            set
            {
                if (value.Equals(_signal)) return;
                _signal = value;
                OnPropertyChanged("Signal");
            }
        }

        public double InjectionTime
        {
            get { return _injectionTime; }
            set
            {
                if (value.Equals(_injectionTime)) return;
                _injectionTime = value;
                OnPropertyChanged(nameof(InjectionTime));
            }
        }

        public void UpdateStatistics()
        {
            if (Points == null)
                return;

            if (_chromPoint == null)
            {
                Min = _points.Yvals.Min();
                Max = _points.Yvals.Max();
                return;
            }

            ID = _chromPoint.SpectrumID;
            Index = _chromPoint.Index;
            RetentionTime = _chromPoint.Time;
            RetentionTimeUnits = _chromPoint.TimeUnits;
            ParentIon = _chromPoint.SpectrumMeta.ParentIon;
            InjectionTime = _chromPoint.SpectrumMeta.InjectionTime;

            if (_chromPoint.SpectrumMeta.MsPower == 1)
            {
                Type = SpectrumType.MS;
            }

            if (_chromPoint.SpectrumMeta.MsPower == 2)
            {
                Type = SpectrumType.MSMS;
            }

            var max = double.NegativeInfinity;
            var min = double.PositiveInfinity;
            // we can not use IntegrateUsefulSignalVersion2 here, it can be already applied
            var intensities = new int[Points.Length];
            double tic = 0.0;


            for (var i = 0; i < Points.Length; i++)
            {
                if (Points.Yvals[i] > max)
                    max = Points.Yvals[i];

                if (Points.Yvals[i] < min)
                    min = Points.Yvals[i];
                tic += Points.Yvals[i];
                intensities[i] = (int) Points.Yvals[i];
            }
            Max = max;
            Min = min;

            var divider = _chromPoint.SpectrumMeta.InjectionTime > 0 ? _chromPoint.SpectrumMeta.InjectionTime : 1.0;

            
            if(!BaselineRemoved)
                tic = _estimator.IntegrateUsefulSignalVersion2(intensities, 1);
            
            TIC = tic/divider;

            Timestamp = _chromPoint.Timestamp;
            TimestampStr = Timestamp.ToString(CultureInfo.InvariantCulture);
        }

        public void RaisePropertyChanged()
        {
            OnPropertyChanged("Index");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}