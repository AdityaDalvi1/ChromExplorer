using Microsoft.Practices.Prism.Events;

namespace ChromExplorer.Infrastructure.Events
{
    public class MTE30AcquisitionRequestData
    {
        public string SampleName { get; set; }
        public int SampleRunCount { get; set; }
        public double DurationMin { get; set; }

        public string MethodHint { get; set; }

        public override string ToString()
        {
            return $"SName = {SampleName}, RunCount = {SampleRunCount}, Duration (min) = {DurationMin}, Method = {MethodHint}";
        }
    }


    public class MTE30AcquisitionRequest : CompositePresentationEvent<MTE30AcquisitionRequestData>
    {
        
    }

    public class MTE30AcquisitionStopRequest : CompositePresentationEvent<object>
    {

    }
}