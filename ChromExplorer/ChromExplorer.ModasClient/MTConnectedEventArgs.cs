using System;
using Modas.MTRemote.Data;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// A Connected event is raised when client is connected to mass spectrometer, A MTConnectedEventArgs object provides <typeparamref name="MTRegistrationResponse"></typeparamref>
    /// </summary>
    public class MTConnectedEventArgs : EventArgs
    {
        public MTConnectedEventArgs(MTRegistrationResponse resp)
        {
            Registration = resp;
        }
        public MTRegistrationResponse Registration { get; set; }
    }
}