using System.Collections.Generic;

namespace ChromExplorer.Modules.Viewer.Services
{
    /// <summary>Label/value row used for the post-processing settings tables. When IsStatusCell is
    /// true the value is rendered as an Active/Inactive pill instead of plain text.</summary>
    public class PdfKeyValueRow
    {
        public string Label;
        public string Value;
        public bool IsStatusCell;
        public bool IsActive;

        // DataContractJsonSerializer (ReportSessionService's queue persistence serializes
        // ChromatogramReportData, which embeds lists of these) requires a parameterless
        // constructor to build a contract for a plain POCO with no [DataContract] attributes �
        // without one it throws InvalidDataContractException the moment it encounters this type,
        // even for an empty list. Kept alongside the convenience constructor below, which every
        // other call site still uses.
        public PdfKeyValueRow()
        {
        }

        public PdfKeyValueRow(string label, string value, bool isStatusCell = false, bool isActive = false)
        {
            Label = label;
            Value = value;
            IsStatusCell = isStatusCell;
            IsActive = isActive;
        }
    }

    /// <summary>All data needed to render the chromatogram analysis report, gathered up-front by the
    /// ViewModel so the exporter itself only deals with layout/drawing.</summary>
    public class ChromatogramReportData
    {
        public string GeneratedDate;
        public string FileName;
        public string InstrumentName = "ChromExplorer 2.0";
        public string UserName;
        public string Description;

        // Populated only for a General Report that went through the review workflow � stamped
        // by ReportPreviewWindow from the approved ReportApprovalService submission right before
        // export, so ChromatogramPdfExporter can print who approved it next to who generated it.
        // Null/empty for anything saved outside that workflow (there is none right now, but this
        // keeps the exporter safe either way).
        public string ReviewedByName;
        public string ReviewedDate;

        // Chromatogram Output
        public string TotalScans;
        public string DataPoints;
        public string SelectedIndex;
        public string SelectedSIC;
        public string VisibleArea;
        public string XAxisUnits;

        // Spectrum Output
        public string RetentionTime;
        public string ScanIndex;
        public string ScanID;
        public string InjectionTimeMs;
        public string TIC;
        public string ParentIon;
        public string BaselineRemoved;

        public byte[] LogoImage;
        public byte[] ChromatogramImage;
        public byte[] SpectrumImage;

        public List<string[]> ChromPeakRows = new List<string[]>();
        public List<string[]> SpectrumPeakRows = new List<string[]>();

        public List<PdfKeyValueRow> SpectrumPostProcessing = new List<PdfKeyValueRow>();
        public List<PdfKeyValueRow> EicPostProcessing = new List<PdfKeyValueRow>();

        // Manual peak integration
        public string IntegrationStatus;
        public string IntegrationStartTime;
        public string IntegrationEndTime;
        public string IntegrationRT;
        public string IntegrationArea;

        // Signal-to-noise
        public string SNRStatus;
        public string SNRSignalDefinition;
        public string SNRSignalValue;
        public string SNRNoiseDefinition;
        public string SNRNoiseValue;
        public string SNRRatio;
        public string SNRNoiseRegionUsed;

        // RSD (Relative Standard Deviation) � same shared-session shape as Quan below: reflects
        // the whole RsdService queue, not any single file, so every entry carries an identical
        // copy (see ChromatogramViewViewModel.BuildReportData). Rendered once per report, just
        // before the Quan section � see ChromatogramPdfExporter/ReportPreviewDocumentBuilder.
        public List<string[]> RsdRows = new List<string[]>();

        // Individual files queued in the RSD tab (its "Queued Files" grid).
        public List<string[]> RsdQueuedRows = new List<string[]>();

        // Quantitation
        public List<string[]> QuanRows = new List<string[]>();
        public string QuanEquation;
        public byte[] QuanChartImage;
    }
}
