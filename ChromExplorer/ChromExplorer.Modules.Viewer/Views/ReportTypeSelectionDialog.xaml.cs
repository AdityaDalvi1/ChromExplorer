using System.Windows;

namespace ChromExplorer.Modules.Viewer.Views
{
    public enum ReportType
    {
        General,
        Forensic,
        AddToReport,
        Method
    }

    public partial class ReportTypeSelectionDialog : Window
    {
        public ReportType SelectedReportType { get; private set; }

        /// <summary>Set by the caller before showing the dialog so it can display how many
        /// files are already queued (e.g. "3 file(s) already added").</summary>
        public int QueuedFileCount { get; set; }

        public ReportTypeSelectionDialog()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                txtAddToReport.Text = QueuedFileCount > 0
                    ? $"+ Add to Report ({QueuedFileCount})"
                    : "+ Add to Report";
            };
        }

        private void BtnGeneral_Click(object sender, RoutedEventArgs e)
        {
            SelectedReportType = ReportType.General;
            DialogResult = true;
        }

        private void BtnForensic_Click(object sender, RoutedEventArgs e)
        {
            SelectedReportType = ReportType.Forensic;
            DialogResult = true;
        }

        private void BtnAddToReport_Click(object sender, RoutedEventArgs e)
        {
            SelectedReportType = ReportType.AddToReport;
            DialogResult = true;
        }

        private void BtnMethod_Click(object sender, RoutedEventArgs e)
        {
            SelectedReportType = ReportType.Method;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
