using System.Collections.Generic;

namespace Modas.Grpc.Models
{

    public class AnalysisMethodCollection : List<AnalysisMethod>
    {
        public static AnalysisMethodCollection DefaultMethods
        {
            get
            {
                var r = new AnalysisMethodCollection
                {
                    new AnalysisMethod("DSAP MS")
                    {
                        ModasMethodFile = "dsap_ms.mtm2",
                        SyringeConfig = new SyringeConfig() {IsVisible = false},
                    },
                    new AnalysisMethod("DSAP MSMS")
                    {
                        ModasMethodFile = "dsap_msms.mtm2",
                        SyringeConfig = new SyringeConfig() {IsVisible = false},
                    },
                    new AnalysisMethod("ESI MS")
                    {
                        ModasMethodFile = "esi_ms.mtm2",
                        SyringeConfig = new SyringeConfig() {IsVisible = true},
                    },
                    new AnalysisMethod("ESI MSMS")
                    {
                        ModasMethodFile = "esi_msms.mtm2",
                        SyringeConfig = new SyringeConfig() {IsVisible = true},
                    }

                };


                return r;
            }
        }
    }

    public class AnalysisMethod
    {
        public AnalysisMethod(string sampleType) : base()
        {
            SampleType = sampleType;
            this.DataFolder = @"C:\MassTech\Data";
            this.AcquisitionLengthSeconds = 30;
        }

        public string DataFolder { get; set; }

        public double AcquisitionLengthSeconds { get; set; }

        public string SampleType { get; private set; }
        public string ModasMethodFile { get; set; }

        public SyringeConfig SyringeConfig { get; set; } = new SyringeConfig();

        public IonSourceConfig IonSourceConfig { get; set; } = new IonSourceConfig();

        public MethodOverrides Overrides { get; set; } = new MethodOverrides();

 
    }
}