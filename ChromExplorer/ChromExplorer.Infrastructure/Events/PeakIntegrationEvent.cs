using Microsoft.Practices.Prism.Events;

namespace ChromExplorer.Infrastructure.Events
{
    public class PeakIntegrationArgs
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public bool IsRemove { get; set; }
    }

    public class PeakIntegrationEvent : CompositePresentationEvent<PeakIntegrationArgs>
    {
    }

    // Published when a Rejected method is loaded for editing: fills the Peak Integration tab only.
    // Unlike PeakIntegrationEvent it never marks or integrates anything on the graph.
    public class PeakIntegrationMethodLoadedEvent : CompositePresentationEvent<PeakIntegrationArgs>
    {
    }
}
