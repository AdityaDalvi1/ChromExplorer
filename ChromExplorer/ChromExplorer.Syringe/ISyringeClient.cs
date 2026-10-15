using System;
using Serial.Utils;

namespace ChromExplorer.Syringe
{
    public interface ISyringeClient
    {
        bool Connect(PortConfiguration config);

        bool IsConnected { get; }

        void Write(string message);

        bool Disconnect();

        event EventHandler<SyringeMessageReceivedEventArgs> MessageReceived;
    }
}