using System;
using Microsoft.Practices.Prism.Events;

namespace ChromExplorer.Mocks
{
    public class MockEventAggregator : IEventAggregator
    {
        public TEventType GetEvent<TEventType>() where TEventType : EventBase, new()
        {
            Console.WriteLine("GetEvent<TEventType>() called");
            return new TEventType();
        }
    }
}

