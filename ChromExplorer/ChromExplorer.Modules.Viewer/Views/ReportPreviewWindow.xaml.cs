using ChromExplorer.Modules.Viewer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ChromExplorer.Modules.Viewer.Views
{
    /// <summary>What actually happened when this window closed.</summary>
    public enum ReportPreviewOutcome
    {
        Discarded,
        ReadyToSave
    }

    /// <summary>Shows the multi-file report entirely inside the app — built as a native WPF
    /// FlowDocument from the queued ChromatogramReportData entries via
    /// ReportPreviewDocumentBuilder — so the user can review exactly what's included, remove any
    /// file's results, and only then choose to save. No PDF file or external viewer is involved
    /// at this stage.
    ///
    /// With zero entries this deliberately shows a blank/empty-state document rather than
    /// falling back to whatever file happens to be open in the viewer — that file was never
    /// explicitly added via "Add to Report" and must not leak into the report.
    ///
    /// Removals here go straight through to the live ReportSessionService (persisted
    /// immediately), not a local copy — so if the caller re-reads the session after this window
    /// closes (e.g. right before exporting), it sees whatever the user removed in here.
    ///
    /// Save is immediate here — the PDF is written by the caller right after this window closes
    /// with Outcome == ReadyToSave, and (like every other saved file type) shows up for review in
    /// the "Review Saved Files" tab afterward rather than being gated before it can be saved.
    /// </summary>
    public partial class ReportPreviewWindow : Window
    {
        // ChromatogramReportData.FileName is a public FIELD, not a property, and WPF's
        // {Binding} can only resolve properties — binding straight to "Data.FileName" silently
        // resolves to nothing, which is why the file pills showed only the ✕ with no name. This
        // wrapper exposes a real FileName property for the DataTemplate to bind to.
        private class FileChip
        {
            public string FileName { get; set; }
            public ReportEntry Entry { get; set; }
        }

        private readonly ReportSessionService _session;
        private readonly ObservableCollection<FileChip> _fileItems;

        public ReportPreviewOutcome Outcome { get; private set; } = ReportPreviewOutcome.Discarded;

        /// <summary>Set only when Outcome == ReadyToSave: the entries to export.</summary>
        public List<ChromatogramReportData> ApprovedEntries { get; private set; }

        public ReportPreviewWindow(ReportSessionService session)
        {
            InitializeComponent();

            _session = session;
            _fileItems = new ObservableCollection<FileChip>(
                (session?.Entries ?? Enumerable.Empty<ReportEntry>())
                    .Select(entry => new FileChip { FileName = entry.Data?.FileName ?? "Unknown", Entry = entry }));

            fileListControl.ItemsSource = _fileItems;

            RefreshDocument();
        }

        private void RefreshDocument()
        {
            fileListPanel.Visibility = _fileItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            if (_fileItems.Count == 0)
            {
                docViewer.Document = ReportPreviewDocumentBuilder.BuildEmpty();
                txtSummary.Text = "No files added yet";
            }
            else
            {
                docViewer.Document = ReportPreviewDocumentBuilder.BuildCombined(_fileItems.Select(f => f.Entry.Data).ToList());
                txtSummary.Text = _fileItems.Count == 1 ? "1 file in this report" : $"{_fileItems.Count} files in this report";
            }

            btnSave.IsEnabled = _fileItems.Count > 0;
            btnSave.ToolTip = _fileItems.Count == 0 ? "Add at least one file to the report first." : null;
        }

        private void BtnRemoveEntry_Click(object sender, RoutedEventArgs e)
        {
            var chip = (sender as Button)?.Tag as FileChip;
            if (chip == null) return;

            _session?.Remove(chip.Entry);
            _fileItems.Remove(chip);

            RefreshDocument();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var entries = _fileItems.Select(f => f.Entry.Data).ToList();
            if (entries.Count == 0)
                return; // guarded by IsEnabled, but don't save an empty report regardless.

            ApprovedEntries = entries;
            Outcome = ReportPreviewOutcome.ReadyToSave;

            DialogResult = true;
        }

        private void BtnDiscard_Click(object sender, RoutedEventArgs e)
        {
            Outcome = ReportPreviewOutcome.Discarded;
            DialogResult = false;
        }
    }
}
