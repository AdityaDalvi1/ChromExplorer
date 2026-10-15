using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modas.Grpc.Models;

namespace Modas.Grpc.Client.Shared
{
    public class StatusReceivedEventArgs
    {
        public string IonSourceID { get; set; }
        public MTMassSpecState State { get; set; }
        public DateTime OccuredAt { get; set; }

        public List<StatusItem> StatusItems { get; set; }
    }

    public class MeasurementsReceivedEventArgs
    {
        public MTMeasurementDataSet Dataset { get; set; }
    }

    public interface IModasRemoteClient : IDisposable
    {  
        event EventHandler<StatusReceivedEventArgs> StatusReceived;
        event EventHandler<MeasurementsReceivedEventArgs> MeasurementReceived;
        event EventHandler MeasurementsCompleted;
        Task<Modas.Grpc.Models.RequestControlStatus> RequestControl();
     
        Task<string> Ping();

        Task UpdateStatusFeed(int intervalMs = 5000);
        void StopStatusFeed();

        Task<RemoteMessage> Reset();

        Task<RemoteMessage> ToPowerOff();
        Task<RemoteMessage> ToStandby();

        Task<RemoteMessage> ToReady();

        Task<MTMassSpecState> GetInstrumentState();

        Task<string> GetIonSourceInfo();

        Task StartMeasurements(AcquisitionConfig config);

        void StopMeasurements();

        Task<RemoteMessage> SyringeControl(SyringeConfig config);

        Task<RemoteMessage> UpdateSprayVoltage(IonSourceConfig config);

        Task<RemoteMessage> UpdateDsapHeater(IonSourceConfig config);

        Task<RemoteMessage> LoadMethodFile(string fileName);
        Task<RemoteMessage> ApplyMethodOverrides(MethodOverrides overrides);
    }
}   
    