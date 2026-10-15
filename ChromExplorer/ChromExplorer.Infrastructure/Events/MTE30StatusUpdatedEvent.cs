using Microsoft.Practices.Prism.Events;

namespace ChromExplorer.Infrastructure.Events
{
    public class MTE30StatusData
    {
        public bool Connected { get; set; }
    }


    public class MTE30StatusUpdatedEvent : CompositePresentationEvent<MTE30StatusData>
    {
        
    }

    public class MTE30StatusRequest : CompositePresentationEvent<object>
    {

    }
}