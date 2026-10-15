using System;
using Modas.MTRemote.Data;

namespace ChromExplorer.ModasClient
{
    public class MTMessageReceivedEventArgs : EventArgs
    {
        public MTMessageReceivedEventArgs(MTMessage msg)
        {
            Severity = msg.Severity;
            Message = msg.Message;
        }

        public MTMessageSeverity Severity { get; private set; }
        public string Message { get; private set; }

    }
}