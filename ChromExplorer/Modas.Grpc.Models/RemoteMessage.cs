using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Modas.Grpc.Models
{

    public enum MessageSeverity
    {
        Info = 0,
        Warn = 1,
        Error = 2,
    }

    public enum ErrorCode
    {
        None = 0,
        ErrorInstrumentState = 1,
        ErrorOperationInProgress = 2,
        ErrorFileNotFound = 3,
        ErrorWeb = 4,
        ErrorMethodFile = 5,
        ErrorOperation = 6,
    }

    public class RemoteMessage : INotifyPropertyChanged
    {
        private DateTime _occuredAt;
        private MessageSeverity _severity;
        private ErrorCode _error;
        private string _message;
        private string _exception;
            
        public DateTime OccuredAt
        {
            get => _occuredAt;
            set => SetField(ref _occuredAt, value);
        }

        public MessageSeverity Severity
        {
            get => _severity;
            set
            {
                if (value == _severity) return;
                _severity = value;
                OnPropertyChanged();
            }
        }

        public ErrorCode Error
        {
            get => _error;
            set => SetField(ref _error, value);
        }

        public string Message
        {
            get => _message;
            set => SetField(ref _message, value);
        }

        public string Exception
        {
            get => _exception;
            set => SetField(ref _exception, value);
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

