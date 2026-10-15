namespace Modas.Grpc.Models
{

    public enum MTIonPolarity
    {
        Positive = 0,
        Negative = 1,
    }
    public class MethodOverrides
    {
        public OverrideParameter<MTIonPolarity> IonPolarity { get; set; }
            = new OverrideParameter<MTIonPolarity>() { Name = "Ion Polarity", Value = MTIonPolarity.Positive };
        public OverrideParameter<MTMeasurementTypes> MeasurementType { get; set; } 
            = new OverrideParameter<MTMeasurementTypes>() { Name = "Measurement Type", Value = MTMeasurementTypes.MS };
        public OverrideParameter<double> ParentIonMz { get; set; }
            = new OverrideParameter<double>() { Name = "Parent Ion (m/z)", Value = 100 };
        public OverrideParameter<double> FragmentationAmplitude{ get; set; }
            = new OverrideParameter<double>() { Name = "Frag. Energy (%)", Value = 10 };

        public OverrideParameter<double> SecondaryParentIonMz { get; set; }
            = new OverrideParameter<double>() { Name = "Sec. Parent Ion (m/z)", Value = 100 };
        public OverrideParameter<double> SecondaryFragmentationAmplitude { get; set; }
            = new OverrideParameter<double>() { Name = "Sec. Frag. Energy (%)", Value = 10 };
        public OverrideParameter<double> InjectionTimeMs { get; set; }
            = new OverrideParameter<double>() { Name = "Injection Time (ms)", Value = 10 };
        public OverrideParameter<double> MFrom { get; set; }
            = new OverrideParameter<double>() { Name = "m/z From", Value = 10 };
        public OverrideParameter<double> MTo { get; set; }
            = new OverrideParameter<double>() { Name = "m/z To", Value = 10 };

        public OverrideParameter<double> AgcTarget { get; set; }
            = new OverrideParameter<double>() { Name = "Agc Target", Value = 100000 };
        public OverrideParameter<int> MicroscanCount { get; set; }
            = new OverrideParameter<int>() { Name = "Microscans", Value = 1 };
    }

    /*
     * message MethodOverrides
       {
       	ClientInfo client_info = 1;
       	MTMeasurementType meas_type = 2;
       	ParentIonInfo parent_ion = 3;
       	MTIonPolarity ion_polarity = 4;
       	double injection_time = 5;
       	double m_from = 6;
       	double m_to = 7;
       }	
       
     */
}