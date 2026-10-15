using System;

namespace ChromExplorer.Syringe
{
    public class SyringeMessageReceivedEventArgs : EventArgs
    {
        public SyringeMessageReceivedEventArgs(string portName, string message)
        {
            this.PortName = portName;
            this.Message = message;
        }

        public string PortName { get; private set; }

        public string Message { get; private set; }
    }

}