using System;
using Modas.MTRemote.Data;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// A StateChanged event is raised when mass spectrometer state changes. MTMassSpecStateChangedEventArgs provides current state of the mass spectrometer
    /// </summary>
    public class MTMassSpecStateChangedEventArgs : EventArgs
    {

        public MTMassSpecStateChangedEventArgs(MTMassSpecState state)
        {
            State = state;
        }

        public MTMassSpecStateChangedEventArgs(int state)
        {
            State = MTMassSpecState.Unknown;
            if (Enum.IsDefined(typeof(MTMassSpecState), state))
            {
                MTMassSpecState s = (MTMassSpecState)Enum.ToObject(typeof(MTMassSpecState), state);
                State = s;
            }
        }

        public MTMassSpecState State { get; private set; }
    }
}