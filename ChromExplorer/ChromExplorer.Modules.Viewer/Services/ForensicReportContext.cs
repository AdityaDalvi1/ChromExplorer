using System.ComponentModel.Composition;

namespace ChromExplorer.Modules.Viewer.Services
{
    /// <summary>
    /// Shared bridge that carries the chromatogram/spectrum plot snapshots captured by
    /// ChromatogramViewViewModel at the moment "Forensic Report" is chosen, so the
    /// ForensicReportView (opened separately via region navigation) can include the same
    /// graphs the user was looking at when generating the PDF.
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ForensicReportContext
    {
        public byte[] ChromatogramImage { get; set; }
        public byte[] SpectrumImage { get; set; }
    }
}
