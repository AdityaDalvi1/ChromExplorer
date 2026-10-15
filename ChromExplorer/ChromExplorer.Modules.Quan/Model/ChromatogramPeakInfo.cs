using System;
using System.ComponentModel;
using System.Xml.Serialization;
using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.Quan.Properties;

namespace ChromExplorer.Modules.Quan.Model
{
    public class ChromatogramPeakInfo : INotifyPropertyChanged
    {
        private double? _calculatedAmount;
        private double? _difference;
        private double? _expectedAmount;
        private double? _recoveryPercent;
        private string _sampleType;
        private int _id;

        public int ID
        {
            get { return _id; }
            set
            {
                if (_id == value)
                    return;
                _id = value;
                OnPropertyChanged(nameof(ID));
            }
        }
        public string FilePath { get; set; }
        public string SampleID { get; set; }

        public string SampleType
        {
            get { return _sampleType; }
            set
            {
                if (_sampleType == value)
                    return;
                _sampleType = value;
                OnPropertyChanged(nameof(SampleType));
                UpdateDerivedFields();
            }
        }

        public double? ExpectedAmount
        {
            get { return _expectedAmount; }
            set
            {
                if (_expectedAmount == value)
                    return;
                _expectedAmount = value;
                OnPropertyChanged(nameof(ExpectedAmount));
                UpdateDerivedFields();
            }
        }

        [XmlIgnore]
        public double? RecoveryPercent
        {
            get { return _recoveryPercent; }
            internal set
            {
                if (_recoveryPercent == value)
                    return;
                _recoveryPercent = value;
                OnPropertyChanged(nameof(RecoveryPercent));
            }
        }

        public double Area        { get; set; }
        public double TimeFrom { get; set; }
        public double TimeTo { get; set; }
        public double MassRangeFrom { get; set; }
        public double MassRangeTo { get; set; }
        public string TimeUnits { get; set; }
     
        public double? Difference
        {
            get { return _difference; }
            set
            {
                if (_difference == value)
                    return;
                _difference = value;
                OnPropertyChanged(nameof(Difference));
            }
        }

        public double? CalculatedAmount
        {
            get { return _calculatedAmount; }
            set
            {
                if(_calculatedAmount == value)
                    return;
                _calculatedAmount = value;

                OnPropertyChanged(nameof(CalculatedAmount));

                UpdateDerivedFields();
            }
        }

        private void UpdateDerivedFields()
        {
            if (_expectedAmount != null && _expectedAmount.Value != 0 && _calculatedAmount != null)
            {
                Difference = (_calculatedAmount - _expectedAmount.Value) / _expectedAmount.Value * 100.0;
            }

            if (IsStandardOrCalibrant())
            {
                if (_expectedAmount != null && _expectedAmount.Value != 0 && _calculatedAmount != null)
                    RecoveryPercent = (_calculatedAmount.Value / _expectedAmount.Value) * 100.0;
                else
                    RecoveryPercent = null;
            }
            else if (!IsSpikeSample())
            {
                RecoveryPercent = null;
            }
        }

        private bool IsStandardOrCalibrant()
        {
            var t = (_sampleType ?? string.Empty).Trim();
            return string.Equals(t, "Standard", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(t, "Calibrant", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSpikeSample()
        {
            var t = (_sampleType ?? string.Empty).Trim();
            return string.Equals(t, "Spike Sample", StringComparison.OrdinalIgnoreCase);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}