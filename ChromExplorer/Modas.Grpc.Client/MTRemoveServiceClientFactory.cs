using Grpc.Core;
using Modas.Grpc.Client.Shared;
using System.ComponentModel.Composition;
using ChannelCredentials = Modas.Grpc.Client.Shared.ChannelCredentials;

namespace Modas.Grpc.Client
{
    [Export(typeof(IModasRemoteClientFactory))]
    public class MTRemoveServiceClientFactory : Modas.Grpc.Client.Shared.IModasRemoteClientFactory
    {
        public IModasRemoteClient Create(string host, int port, ChannelCredentials credentials)
        {
            var channel = new Channel(host, port, global::Grpc.Core.ChannelCredentials.Insecure);

            return new MTRemoteServiceClientImpl(channel);

        }
    }
}