namespace Modas.Grpc.Client.Shared
{
    public enum ChannelCredentials
    {
        Insecure,
        SecureSsl,

    }

    public interface IModasRemoteClientFactory
    {
        IModasRemoteClient Create(string host, int port, ChannelCredentials credentials);
    }
}