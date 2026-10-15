using Microsoft.Practices.Prism.Events;
using ChromExplorer.Infrastructure.Interfaces;

namespace ChromExplorer.Infrastructure.Events
{
    public class SNRCalculatedEvent : CompositePresentationEvent<SNRResult>
    {
    }
}
