using System.ComponentModel;
using System.IO.Ports;
using Serial.Utils.Annotations;

namespace Serial.Utils
{
   
    public class PortConfiguration : INotifyPropertyChanged
    {
        private BaudRateDescriptor _baudRate;
        private string _name;
        private int _dataBits;
        private StopBits _stopBits;
        private Parity _parity;
        private int _timeout;
        private Handshake _handshake;
        private bool _useTcpIp;
        private int _port;
        private bool _serverMode;
        private string _address;

        public PortConfiguration()
        {
            BaudRate = BaudRateDescriptor.DefaultValue;
            DataBits = 8;
            this.StopBits = StopBits.One;
            this.Parity = Parity.None;
            Timeout = 500;
            this.Handshake = Handshake.None;
            UseTcpIP = false;
            Port = 4161;
            ServerMode = false;
            _address = "10.0.0.200";
        }

        public BaudRateDescriptor BaudRate
        {
            get { return _baudRate; }
            set
            {
                if (Equals(value, _baudRate)) return;
                _baudRate = value;
                OnPropertyChanged("BaudRate");
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        public int DataBits
        {
            get { return _dataBits; }
            set
            {
                if (value == _dataBits) return;
                _dataBits = value;
                OnPropertyChanged("DataBits");
            }
        }

        public StopBits StopBits
        {
            get { return _stopBits; }
            set
            {
                if (value == _stopBits) return;
                _stopBits = value;
                OnPropertyChanged("StopBits");
            }
        }

        public Parity Parity
        {
            get { return _parity; }
            set
            {
                if (value == _parity) return;
                _parity = value;
                OnPropertyChanged("Parity");
            }
        }

        public int Timeout
        {
            get { return _timeout; }
            set
            {
                if (value == _timeout) return;
                _timeout = value;
                OnPropertyChanged("Timeout");
            }
        }

        public Handshake Handshake
        {
            get { return _handshake; }
            set
            {
                if (value == _handshake) return;
                _handshake = value;
                OnPropertyChanged("Handshake");
            }
        }

        public bool UseTcpIP
        {
            get { return _useTcpIp; }
            set
            {
                if (value == _useTcpIp) return;
                _useTcpIp = value;
                OnPropertyChanged("UseTcpIP");
            }
        }

        public int Port
        {
            get { return _port; }
            set
            {
                if (value == _port) return;
                _port = value;
                OnPropertyChanged("Port");
            }
        }

        public string Address
        {
            get { return _address; }
            set
            {
                if (value == _address) return;
                _address = value;
                OnPropertyChanged("Address");
            }
        }

        

        public bool ServerMode
        {
            get { return _serverMode; }
            set
            {
                if (value == _serverMode) return;
                _serverMode = value;
                OnPropertyChanged("ServerMode");
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
