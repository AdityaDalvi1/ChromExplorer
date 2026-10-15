using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Modas.Grpc.Models
{
    public class OverrideParameter<T1> : INotifyPropertyChanged
    {
        private T1 _value;
        private bool _override = false;
        private bool _isVisible = true;
        public string Name { get; set; }

        public T1 Value
        {
            get => _value;
            set => SetField(ref _value, value);
        }

        public bool Override
        {
            get => _override;
            set => SetField(ref _override, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetField(ref _isVisible, value);
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
