using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Modas.Grpc.Models
{
    public enum MTSyringeAction
    {
        Stop = 0,
        Start = 1,
    }

    public class SyringeConfig : INotifyPropertyChanged
    {
        private double _volume = 1;
        private double _rate = 0.1;
        private double _diameter = 0.460;
        private bool _autoEnable = true;

        public double Volume
        {
            get => _volume;
            set => SetField(ref _volume, value);
        }

        public double Rate
        {
            get => _rate;
            set => SetField(ref _rate, value);
        }

        public double Diameter
        {
            get => _diameter;
            set => SetField(ref _diameter, value);
        }

        public bool AutoEnable
        {
            get => _autoEnable;
            set => SetField(ref _autoEnable, value);
        }

        public bool IsVisible { get; set; }

        public MTSyringeAction Action { get; set; } = MTSyringeAction.Stop;

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