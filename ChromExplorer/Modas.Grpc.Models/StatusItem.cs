using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Modas.Grpc.Models
{

    public enum MTMassSpecState
    {
        Error = 0,
        Off = 1,
        Standby = 2,
        Ready = 3,
        Acquiring = 4,
        Unknown = 5,
        Pumping = 6,
    }


    public class StatusItem : INotifyPropertyChanged
    {
        private string _name;
        private string _value;
        private string _category;

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string Value
        {
            get => _value;
            set => SetField(ref _value, value);
        }

        public string Category
        {
            get => _category;
            set => SetField(ref _category, value);
        }

        private bool _isOutOfRange;

        // Set by StatusViewViewModel's per-row safety checks (Capillary Temperature baseline,
        // Buffer Gas Low, chamber vacuum thresholds) — drives the orange row highlight in
        // StatusView.
        public bool IsOutOfRange
        {
            get => _isOutOfRange;
            set => SetField(ref _isOutOfRange, value);
        }

        private string _outOfRangeReason;

        // Human-readable explanation set alongside IsOutOfRange (null once the row is back in
        // range) — shown as this row's tooltip in StatusView so hovering over e.g. Capillary
        // Temperature while it's still warming up explains why it's highlighted.
        public string OutOfRangeReason
        {
            get => _outOfRangeReason;
            set => SetField(ref _outOfRangeReason, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
