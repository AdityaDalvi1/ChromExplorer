using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Modas.Grpc.Models;

namespace Modas.Grpc.Controls
{
    public class RemoteMessageViewViewModel : INotifyPropertyChanged
    {
        private string _dateTime;
        private RemoteMessage _message;

        public RemoteMessage Message
        {
            get => _message;
            set
            {
                if (Equals(value, _message)) return;
                _message = value;
                OnPropertyChanged();

                UpdateDateTime();
            }
        }

        private void UpdateDateTime()
        {
            var dt = Message.OccuredAt;
            string time = dt.ToShortTimeString();
            DateTime = $"{Message.OccuredAt.ToShortTimeString()}\n{Message.OccuredAt.ToShortDateString()}";
        }

        public string DateTime
        {
            get => _dateTime;
            private set
            {
                if (value == _dateTime) return;
                _dateTime = value;
                OnPropertyChanged();
            }
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