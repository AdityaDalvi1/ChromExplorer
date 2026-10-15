namespace Modas.Grpc.Models
{
    public class RequestControlStatus
    {
        public RequestControlStatus(string sn, string sv, bool granted)
        {
            SerialNumber = sn;
            SoftwareVersion = sv;
            IsControlGranted = granted;
        }

        public string SerialNumber { get; private set; }
        public string SoftwareVersion { get; private set; }
        public bool IsControlGranted { get; private set; }
    }
}