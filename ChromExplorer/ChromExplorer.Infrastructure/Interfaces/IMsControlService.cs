namespace ChromExplorer.Infrastructure.Interfaces
{
    public interface IMsControlService
    {
        void Start();
        void Stop();
        void Connect();
        void Disconnect();
        string Ping();
    }
}    