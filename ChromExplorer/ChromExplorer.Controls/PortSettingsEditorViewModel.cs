using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows;
using Serial.Utils;

namespace ChromExplorer.Controls
{

    public class PortSettingsEditorViewModel
    {
        private const string EmptyPort = "Empty";
        public PortSettingsEditorViewModel(PortConfiguration config)
        {
            BaudRates = BaudRateDescriptor.BaudRates;
            AvailablePorts = new ObservableCollection<string>(SerialPort.GetPortNames());
            AvailablePorts.Add(EmptyPort);
            AvailableDataBits = new List<int>() { 5, 6, 7, 8 };

            Name = config.Name;
            BaudRate = config.BaudRate;
            DataBits = config.DataBits;
            this.StopBits = config.StopBits;
            Timeout = config.Timeout;
            this.Parity = config.Parity;
            this.Handshake = config.Handshake;
            this.Port = config.Port;
            this.UseTcpIP = config.UseTcpIP;
            Address = config.Address;
            TcpControlsVisibility = Visibility.Visible;
        }


        public BaudRateDescriptor BaudRate { get; set; }
        public string Name { get; set; }
        public int DataBits { get; set; }
        public StopBits StopBits { get; set; }
        public int Timeout { get; set; }
        public Parity Parity { get; set; }
        public Handshake Handshake { get; set; }
        public bool UseTcpIP { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public Visibility TcpControlsVisibility { get; set; }

        public ObservableCollection<BaudRateDescriptor> BaudRates { get; private set; }
        public ObservableCollection<string> AvailablePorts { get; private set; }
        public List<int> AvailableDataBits { get; private set; }

        public void UpdateConfig(PortConfiguration config)
        {
            config.BaudRate = BaudRate;
            config.Name = Name;

            if (Name == EmptyPort)
            {
                config.Name = string.Empty;
            }
            //sanket pandit
            config.DataBits = DataBits;
            config.StopBits = this.StopBits;
            config.Timeout = Timeout;
            config.Parity = this.Parity;
            config.Handshake = this.Handshake;
            config.UseTcpIP = this.UseTcpIP;
            config.Port = this.Port;
            config.Address = this.Address;
        }
    }
}