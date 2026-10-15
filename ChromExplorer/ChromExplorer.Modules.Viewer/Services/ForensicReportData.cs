using System.Collections.Generic;

namespace ChromExplorer.Modules.Viewer.Services
{
    public class ForensicReportData
    {
        public string GeneratedDate { get; set; }

        public string ReportNo { get; set; }
        public string CaseNo { get; set; }
        public string SampleDescription { get; set; }
        public string ReceivedDate { get; set; }
        public string AnalysisDate { get; set; }
        public string Analyst { get; set; }

        public string Conclusion { get; set; }
        public string AnalystSignature { get; set; }
        public string Reviewer { get; set; }

        public bool ShowMassSpectrum { get; set; }
        public bool ShowChromatogram { get; set; }
        public bool ShowLibrarySearch { get; set; }

        public byte[] LogoImage { get; set; }
        public byte[] ChromatogramImage { get; set; }
        public byte[] SpectrumImage { get; set; }

        public List<string[]> ResultRows { get; set; } = new List<string[]>();
    }
}
