using System.Collections.Generic;

namespace ChromExplorer.Modules.Viewer.Services
{
    /// <summary>One label/value line inside a report section.</summary>
    public class MethodReportRow
    {
        public string Label;
        public string Value;

        public MethodReportRow(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }

    /// <summary>One titled block of the report (Sample, General, Ion Source, Acquisition Parameters)
    /// holding only the rows the .mtd file actually records.</summary>
    public class MethodReportSection
    {
        public string Title;
        public List<MethodReportRow> Rows = new List<MethodReportRow>();

        public MethodReportSection(string title)
        {
            Title = title;
        }

        public MethodReportSection Add(string label, string value)
        {
            Rows.Add(new MethodReportRow(label, value));
            return this;
        }
    }

    /// <summary>Snapshot of the acquisition settings stored in the loaded .mtd file at the moment the report is
    /// generated. The section/row lists are built dynamically from what the file records
    /// (e.g. Ion Source only if the file holds spray/heater readings, Parent Ion only for MS2/MS3), so
    /// MethodReportDocumentBuilder and MethodPdfExporter just render whatever is in here.</summary>
    public class MethodReportData
    {
        public string GeneratedDate;
        public string UserName;

        public List<MethodReportSection> Sections = new List<MethodReportSection>();

    }
}
