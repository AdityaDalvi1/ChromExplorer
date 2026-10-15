using ChromExplorer.Modules.Viewer.Services;
using System.Windows;

namespace ChromExplorer.Modules.Viewer.Views
{
    /// <summary>Shows the Method Report entirely inside the app — a native WPF FlowDocument
    /// built from MethodReportData via MethodReportDocumentBuilder — before the user chooses to
    /// save it. This is a standalone, single-shot report (current instrument method snapshot),
    /// unrelated to the multi-file chromatogram report queue.</summary>
    public partial class MethodReportPreviewWindow : Window
    {
        public bool SaveRequested { get; private set; }

        public MethodReportPreviewWindow(MethodReportData data)
        {
            InitializeComponent();
            docViewer.Document = MethodReportDocumentBuilder.Build(data);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveRequested = true;
            DialogResult = true;
        }

        private void BtnDiscard_Click(object sender, RoutedEventArgs e)
        {
            SaveRequested = false;
            DialogResult = false;
        }
    }
}
