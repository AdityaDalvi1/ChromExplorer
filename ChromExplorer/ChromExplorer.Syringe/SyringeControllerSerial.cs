using log4net;
using Serial.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.Syringe
{
    public class SyringeControllerSerial : IDisposable, ISyringeClient
    {
        private readonly object _lockObject = new object();
        //private static readonly IModasLog log = ModasLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private SerialCom _serialPort;

        public event EventHandler<SyringeMessageReceivedEventArgs> MessageReceived;

        public bool Connect(PortConfiguration config)
        {
            try
            {
                lock (this._lockObject)
                {
                    if (!this.NeedConnect(config))
                        return true;
                    if (this._serialPort != null)
                        this.Disconnect();
                    if (string.IsNullOrEmpty(config.Name))
                        return false;
                    this._serialPort = new SerialCom(config.Name, config.BaudRate.BaudRate, config.StopBits, config.Parity);
                    this._serialPort.DataReceived += new EventHandler<EventArgs>(this.SerialPortDataReceived);
                    return this._serialPort.Open();
                }
            }
            catch (Exception ex)
            {
                log.Error("syringe_connect", ex);
            }
            return false;
        }

        private bool NeedConnect(PortConfiguration config)
        {
            return this._serialPort == null || !this.IsConnected || !this.IsOpen || this._serialPort.PortName != config.Name || this._serialPort.BaudRate != config.BaudRate.BaudRate || (int)this._serialPort.DataBits != config.DataBits || this._serialPort.StopBits != config.StopBits || this._serialPort.Parity != config.Parity;
        }

        public bool Disconnect()
        {
            try
            {
                lock (this._lockObject)
                {
                    if (this._serialPort != null)
                    {
                        this._serialPort.DataReceived -= new EventHandler<EventArgs>(this.SerialPortDataReceived);
                        this._serialPort.Dispose();
                        this._serialPort = (SerialCom)null;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                log.Error((object)ex);
            }
            return false;
        }

        public bool IsConnected => this._serialPort != null;

        public bool IsOpen => this._serialPort != null && this._serialPort.IsOpen;

        public void Write(string message)
        {
            if (this._serialPort == null || !this._serialPort.IsOpen)
                return;
            lock (this._lockObject)
                this._serialPort.WriteLine(message);
        }

        public void RawWrite(byte[] buffer)
        {
            if (this._serialPort == null || !this._serialPort.IsOpen)
                return;
            lock (this._lockObject)
                this._serialPort.Write(buffer);
        }

        private void SerialPortDataReceived(object sender, EventArgs e)
        {
            try
            {
                while (this._serialPort.BytesToRead > 0)
                    this.OnMessageReceived(new SyringeMessageReceivedEventArgs(this._serialPort.PortName, this._serialPort.ReadString(16384 /*0x4000*/)));
            }
            catch (TimeoutException ex)
            {
                SyringeControllerSerial.log.Warn((object)"Timeout detected");
            }
            catch (InvalidOperationException ex)
            {
                SyringeControllerSerial.log.Warn((object)"Invalid port detected");
            }
            catch (Exception ex)
            {
                SyringeControllerSerial.log.Warn((object)ex);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }

        ~SyringeControllerSerial() => this.Dispose(false);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || this._serialPort == null)
                return;
            this.Disconnect();
        }

        protected virtual void OnMessageReceived(SyringeMessageReceivedEventArgs e)
        {
            EventHandler<SyringeMessageReceivedEventArgs> messageReceived = this.MessageReceived;
            if (messageReceived == null)
                return;
            messageReceived((object)this, e);
        }
    }
}
