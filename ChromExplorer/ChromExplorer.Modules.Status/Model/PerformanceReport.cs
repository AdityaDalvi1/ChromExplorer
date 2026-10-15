using System.ComponentModel;
using ChromExplorer.Modules.Status.Properties;

namespace ChromExplorer.Modules.Status.Model
{
    public class MemUsageUnit
    {
        public MemUsageUnit(string name, double multiplier)
        {
            Name = name;
            Multiplier = multiplier;
        }

        public string Name { get; protected set; }
        public double Multiplier { get; protected set; }
    }

    public class MemUsageMB : MemUsageUnit
    {
        public MemUsageMB() : base("MB", 1024.0*1024.0)
        {
        }
    }


    public class PerformanceReport : INotifyPropertyChanged
    {
        private string _memUnits;
        private double _memUsage;
        private double _memUsageChange;

        public string MemUnits
        {
            get { return _memUnits; }
            set
            {
                if (value == _memUnits) return;
                _memUnits = value;
                OnPropertyChanged("MemUnits");
            }
        }

        public double MemUsage
        {
            get { return _memUsage; }
            set
            {
                if (value.Equals(_memUsage)) return;
                _memUsage = value;
                OnPropertyChanged("MemUsage");
            }
        }

        public double MemUsageChange
        {
            get { return _memUsageChange; }
            set
            {
                if (value.Equals(_memUsageChange)) return;
                _memUsageChange = value;
                OnPropertyChanged("MemUsageChange");
            }
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