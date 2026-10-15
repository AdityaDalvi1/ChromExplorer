using Modas.MTRemote.Data;

namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// A IonSourceConnected event is raised when ion source with recognizable ID resistor is connected to mass spectrometer.
    /// MTIonSourceConnectedEventArgs provides information related to connected ion source as instance of MTIonSourceInfo
    /// </summary>
    public class MTIonSourceConnectedEventArgs
    {
        public MTIonSourceConnectedEventArgs(MTIonSourceInfo info)
        {
            SourceInfo = info;
        }
        public MTIonSourceInfo SourceInfo { get; set; }
    }
}