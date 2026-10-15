using System;
using Microsoft.Practices.Prism.Events;

namespace ChromExplorer.Infrastructure.Events
{
    public class DataProviderChangedEvent : CompositePresentationEvent<Type>
    {
    }
}
