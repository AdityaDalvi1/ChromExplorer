using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.Auth.Models;
using ChromExplorer.Modules.Auth.Services;
using ChromExplorer.Modules.Auth.ViewModels;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Win32;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ChromExplorer.Modules.Auth.Views
{
    // Open to every role (granted via AuthService.EnsureReviewerFeatureSeed) — lists everything
    // ChromExplorer.Infrastructure.SavedFileRegistry has recorded: saved Sample Sequences,
    // generated Reports, Method Files, and Data Processing Methods. A Reviewer sees every
    // creator's files; anyone else only sees the files they themselves saved (see LoadData).
    // Reviewing/signing a file is still restricted to the Reviewer role specifically.
    [Export(typeof(ReviewSavedFiles))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ReviewSavedFiles : UserControl, INavigationAware
    {
        private readonly ReviewSavedFilesViewModel _viewModel;
        private string _currentTab = SavedFileRegistry.TypeSampleSequence;
        private bool _loaded;

        [Import]
        public IRegionManager RegionManager { get; set; }

        [Import]
        public ChromDataProviderFactory ChromDataProviderFactory { get; set; }

        public ReviewSavedFiles()
        {
            _viewModel = new ReviewSavedFilesViewModel();
            InitializeComponent();
            _loaded = true;
            LoadData();
            IntimateReviewerOfPendingReviews();
            IntimateCreatorOfReviewedFiles();
        }

        // The other half of "whoever created it also gets a notification" — whether they're a
        // Reviewer or not, whoever is logged in gets told about any file THEY created/submitted
        // that has since been reviewed (Approved or Rejected), once, the next time they open this
        // screen.
        private void IntimateCreatorOfReviewedFiles()
        {
            if (Application.Current?.Properties?["UserId"] == null)
                return;

            int userId = Convert.ToInt32(Application.Current.Properties["UserId"]);
            var reviews = SavedFileRegistry.GetUnnotifiedReviewsForCreator(userId);
            if (reviews.Count == 0)
                return;

            var lines = new System.Collections.Generic.List<string>();
            foreach (var r in reviews)
                lines.Add($"{r.fileName} — {r.verdict} by {r.reviewedBy}");

            MessageBox.Show(
                "Your file(s) have been reviewed:\n\n" + string.Join("\n", lines),
                "Review Update", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // "Intimation to reviewer" — since this app has no push/email notification system, the
        // intimation is this: the moment a Reviewer opens this screen, they're told how many
        // files across EVERY category (Sample Sequence, Report, Sample Analysis, Method File)
        // are still waiting on their review. Method File uses the explicit submit workflow
        // (review_status); the other three don't have a submit step, so anything active and not
        // yet reviewed counts as pending.
        private void IntimateReviewerOfPendingReviews()
        {
            if (!new AuthService().IsCurrentUserReviewer())
                return;

            var lines = new System.Collections.Generic.List<string>();

            int sequencePending = SavedFileRegistry.GetUnreviewedCount(SavedFileRegistry.TypeSampleSequence);
            if (sequencePending > 0) lines.Add($"{sequencePending} Sample Sequence file(s)");

            int reportPending = SavedFileRegistry.GetUnreviewedCount(SavedFileRegistry.TypeReport);
            if (reportPending > 0) lines.Add($"{reportPending} Report(s)");

            int analysisPending = SavedFileRegistry.GetUnreviewedCount(SavedFileRegistry.TypeSampleAnalysis);
            if (analysisPending > 0) lines.Add($"{analysisPending} Sample Analysis file(s)");

            int dataProcessingPending = SavedFileRegistry.GetUnreviewedCount(SavedFileRegistry.TypeDataProcessingMethod);
            if (dataProcessingPending > 0) lines.Add($"{dataProcessingPending} Data Processing Method file(s)");

            int methodPending = SavedFileRegistry.GetPendingReviewCount(SavedFileRegistry.TypeMethodFile);
            if (methodPending > 0) lines.Add($"{methodPending} Method File(s) submitted for review");

            if (lines.Count > 0)
            {
                MessageBox.Show(
                    "You have files waiting on your review:\n\n" + string.Join("\n", lines),
                    "Pending Reviews", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Reloads the grid for whichever tab (Sample Sequence / Report / Sample Analysis) is
        // currently selected. A Reviewer sees every creator's files; anyone else only sees the
        // files they themselves saved.
        private void LoadData()
        {
            bool isReviewer = new AuthService().IsCurrentUserReviewer();
            int currentUserId = 0;
            if (Application.Current?.Properties?["UserId"] != null)
                currentUserId = Convert.ToInt32(Application.Current.Properties["UserId"]);

            var records = _viewModel.GetSavedFiles(_currentTab, currentUserId, isReviewer);
            foreach (var record in records)
                record.IsReviewerSession = isReviewer;

            SavedFilesGrid.ItemsSource = records;
            EmptyStateText.Visibility = records.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ReviewTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // The default-selected tab fires this during InitializeComponent, before the grid's
            // own named elements exist yet — ignore it; the constructor's own LoadData() call
            // right after InitializeComponent covers that first load.
            if (!_loaded) return;

            var tag = (ReviewTabs.SelectedItem as TabItem)?.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            _currentTab = tag;
            LoadData();
        }

        private void CloseView()
        {
            if (RegionManager == null)
                return;

            var popupRegion = RegionManager.Regions[RegionNames.OverContentRegion];
            popupRegion?.Deactivate(this);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            CloseView();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            LoadData();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        private void Action_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string action = btn?.Tag?.ToString();
            SavedFileRecord record = btn?.CommandParameter as SavedFileRecord;

            if (record == null) return;

            if (action == "View")
            {
                OpenRecordFile(record);
            }
            else if (action == "Download")
            {
                DownloadRecordFile(record);
            }
            else if (action == "Review")
            {
                ReviewRecord(record);
            }
            else if (action == "ToggleActive")
            {
                ToggleActive(record);
            }
            else if (action == "SubmitForReview")
            {
                SubmitForReview(record);
            }
        }

        // Stage 1 of the Method File workflow ("Submit by analyst for review") — open to any
        // authorized user, not just the Reviewer role, since this is the analyst's own step.
        // Same reason+password confirmation as every other state-changing action here.
        private void SubmitForReview(SavedFileRecord record)
        {
            string reason = ShowReasonDialog("Reason for Submitting this Method File for Review");
            if (reason == null) return;

            var confirm = MessageBox.Show(
                $"Submit this method file for the Reviewer's review?\n\n{record.FileName}{record.FileExt}\n\nReason: {reason}",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            if (!ConfirmCurrentUserPassword("Method File - Submit for Review", reason)) return;

            int userId = 0;
            if (Application.Current?.Properties?["UserId"] != null)
                userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

            string fullName = new AuthService().GetCurrentUserFullName();

            SavedFileRegistry.SubmitForReview(record.SavedFileId, userId, fullName);

            new AuthService().Log("Method File - Submitted for Review",
                $"Method file '{record.FileName}{record.FileExt}' submitted for review by '{fullName}' | Reason: {reason}");

            LoadData();
        }

        // Plain "View" — opens the file immediately, with no reason/password prompt and no
        // remark/verdict panel. Anyone with page access can look at the data; this is just
        // looking, not reviewing it.
        private void OpenRecordFile(SavedFileRecord record)
        {
            new AuthService().Log("Saved File - View",
                $"Viewed saved file record '{record.FileName}{record.FileExt}' ({record.RecordType})");

            OpenFileContent(record, allowReview: false);
        }

        // "Download" — only offered for a reviewed/signed Report (see SavedFileRecord.CanDownload).
        // Saves a copy of the already-signed PDF into the fixed report folder for its kind
        // (D:\ChromReport\General|Method|Forensic by default, see ReportStorage) as
        // "<name>_Reviewed.pdf" — users can't choose another location.
        private void DownloadRecordFile(SavedFileRecord record)
        {
            string sourcePath = record.FullPath;
            if (!File.Exists(sourcePath))
            {
                MessageBox.Show($"File not found on disk: {sourcePath}", "File Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var kind = ReportStorage.GetKindFromFileName(record.FileNameWithExt);
            string folder = ReportStorage.EnsureFolder(kind);
            if (folder == null)
                return;

            string ext = string.IsNullOrEmpty(record.FileExt) ? ".pdf" : record.FileExt;
            string targetPath = Path.Combine(folder, record.FileName + "_Reviewed" + ext);

            if (File.Exists(targetPath))
            {
                ReportStorage.ShowSavedMessage(targetPath, "Already Downloaded",
                    "This reviewed report was already downloaded to the location above.");
                return;
            }

            try
            {
                File.Copy(sourcePath, targetPath, overwrite: false);

                new AuthService().Log("Saved File - Download",
                    $"Downloaded reviewed saved file record '{record.FileName}{record.FileExt}' ({record.RecordType}) to '{targetPath}'");

                ReportStorage.ShowSavedMessage(targetPath, "Report Downloaded");
            }
            catch (Exception ex)
            {
                new AuthService().Log("Saved File - Download Failed",
                    $"Failed to download saved file record '{record.FileName}{record.FileExt}' ({record.RecordType}): {ex.Message}");

                MessageBox.Show("Error downloading file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // "Review" — the gated flow: reason, then password, then (only once both of those check
        // out) the Reviewer role itself is verified, and only THEN is the file opened, this time
        // with the remark + Approve/Reject panel available.
        private void ReviewRecord(SavedFileRecord record)
        {
            if (record.IsReviewed)
            {
                MessageBox.Show(
                    $"This file was already reviewed and electronically signed by {record.ReviewedByUsername} ({record.ReviewedByRole}) on {record.ReviewedDate}.\n\nIt cannot be reviewed again.",
                    "Already Reviewed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (record.IsMethodFile && record.ReviewStatus != SavedFileRegistry.StatusPendingReview)
            {
                MessageBox.Show(
                    "This method file has not been submitted for review by the analyst yet.",
                    "Not Yet Submitted", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string reason = ShowReasonDialog("Reason for Reviewing this File");
            if (reason == null) return;

            if (!ConfirmCurrentUserPassword("Saved File - Review", reason)) return;

            var auth = new AuthService();
            if (!auth.IsCurrentUserReviewer())
            {
                auth.Log("Saved File - Review - Access Denied",
                    $"User is not in the Reviewer role | Reason given: {reason}");

                MessageBox.Show("Only the Reviewer role can review saved file records.",
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            auth.Log("Saved File - Review Opened",
                $"Opened saved file record '{record.FileName}{record.FileExt}' ({record.RecordType}) for review | Reason: {reason}");

            OpenFileContent(record, allowReview: true);
        }

        // Shared "open the file" logic for both View and Review — only the remark/verdict panel
        // differs between the two.
        private void OpenFileContent(SavedFileRecord record, bool allowReview)
        {
            var path = record.FullPath;

            if (!File.Exists(path))
            {
                new AuthService().Log("Saved File - Open Failed",
                    $"'{record.FileName}{record.FileExt}' ({record.RecordType}) could not be found at its recorded location: {path}");

                MessageBox.Show($"This file could not be found at its recorded location:\n{path}",
                    "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (record.RecordType == SavedFileRegistry.TypeSampleAnalysis)
            {
                // Offer the remark before the page closes/navigates away for this one, since
                // OpenInChromatogramViewer tears this view down.
                if (allowReview)
                    OfferReviewRemark(record);
                OpenInChromatogramViewer(path);
                return;
            }

            var ext = (record.FileExt ?? "").ToLowerInvariant();

            // Sample Sequence (.csv), Method File and Data Processing Method (.mtm2/.cmethod/XML)
            // and Report (.pdf) are all shown in a viewer built right into this screen — a real
            // grid for the CSV, a formatted text panel for the XML, an embedded PDF page for the
            // report — instead of depending on Excel/Adobe or a file association that might not
            // exist on this machine. The Reviewer's remark/verdict panel is embedded right in
            // that same window when allowReview is true, next to the actual file content, so the
            // reviewer sees exactly what they're signing off on without switching windows.
            if (ext == ".csv")
            {
                ShowCsvViewer(path, record.FileNameWithExt, record, allowReview);
                return;
            }
            else if (ext == ".mtm2" || ext == ".xml" || ext == ".cmethod")
            {
                ShowTextFileViewer(path, record.FileNameWithExt, record, allowReview);
                return;
            }
            else if (ext == ".pdf")
            {
                ShowPdfViewer(path, record.FileNameWithExt, record, allowReview);
                return;
            }
            else
            {
                // "View" must not double as an edit — mark the file read-only on disk first, so
                // the PDF reader (or whatever else) shows it read-only rather than letting it be
                // silently modified.
                try
                {
                    var attrs = File.GetAttributes(path);
                    if ((attrs & FileAttributes.ReadOnly) == 0)
                        File.SetAttributes(path, attrs | FileAttributes.ReadOnly);
                }
                catch
                {
                    // Best-effort — a saved record should still open even if the read-only flag
                    // couldn't be set (e.g. permissions on that folder).
                }

                try
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    new AuthService().Log("Saved File - Open Failed",
                        $"Shell-execute failed for '{record.FileName}{record.FileExt}' ({record.RecordType}) | Error: {ex.Message} | Falling back to Notepad");

                    if (!TryOpenInNotepad(path))
                        return;
                }
            }

            if (allowReview)
                OfferReviewRemark(record);
        }

        private static bool TryOpenInNotepad(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = true });
                return true;
            }
            catch (Exception ex)
            {
                new AuthService().Log("Saved File - Open Failed",
                    $"Notepad fallback also failed for '{path}' | Error: {ex.Message}");

                MessageBox.Show($"Could not open this file:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // Shows a .csv (Sample Sequence) file's rows/columns in an actual grid, right inside the
        // app, instead of relying on Excel being installed and un-editing what the reviewer's
        // "View" opened. Handles simple quoted fields the same way this app's own CSV writer
        // (EscapeCsvField, in MtRemoteGrpcDashboardViewModel) produces them. A running Sr.No
        // column is always added up front — the sequence itself can have any number of rows (2,
        // 10, 60, whatever), and none of that numbering lives in the CSV's own columns.
        private void ShowCsvViewer(string path, string title, SavedFileRecord record, bool allowReview)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                new AuthService().Log("Saved File - Open Failed",
                    $"Could not read '{record.FileName}{record.FileExt}' ({record.RecordType}) | Error: {ex.Message}");

                MessageBox.Show($"Could not read this file:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new Window
            {
                Title = title,
                Width = 950,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = true,
                IsReadOnly = true,
                CanUserAddRows = false,
                Margin = new Thickness(12),
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal
            };

            var signatureBanner = ExtractCsvSignatureBanner(ref lines);

            if (lines.Length > 0)
            {
                var header = SplitCsvLine(lines[0]);
                var table = new System.Data.DataTable();
                // Named "Sr No" (no dot) rather than "Sr.No" — DataGrid's AutoGenerateColumns
                // builds each cell's binding from the column name, and WPF's binding-path parser
                // treats a "." as a nested-property separator, not a literal character. With a
                // dot in the name, every cell silently binds to nothing and renders blank while
                // the column header (plain text, unaffected) still looks fine.
                // Sequence files saved by this app already carry their own "Serial Number" column —
                // adding a running "Sr No" on top showed two numbering columns side by side. The
                // running number is only added for files that have no serial column of their own.
                bool hasOwnSerial = header.Any(h => string.Equals(h?.Trim(), "Serial Number", StringComparison.OrdinalIgnoreCase));
                int offset = hasOwnSerial ? 0 : 1;
                if (!hasOwnSerial)
                    table.Columns.Add("Sr No");
                foreach (var col in header)
                    table.Columns.Add(string.IsNullOrWhiteSpace(col) ? " " : col);

                int srNo = 1;
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    var fields = SplitCsvLine(lines[i]);
                    var row = table.NewRow();
                    if (!hasOwnSerial)
                        row[0] = srNo++;
                    for (int c = 0; c < fields.Length && c + offset < table.Columns.Count; c++)
                        row[c + offset] = fields[c];
                    table.Rows.Add(row);
                }

                dataGrid.ItemsSource = table.DefaultView;
            }

            ShowReviewableDialog(dialog, dataGrid, record, allowReview, signatureBanner);
        }

        // A signed CSV starts with the "ELECTRONIC SIGNATURE" block (see SignCsvFile) followed by
        // a blank line before the real header row — this pulls that block out into its own banner
        // (shown prominently at the top of the viewer, like an email signature) and strips those
        // lines from what gets parsed as the data table, so the signature doesn't show up as a
        // bogus extra "row" of column headers.
        private static FrameworkElement ExtractCsvSignatureBanner(ref string[] lines)
        {
            if (lines.Length == 0 || !string.Equals(lines[0].Trim(), "ELECTRONIC SIGNATURE", StringComparison.OrdinalIgnoreCase))
                return null;

            int end = 1;
            var fields = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (end < lines.Length && !string.IsNullOrWhiteSpace(lines[end]))
            {
                var kv = SplitCsvLine(lines[end]);
                if (kv.Length >= 2)
                    fields[kv[0].Trim()] = kv[1];
                end++;
            }

            lines = lines.Length > end + 1
                ? new System.Collections.Generic.List<string>(lines).GetRange(end + 1, lines.Length - end - 1).ToArray()
                : new string[0];

            return BuildSignatureBanner(fields);
        }

        // ONE format for every signature line (CSV / XML banner, PDF stamp, the "already
        // reviewed" line): "<mark> Reviewed by: <user> (<role>) | Verdict: <v> | <date> |
        // Remark: <remark>" — green + ✔ when Approved, red + ✖ when Rejected.
        private static string FormatSignatureLine(string username, string role, string verdict, string signedDate, string remark)
        {
            bool rejected = string.Equals(verdict, SavedFileRegistry.VerdictRejected, StringComparison.OrdinalIgnoreCase);
            return $"{(rejected ? "✖" : "✔")} Reviewed by: {username} ({role})  |  Verdict: {verdict}  |  {signedDate}  |  Remark: {remark}";
        }

        private static FrameworkElement BuildSignatureBanner(System.Collections.Generic.IDictionary<string, string> fields)
        {
            string Get(string key) => fields.TryGetValue(key, out var v) ? v : "";

            string verdict = Get("Verdict");
            bool rejected = string.Equals(verdict, SavedFileRegistry.VerdictRejected, StringComparison.OrdinalIgnoreCase);

            return new Border
            {
                Background = rejected ? System.Windows.Media.Brushes.MistyRose : System.Windows.Media.Brushes.Honeydew,
                BorderBrush = rejected ? System.Windows.Media.Brushes.Firebrick : System.Windows.Media.Brushes.SeaGreen,
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(14, 10, 14, 10),
                Child = new TextBlock
                {
                    Text = FormatSignatureLine(Get("Reviewed By"), Get("Role"), verdict, Get("Reviewed Date"), Get("Remark")),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = rejected ? System.Windows.Media.Brushes.Firebrick : System.Windows.Media.Brushes.DarkGreen,
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        private static string[] SplitCsvLine(string line)
        {
            var fields = new System.Collections.Generic.List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else if (c == '"') inQuotes = false;
                    else current.Append(c);
                }
                else
                {
                    if (c == '"') inQuotes = true;
                    else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
                    else current.Append(c);
                }
            }
            fields.Add(current.ToString());
            return fields.ToArray();
        }

        // Shows a Method File (.mtm2, which is plain XML) as a clean "Setting / Value" table —
        // reading raw XML tags is unusable for a reviewer who just needs to check the actual
        // configured values. Falls back to the raw text if the file isn't well-formed XML.
        private void ShowTextFileViewer(string path, string title, SavedFileRecord record, bool allowReview)
        {
            string content;
            try
            {
                content = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                new AuthService().Log("Saved File - Open Failed",
                    $"Could not read '{record.FileName}{record.FileExt}' ({record.RecordType}) | Error: {ex.Message}");

                MessageBox.Show($"Could not read this file:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new Window
            {
                Title = title,
                Width = 820,
                Height = 680,
                MinWidth = 600,
                MinHeight = 400,
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            System.Xml.Linq.XDocument doc = null;
            try
            {
                doc = System.Xml.Linq.XDocument.Parse(content);
            }
            catch
            {
                // Not well-formed XML (or not XML at all) — fall through to the raw-text view.
            }

            if (doc?.Root == null)
            {
                var textBox = new TextBox
                {
                    Text = content,
                    IsReadOnly = true,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.NoWrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Margin = new Thickness(12),
                    Background = System.Windows.Media.Brushes.White
                };

                ShowReviewableDialog(dialog, textBox, record, allowReview);
                return;
            }

            FrameworkElement signatureBanner = ExtractXmlSignatureBanner(doc);

            var table = new System.Data.DataTable();
            table.Columns.Add("Setting");
            table.Columns.Add("Value");
            if (doc.Root.Name.LocalName == "ChromMethodFile")
                AppendMethodSummaryRows(doc.Root, table);
            else
                AppendXmlLeafValues(doc.Root, table, "");

            var titleBar = new Border
            {
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                Padding = new Thickness(16, 12, 16, 12),
                Child = new TextBlock
                {
                    Text = title,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.DarkSlateGray
                }
            };

            var gridCard = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(16, 0, 16, 16)
            };

            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                CanUserAddRows = false,
                CanUserResizeRows = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = System.Windows.Media.Brushes.WhiteSmoke,
                BorderThickness = new Thickness(0),
                RowHeight = 32,
                FontSize = 13,
                AlternatingRowBackground = System.Windows.Media.Brushes.WhiteSmoke,
                ItemsSource = table.DefaultView
            };

            dataGrid.ColumnHeaderStyle = new Style(typeof(DataGridColumnHeader));
            dataGrid.ColumnHeaderStyle.Setters.Add(new Setter(Control.BackgroundProperty, System.Windows.Media.Brushes.DarkSlateGray));
            dataGrid.ColumnHeaderStyle.Setters.Add(new Setter(Control.ForegroundProperty, System.Windows.Media.Brushes.White));
            dataGrid.ColumnHeaderStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
            dataGrid.ColumnHeaderStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 8, 10, 8)));
            dataGrid.ColumnHeaderStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));

            var settingCellStyle = new Style(typeof(DataGridCell));
            settingCellStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
            settingCellStyle.Setters.Add(new Setter(Control.ForegroundProperty, System.Windows.Media.Brushes.DimGray));

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Setting",
                Binding = new System.Windows.Data.Binding("Setting"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                CellStyle = settingCellStyle
            });
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Value",
                Binding = new System.Windows.Data.Binding("Value"),
                Width = new DataGridLength(1.2, DataGridLengthUnitType.Star)
            });

            gridCard.Child = dataGrid;

            var body = new DockPanel();
            DockPanel.SetDock(titleBar, Dock.Top);
            body.Children.Add(titleBar);
            body.Children.Add(gridCard);

            ShowReviewableDialog(dialog, body, record, allowReview, signatureBanner);
        }

        // Shows a Report (.pdf) right inside the app via an embedded WebBrowser control pointed
        // at the file — the same "view it inside the application" treatment as CSV/XML, so the
        // Reviewer sees the actual signed-off content (chromatogram, peaks, everything printed
        // in the PDF) right next to the remark/Approve/Reject panel instead of having to alt-tab
        // to a separate PDF reader window. A toolbar "Open in External Viewer" fallback is kept
        // in case the local WebBrowser control's PDF plugin isn't available on this machine.
        private void ShowPdfViewer(string path, string title, SavedFileRecord record, bool allowReview)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 900,
                Height = 760,
                MinWidth = 600,
                MinHeight = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var toolbar = new Border
            {
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                Padding = new Thickness(12, 8, 12, 8),
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            var toolbarPanel = new DockPanel();
            var titleText = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            // The embedded WebView2 control can end up covering/obscuring this Window's own
            // native title bar close button in some DPI/positioning setups — always give the
            // user an explicit, guaranteed-visible way to leave this screen instead of relying
            // solely on window chrome.
            var btnBack = new Button
            {
                Content = "Back",
                Padding = new Thickness(14, 4, 14, 4),
                Margin = new Thickness(0, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            btnBack.Click += (s, e) => dialog.Close();

            var btnOpenExternal = new Button
            {
                Content = "Open in External Viewer",
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            btnOpenExternal.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    new AuthService().Log("Saved File - Opened in External Viewer",
                        $"'{record.FileName}{record.FileExt}' ({record.RecordType}) opened in the external/system PDF viewer from the in-app review window");
                }
                catch (Exception ex)
                {
                    new AuthService().Log("Saved File - Open in External Viewer Failed",
                        $"Could not open '{record.FileName}{record.FileExt}' ({record.RecordType}) in an external viewer | Error: {ex.Message}");

                    MessageBox.Show("Could not open this file:\n" + ex.Message, "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            DockPanel.SetDock(btnBack, Dock.Right);
            DockPanel.SetDock(btnOpenExternal, Dock.Right);
            toolbarPanel.Children.Add(btnBack);
            toolbarPanel.Children.Add(btnOpenExternal);
            toolbarPanel.Children.Add(titleText);
            toolbar.Child = toolbarPanel;

            // WebView2 (Edge/Chromium) renders the PDF inline with its built-in viewer. The old
            // WPF WebBrowser control is Internet Explorer, which can't render PDFs itself and
            // instead popped up an "Open / Save / Cancel" File Download prompt.
            var webView = new Microsoft.Web.WebView2.Wpf.WebView2
            {
                CreationProperties = new Microsoft.Web.WebView2.Wpf.CoreWebView2CreationProperties
                {
                    // Default user-data folder is next to the exe, which isn't writable under
                    // Program Files — keep it in the per-user app data folder instead.
                    UserDataFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ChromExplorer", "WebView2")
                }
            };

            var host = new Grid();
            host.Children.Add(webView);

            webView.Loaded += async (s, e) =>
            {
                try
                {
                    await webView.EnsureCoreWebView2Async();
                    webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    webView.CoreWebView2.Navigate(new Uri(path).AbsoluteUri);
                }
                catch (Exception ex)
                {
                    // Typically the WebView2 Runtime is missing on this machine. The "Open in
                    // External Viewer" button above still works, but log it and say so, since a
                    // reviewer silently seeing a blank PDF pane needs a trace.
                    new AuthService().Log("Saved File - In-App PDF Render Failed",
                        $"Embedded PDF viewer could not render '{record.FileName}{record.FileExt}' ({record.RecordType}) | Error: {ex.Message}");

                    host.Children.Clear();
                    host.Children.Add(new TextBlock
                    {
                        Text = "This report could not be displayed inside the application.\n" +
                               "Use \"Open in External Viewer\" above to view it.\n\n" + ex.Message,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(20),
                        Foreground = System.Windows.Media.Brushes.DimGray
                    });
                }
            };

            var body = new DockPanel();
            DockPanel.SetDock(toolbar, Dock.Top);
            body.Children.Add(toolbar);
            body.Children.Add(host);

            // Releases the embedded viewer's hold on the PDF file right before a Reviewer signs
            // it — disposing WebView2 shuts down its browser process and the file handle it
            // opened to render the PDF, which would otherwise block SignPdfFile's write. The
            // dialog closes right after signing, so the viewer isn't needed any more.
            Action releasePdfFileLock = () =>
            {
                try
                {
                    host.Children.Clear();
                    webView.Dispose();
                    Dispatcher.Invoke(new Action(() => { }), System.Windows.Threading.DispatcherPriority.Background);
                }
                catch { }
            };

            dialog.Closed += (s, e) =>
            {
                try { webView.Dispose(); } catch { }
            };

            ShowReviewableDialog(dialog, body, record, allowReview, beforeSign: releasePdfFileLock);
        }

        // A Data Processing Method (.cmethod) as a reviewer-friendly summary: only the settings the
        // user actually chose in Create Method, with readable names — no internal keys, creation
        // timestamp, or the settings of post-processing operations that were left switched off.
        private static void AppendMethodSummaryRows(System.Xml.Linq.XElement root, System.Data.DataTable table)
        {
            Func<System.Xml.Linq.XElement, string, string> val = (parent, name) =>
                ((string)parent?.Element(name) ?? "").Trim();
            Action<string, string> add = (setting, value) =>
            {
                var row = table.NewRow();
                row[0] = setting;
                row[1] = value;
                table.Rows.Add(row);
            };
            Func<string, string> pretty = s =>
                System.Text.RegularExpressions.Regex.Replace(s ?? "", "(?<=[a-z0-9])(?=[A-Z])", " ");
            Func<string, string> yesNo = s =>
                string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) ? "Yes" : "No";

            var pp = root.Element("PeakPicking");
            if (pp != null)
            {
                add("Peak Picking > Method", pretty(val(pp, "AlgorithmObjectKey")));
                add("Peak Picking > Threshold Type", val(pp, "ThresholdType"));
                add("Peak Picking > Threshold", val(pp, "Threshold"));
                add("Peak Picking > Min Peak Half Width (pt)", val(pp, "MinPeakHalfSize"));
                add("Peak Picking > Peak Half Width (pt)", val(pp, "PeakHalfSize"));
                add("Peak Picking > Analyzer", val(pp, "Analyzer"));
                add("Peak Picking > Label Format", val(pp, "LabelFormat"));
                add("Peak Picking > Label Orientation", val(pp, "LabelOrientation"));
                add("Peak Picking > Label Placement", val(pp, "LabelPlacement"));
            }

            var activeOps = (root.Element("PostProcessingOperations")?.Elements() ?? Enumerable.Empty<System.Xml.Linq.XElement>())
                .Where(op => string.Equals(val(op, "IsActive"), "true", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (activeOps.Count == 0)
            {
                add("Post Processing", "None applied");
            }
            foreach (var op in activeOps)
            {
                var opName = pretty(val(op, "ObjectKey"));
                add("Post Processing > " + opName, "Applied");
                foreach (var p in op.Element("Parameters")?.Elements() ?? Enumerable.Empty<System.Xml.Linq.XElement>())
                    add("Post Processing > " + opName + " > " + pretty(val(p, "Name")), val(p, "Value"));
            }

            var pi = root.Element("PeakIntegration");
            if (pi != null)
            {
                add("Peak Integration > Start Time", val(pi, "StartTime"));
                add("Peak Integration > End Time", val(pi, "EndTime"));
            }

            var snr = root.Element("SNR");
            if (snr != null)
            {
                var auto = string.Equals(val(snr, "IsAutomaticDetection"), "true", StringComparison.OrdinalIgnoreCase);
                add("SNR > Signal Definition", val(snr, "SignalDefinition"));
                add("SNR > Noise Definition", val(snr, "NoiseDefinition"));
                add("SNR > Multiplier", val(snr, "Multiplier"));
                add("SNR > Automatic Noise Detection", yesNo(val(snr, "IsAutomaticDetection")));
                if (auto)
                {
                    add("SNR > Desired Length", val(snr, "DesiredLength"));
                    add("SNR > Minimum Length", val(snr, "MinimumLength"));
                }
                else if (val(snr, "SpecificTimeRange").Length > 0)
                {
                    add("SNR > Noise Time Range", val(snr, "SpecificTimeRange"));
                }

                double ratio;
                if (double.TryParse(val(snr, "SNRatio"), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out ratio) && ratio != 0)
                {
                    add("SNR > Signal Value", val(snr, "SignalValue"));
                    add("SNR > Noise Value", val(snr, "NoiseValue"));
                    add("SNR > S/N Ratio", val(snr, "SNRatio"));
                }
            }

            var nist = root.Element("NistLibrary");
            if (nist != null)
            {
                if (val(nist, "NistExecutablePath").Length > 0)
                    add("NIST Library > Executable Path", val(nist, "NistExecutablePath"));
                if (val(nist, "FirstLocatorFile").Length > 0)
                    add("NIST Library > First Locator File", val(nist, "FirstLocatorFile"));
                if (val(nist, "SecondLocatorFile").Length > 0)
                    add("NIST Library > Second Locator File", val(nist, "SecondLocatorFile"));
                if (val(nist, "WorkingDir").Length > 0)
                    add("NIST Library > Working Directory", val(nist, "WorkingDir"));
            }
        }

        // Walks the XML tree collecting only the LEAF elements (the ones that actually hold a
        // value, not the containers like <IonSourceConfiguration> or <Channels>) into flat
        // "Setting / Value" rows. Nested elements get a "Parent > Child" style setting name so
        // it's still clear which group a value came from (e.g. "Channels > IonSourceChannel >
        // Name").
        private static void AppendXmlLeafValues(System.Xml.Linq.XElement element, System.Data.DataTable table, string pathPrefix)
        {
            foreach (var child in element.Elements())
            {
                var path = string.IsNullOrEmpty(pathPrefix) ? child.Name.LocalName : $"{pathPrefix} > {child.Name.LocalName}";

                if (child.HasElements)
                {
                    AppendXmlLeafValues(child, table, path);
                }
                else
                {
                    var row = table.NewRow();
                    row[0] = path;
                    row[1] = child.Value;
                    table.Rows.Add(row);
                }
            }
        }

        // A signed Method File carries its signature as the last XML comment inside the root
        // (see TrySignXmlFileAtBottom) — pulls that out into its own banner and removes the
        // comment from the document so it doesn't show up as a stray row.
        private static FrameworkElement ExtractXmlSignatureBanner(System.Xml.Linq.XDocument doc)
        {
            var comment = doc.Root?.Nodes()
                .OfType<System.Xml.Linq.XComment>()
                .FirstOrDefault(c => c.Value.Contains("ELECTRONIC SIGNATURE"));

            if (comment == null)
                return null;

            var text = comment.Value.Trim();
            comment.Remove();

            // " ELECTRONIC SIGNATURE | Reviewed By: x | Role: y | Verdict: v | Reviewed Date: d | Remark: r "
            var fields = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in text.Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = part.IndexOf(':');
                if (colon > 0)
                    fields[part.Substring(0, colon).Trim()] = part.Substring(colon + 1).Trim();
            }

            return BuildSignatureBanner(fields);
        }

        // Shared shell for the two in-app viewers: an optional signature banner docked at the TOP
        // (when the file already carries one), the file's content filling the rest of the window,
        // and — only when opened via "Review" (allowReview) and the current user is actually
        // eligible to review this exact record — the Reviewer's remark/verdict panel docked at the
        // bottom, so seeing the data and reviewing it happen in one place instead of two separate
        // popups. Opened via plain "View", it's just the data (plus the banner, if signed).
        private void ShowReviewableDialog(Window dialog, UIElement content, SavedFileRecord record, bool allowReview, FrameworkElement signatureBanner = null, Action beforeSign = null)
        {
            var reviewPanel = allowReview ? BuildReviewPanel(record, dialog, beforeSign) : null;

            // Not reviewed yet: no signature to show — just say so, nothing else.
            if (signatureBanner == null && record != null && !record.IsReviewed)
            {
                signatureBanner = new Border
                {
                    Background = System.Windows.Media.Brushes.Cornsilk,
                    BorderBrush = System.Windows.Media.Brushes.DarkOrange,
                    BorderThickness = new Thickness(0, 0, 0, 2),
                    Padding = new Thickness(14, 10, 14, 10),
                    Child = new TextBlock
                    {
                        Text = "Not reviewed yet",
                        FontWeight = FontWeights.SemiBold,
                        Foreground = System.Windows.Media.Brushes.DarkOrange
                    }
                };
            }

            if (reviewPanel == null && signatureBanner == null)
            {
                dialog.Content = content;
                dialog.ShowDialog();
                return;
            }

            var dock = new DockPanel();
            if (signatureBanner != null)
            {
                DockPanel.SetDock(signatureBanner, Dock.Top);
                dock.Children.Add(signatureBanner);
            }
            if (reviewPanel != null)
            {
                DockPanel.SetDock(reviewPanel, Dock.Bottom);
                dock.Children.Add(reviewPanel);
            }
            dock.Children.Add(content);
            dialog.Content = dock;
            dialog.ShowDialog();
        }

        // Builds the Reviewer's remark + Approve/Reject panel for a record — or null if the
        // current user isn't eligible to review it right now (not a Reviewer, already reviewed,
        // or — for a Method File — not yet submitted by the analyst). Approve/Reject both
        // require a remark; the only difference is which verdict gets recorded and signed.
        private FrameworkElement BuildReviewPanel(SavedFileRecord record, Window dialog, Action beforeSign = null)
        {
            var auth = new AuthService();
            if (!auth.IsCurrentUserReviewer())
                return null;

            if (record.IsReviewed)
            {
                var fields = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["Reviewed By"] = record.ReviewedByUsername,
                    ["Role"] = record.ReviewedByRole,
                    ["Verdict"] = record.ReviewVerdict,
                    ["Reviewed Date"] = record.ReviewedDate?.ToString(),
                    ["Remark"] = record.ReviewerRemark
                };
                return BuildSignatureBanner(fields);
            }

            if (record.IsMethodFile && record.ReviewStatus != SavedFileRegistry.StatusPendingReview)
                return null;

            var panel = new Border
            {
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(12)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock
            {
                Text = "Reviewer remark:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(label, 0);

            var txtRemark = new TextBox
            {
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(txtRemark, 1);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetRow(btnPanel, 2);

            Action<string> submit = verdict =>
            {
                if (string.IsNullOrWhiteSpace(txtRemark.Text))
                {
                    MessageBox.Show("Remark cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var remark = txtRemark.Text.Trim();

                // The remark itself is the reason — password confirms identity right before the
                // signature is finalized, same gate every other state-changing action here uses.
                if (!ConfirmCurrentUserPassword($"Saved File - {verdict}", remark))
                    return;

                int userId = 0;
                if (Application.Current?.Properties?["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                string fullName = auth.GetCurrentUserFullName();
                string role = auth.GetCurrentUserRoleName();

                SavedFileRegistry.SubmitReview(record.SavedFileId, remark, verdict, userId, fullName, role);

                // For a PDF viewed through the embedded in-app viewer, the WebBrowser control is
                // still holding the file open right now — release it first, or the write below
                // fails with "being used by another process" (SignPdfFile also retries on its
                // own as a safety net, but releasing the handle here avoids relying on that).
                beforeSign?.Invoke();

                ApplyElectronicSignatureToFile(record, fullName, role, remark, verdict);

                auth.Log("Saved File - Reviewed",
                    $"Saved file record '{record.FileName}{record.FileExt}' ({record.RecordType}) electronically signed by '{fullName}' ({role}) | Verdict: {verdict} | Remark: {remark}");

                LoadData();
                dialog.Close();
            };

            var btnApprove = new Button
            {
                Content = "Approve & Sign",
                Width = 130,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 0),
                Background = System.Windows.Media.Brushes.SeaGreen,
                Foreground = System.Windows.Media.Brushes.White
            };
            btnApprove.Click += (s, e) => submit(SavedFileRegistry.VerdictApproved);

            var btnReject = new Button
            {
                Content = "Reject & Sign",
                Width = 130,
                Height = 30,
                Background = System.Windows.Media.Brushes.IndianRed,
                Foreground = System.Windows.Media.Brushes.White
            };
            btnReject.Click += (s, e) => submit(SavedFileRegistry.VerdictRejected);

            btnPanel.Children.Add(btnApprove);
            btnPanel.Children.Add(btnReject);

            grid.Children.Add(label);
            grid.Children.Add(txtRemark);
            grid.Children.Add(btnPanel);
            panel.Child = grid;
            return panel;
        }

        // Only the Reviewer role gets asked for a remark — matches the same Reviewer-only gate
        // Edit/Deactivate already uses. Anyone else's View just opens the file, nothing more.
        // Once a file has been reviewed and electronically signed, that's final — it is never
        // re-offered for review again, on any category, so the signature can't be silently
        // overwritten (or, for a .csv, a second "ELECTRONIC SIGNATURE" block appended under the
        // first one) by opening it a second time. A Method File specifically also has to have
        // cleared stage 1 first (the analyst's own "Submit for Review") — the Reviewer shouldn't
        // be signing off on something that was never handed to them.
        private void OfferReviewRemark(SavedFileRecord record)
        {
            var auth = new AuthService();
            if (!auth.IsCurrentUserReviewer())
                return;

            if (record.IsReviewed)
            {
                MessageBox.Show(
                    $"This file was already reviewed and electronically signed by {record.ReviewedByUsername} ({record.ReviewedByRole}) on {record.ReviewedDate}.\n\nIt cannot be reviewed again.",
                    "Already Reviewed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (record.IsMethodFile && record.ReviewStatus != SavedFileRegistry.StatusPendingReview)
                return;

            var (remark, verdict) = ShowRemarkDialog(record);
            if (remark == null) return;

            if (!ConfirmCurrentUserPassword($"Saved File - {verdict}", remark))
                return;

            int userId = 0;
            if (Application.Current?.Properties?["UserId"] != null)
                userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

            string fullName = auth.GetCurrentUserFullName();
            string role = auth.GetCurrentUserRoleName();

            SavedFileRegistry.SubmitReview(record.SavedFileId, remark, verdict, userId, fullName, role);

            ApplyElectronicSignatureToFile(record, fullName, role, remark, verdict);

            auth.Log("Saved File - Reviewed",
                $"Saved file record '{record.FileName}{record.FileExt}' ({record.RecordType}) electronically signed by '{fullName}' ({role}) | Verdict: {verdict} | Remark: {remark}");

            LoadData();
        }

        // The electronic signature (logged-in username + their role, at this exact moment) is
        // written into the file itself, not just the FileM row — a reviewer looking at the file
        // later, outside this app, should still see who signed off on it and in what capacity.
        // A .csv gets the signature appended as extra rows; a .pdf gets it stamped onto the last
        // page. A Sample Analysis (.mtd) file is a proprietary binary format — editing it in
        // place isn't safe, so its signature goes into a sidecar "<file>.signature.txt" instead.
        private void ApplyElectronicSignatureToFile(SavedFileRecord record, string username, string role, string remark, string verdict)
        {
            var path = record.FullPath;
            var signedDate = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            try
            {
                var attrs = File.Exists(path) ? File.GetAttributes(path) : (FileAttributes?)null;
                bool wasReadOnly = attrs.HasValue && (attrs.Value & FileAttributes.ReadOnly) != 0;

                if (wasReadOnly)
                    File.SetAttributes(path, attrs.Value & ~FileAttributes.ReadOnly);

                var ext = (record.FileExt ?? "").ToLowerInvariant();

                if (ext == ".csv")
                {
                    // Only the CSV signature goes at the top — everything else goes at the bottom.
                    SignCsvFile(path, username, role, signedDate, remark, verdict);
                }
                else if (ext == ".pdf")
                {
                    SignPdfFile(path, username, role, signedDate, remark, verdict);
                }
                else if (ext == ".mtm2" || ext == ".cmethod" || ext == ".xml")
                {
                    // These are plain XML — safe to embed the signature directly in the file
                    // (as the last child of the root element, so the XML stays valid) instead of
                    // a separate sidecar file.
                    if (!TrySignXmlFileAtBottom(path, username, role, signedDate, remark, verdict))
                        SignViaSidecarFile(path, username, role, signedDate, remark, verdict);
                }
                else
                {
                    // A Sample Analysis (.mtd) file is a proprietary binary format — editing it in
                    // place isn't safe, so its signature goes into a sidecar file instead.
                    SignViaSidecarFile(path, username, role, signedDate, remark, verdict);
                }

                if (wasReadOnly || ext == ".csv" || ext == ".pdf")
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
            }
            catch (Exception ex)
            {
                // The DB review record is already saved at this point — a signature-stamping
                // failure (locked file, moved file, etc.) shouldn't look like the whole review
                // failed, just flag it separately. Still gets its own audit trail either way.
                new AuthService().Log("Saved File - Signature Not Written",
                    $"Review was recorded for '{record.FileName}{record.FileExt}' ({record.RecordType}), but the electronic signature could not be written into the file | Error: {ex.Message}");

                MessageBox.Show(
                    $"The review was recorded, but the electronic signature could not be written into the file itself:\n{ex.Message}",
                    "Signature Not Written", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // The signature goes at the TOP of the file — like a letterhead/email signature block —
        // not buried at the bottom where it's easy to miss when the file is opened in Excel.
        private static void SignCsvFile(string path, string username, string role, string signedDate, string remark, string verdict)
        {
            var signatureLines = new[]
            {
                "ELECTRONIC SIGNATURE",
                $"Reviewed By,{username}",
                $"Role,{role}",
                $"Verdict,{verdict}",
                $"Reviewed Date,{signedDate}",
                $"Remark,{remark.Replace(",", ";")}",
                ""
            };

            var existingLines = File.Exists(path) ? File.ReadAllLines(path) : new string[0];
            var combined = new System.Collections.Generic.List<string>(signatureLines);
            combined.AddRange(existingLines);

            File.WriteAllLines(path, combined);
        }

        // Stamped at the BOTTOM of the LAST page. Retries a few times with a short wait — the
        // embedded in-app PDF viewer's WebBrowser control is asked to release its hold on this
        // same file right before this runs (see ShowPdfViewer's releasePdfFileLock), but that
        // release is asynchronous, so the OS may not have actually freed the file handle by the
        // time this first attempt runs.
        private static void SignPdfFile(string path, string username, string role, string signedDate, string remark, string verdict)
        {
            const int maxAttempts = 6;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using (var document = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify))
                    {
                        var page = document.Pages[document.PageCount - 1];
                        using (var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page))
                        {
                            var font = new PdfSharp.Drawing.XFont("Arial", 8, PdfSharp.Drawing.XFontStyle.Bold);
                            bool rejected = string.Equals(verdict, SavedFileRegistry.VerdictRejected, StringComparison.OrdinalIgnoreCase);
                            var brush = rejected ? PdfSharp.Drawing.XBrushes.Firebrick : PdfSharp.Drawing.XBrushes.DarkGreen;

                            var text = FormatSignatureLine(username, role, verdict, signedDate, remark);
                            var y = page.Height.Point - 26;

                            gfx.DrawRectangle(rejected ? PdfSharp.Drawing.XBrushes.MistyRose : PdfSharp.Drawing.XBrushes.Honeydew, new PdfSharp.Drawing.XRect(0, y, page.Width.Point, 26));
                            gfx.DrawString(text, font, brush,
                                new PdfSharp.Drawing.XRect(10, y + 4, page.Width.Point - 20, 20),
                                PdfSharp.Drawing.XStringFormats.CenterLeft);
                        }

                        document.Save(path);
                    }

                    return;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    System.Threading.Thread.Sleep(300);
                }
            }
        }

        // Adds the signature as an XML comment — the LAST child of the root element — so it
        // visually sits at the bottom of the file (right before the closing tag) while keeping
        // the document strictly valid XML (a trailing comment after the root's closing tag is
        // not actually legal XML, so it has to live inside the root, not after it). Returns false
        // if the file isn't well-formed XML, so the caller can fall back to a sidecar file.
        private static bool TrySignXmlFileAtBottom(string path, string username, string role, string signedDate, string remark, string verdict)
        {
            try
            {
                var doc = System.Xml.Linq.XDocument.Load(path);
                if (doc.Root == null)
                    return false;

                var commentText =
                    $" ELECTRONIC SIGNATURE | Reviewed By: {username} | Role: {role} | Verdict: {verdict} | Reviewed Date: {signedDate} | Remark: {remark} ";

                doc.Root.Add(new System.Xml.Linq.XComment(commentText));
                doc.Save(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SignViaSidecarFile(string path, string username, string role, string signedDate, string remark, string verdict)
        {
            var lines = new[]
            {
                "ELECTRONIC SIGNATURE",
                $"File: {Path.GetFileName(path)}",
                $"Reviewed By: {username}",
                $"Role: {role}",
                $"Verdict: {verdict}",
                $"Reviewed Date: {signedDate}",
                $"Remark: {remark}"
            };

            File.WriteAllLines(path + ".signature.txt", lines);
        }

        private void OpenInChromatogramViewer(string path)
        {
            try
            {
                var provider = ChromDataProviderFactory.GetProvider<ChromExplorer.Modules.ModasChromProvider.Services.ModasChromProvider>();
                provider.Load(path);

                CloseView();
                RegionManager.RequestNavigate(RegionNames.MainContentArea, new Uri("ChromatorgramView", UriKind.Relative));
            }
            catch (Exception ex)
            {
                new AuthService().Log("Saved File - Open Failed",
                    $"Could not open '{path}' in the chromatogram viewer | Error: {ex.Message}");

                MessageBox.Show($"Could not open this file:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleActive(SavedFileRecord record)
        {
            bool newActive = !record.IsActive;
            string actionContext = newActive ? "Saved File - Activate" : "Saved File - Deactivate";
            string reasonTitle = newActive ? "Reason for Activation" : "Reason for Deactivation";

            string reason = ShowReasonDialog(reasonTitle);
            if (reason == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to {(newActive ? "activate" : "deactivate")} this record?\n\n{record.FileName}{record.FileExt}\n\nReason: {reason}",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            if (!ConfirmCurrentUserPassword(actionContext, reason)) return;

            // Correct password just proves who they are — editing/deactivating a record is a
            // further, separate check: only the Reviewer role is allowed to do it at all.
            if (!new AuthService().IsCurrentUserReviewer())
            {
                new AuthService().Log($"{actionContext} - Access Denied",
                    $"User is not in the Reviewer role | Reason given: {reason}");

                MessageBox.Show("Only the Reviewer role can edit or deactivate saved file records.",
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int userId = 0;
            if (Application.Current?.Properties?["UserId"] != null)
                userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

            SavedFileRegistry.SetActive(record.SavedFileId, newActive, userId);

            new AuthService().Log(actionContext,
                $"Saved file record '{record.FileName}{record.FileExt}' ({record.RecordType}) set to {(newActive ? "Active" : "Inactive")} | Reason: {reason}");

            LoadData();
        }

        // Approve & Sign / Reject & Sign / Skip — Skip and the window-close [X] both return a
        // null remark (nothing recorded, flag stays as-is); either signing button requires
        // actual remark text so the review is meaningful, and records which verdict was given.
        private (string remark, string verdict) ShowRemarkDialog(SavedFileRecord record)
        {
            var dialog = new Window
            {
                Title = "Reviewer Remark",
                Width = 460,
                Height = 270,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock
            {
                Text = $"Reviewer remark for: {record.FileName}{record.FileExt}",
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(label, 0);

            var hint = new TextBlock
            {
                Text = "Approve or Reject marks this file as Reviewed and electronically signs it. Skip leaves it as-is.",
                Foreground = System.Windows.Media.Brushes.Gray,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(hint, 1);

            var txtRemark = new TextBox
            {
                Height = 90,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                Padding = new Thickness(6)
            };
            Grid.SetRow(txtRemark, 2);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };
            Grid.SetRow(btnPanel, 3);

            string resultRemark = null;
            string resultVerdict = null;

            Action<string> chooseVerdict = verdict =>
            {
                if (string.IsNullOrWhiteSpace(txtRemark.Text))
                {
                    MessageBox.Show("Remark cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                resultRemark = txtRemark.Text.Trim();
                resultVerdict = verdict;
                dialog.DialogResult = true;
            };

            var btnApprove = new Button { Content = "Approve & Sign", Width = 120, Height = 30, Margin = new Thickness(0, 0, 8, 0), Background = System.Windows.Media.Brushes.SeaGreen, Foreground = System.Windows.Media.Brushes.White };
            btnApprove.Click += (s, ev) => chooseVerdict(SavedFileRegistry.VerdictApproved);

            var btnReject = new Button { Content = "Reject & Sign", Width = 120, Height = 30, Margin = new Thickness(0, 0, 8, 0), Background = System.Windows.Media.Brushes.IndianRed, Foreground = System.Windows.Media.Brushes.White };
            btnReject.Click += (s, ev) => chooseVerdict(SavedFileRegistry.VerdictRejected);

            var btnSkip = new Button { Content = "Skip", Width = 70, Height = 30 };
            btnSkip.Click += (s, ev) => { dialog.DialogResult = false; };

            btnPanel.Children.Add(btnApprove);
            btnPanel.Children.Add(btnReject);
            btnPanel.Children.Add(btnSkip);

            grid.Children.Add(label);
            grid.Children.Add(hint);
            grid.Children.Add(txtRemark);
            grid.Children.Add(btnPanel);
            dialog.Content = grid;

            bool? ok = dialog.ShowDialog();
            return ok == true ? (resultRemark, resultVerdict) : (null, null);
        }

        private string ShowReasonDialog(string title)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 420,
                Height = 210,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock
            {
                Text = "Please enter a reason:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(label, 0);

            var txtReason = new TextBox
            {
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                Padding = new Thickness(6)
            };
            Grid.SetRow(txtReason, 1);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };
            Grid.SetRow(btnPanel, 2);

            string result = null;

            var btnOk = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0), Background = System.Windows.Media.Brushes.SteelBlue, Foreground = System.Windows.Media.Brushes.White };
            btnOk.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtReason.Text))
                {
                    MessageBox.Show("Reason cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = txtReason.Text.Trim();
                dialog.DialogResult = true;
            };

            var btnCancel = new Button { Content = "Cancel", Width = 80, Height = 30 };
            btnCancel.Click += (s, ev) => { dialog.DialogResult = false; };

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);

            grid.Children.Add(label);
            grid.Children.Add(txtReason);
            grid.Children.Add(btnPanel);
            dialog.Content = grid;

            bool? ok = dialog.ShowDialog();
            return ok == true ? result : null;
        }

        private bool ConfirmCurrentUserPassword(string actionContext, string reason)
        {
            var password = ShowPasswordDialog();
            if (password == null)
                return false;

            if (!new AuthService().VerifyCurrentUserPassword(password))
            {
                new AuthService().Log($"{actionContext} - Password Verification Failed",
                    $"Incorrect password entered | Reason: {reason}");

                MessageBox.Show("Incorrect password. Action cancelled.", "Authentication Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private string ShowPasswordDialog()
        {
            var dialog = new Window
            {
                Title = "Confirm Password",
                Width = 380,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var label = new TextBlock
            {
                Text = "Enter your password to confirm this action:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(label, 0);

            var (txtPassword, txtPasswordVisible) = AddPasswordFieldWithEyeToggle(grid, 1);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };
            Grid.SetRow(btnPanel, 2);

            string result = null;

            var btnOk = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0), Background = System.Windows.Media.Brushes.SteelBlue, Foreground = System.Windows.Media.Brushes.White };
            btnOk.Click += (s, ev) =>
            {
                var entered = GetPasswordFieldText(txtPassword, txtPasswordVisible);
                if (string.IsNullOrEmpty(entered))
                {
                    MessageBox.Show("Password cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = entered;
                dialog.DialogResult = true;
            };

            var btnCancel = new Button { Content = "Cancel", Width = 80, Height = 30 };
            btnCancel.Click += (s, ev) => { dialog.DialogResult = false; };

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);

            grid.Children.Add(label);
            grid.Children.Add(btnPanel);
            dialog.Content = grid;

            bool? ok = dialog.ShowDialog();
            return ok == true ? result : null;
        }

        // Shared "eye" toggle so a password dialog's field can be revealed as plain text — a
        // bordered PasswordBox + TextBox pair, with a 👁 ToggleButton swapping which one is
        // visible (and carrying the typed value across the swap).
        private (PasswordBox, TextBox) AddPasswordFieldWithEyeToggle(Grid parentGrid, int row)
        {
            var border = new Border
            {
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 0, 4, 0)
            };
            Grid.SetRow(border, row);

            var inner = new Grid();
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pb = new PasswordBox
            {
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                Height = 28,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(pb, 0);

            var tb = new TextBox
            {
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                Height = 28,
                VerticalContentAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            Grid.SetColumn(tb, 0);

            var btnToggle = new ToggleButton
            {
                Content = "👁",
                Width = 26,
                Height = 26,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            Grid.SetColumn(btnToggle, 1);
            btnToggle.Click += (s, ev) =>
            {
                if (tb.Visibility == Visibility.Visible)
                {
                    pb.Password = tb.Text;
                    pb.Visibility = Visibility.Visible;
                    tb.Visibility = Visibility.Collapsed;
                }
                else
                {
                    tb.Text = pb.Password;
                    tb.Visibility = Visibility.Visible;
                    pb.Visibility = Visibility.Collapsed;
                }
            };

            inner.Children.Add(pb);
            inner.Children.Add(tb);
            inner.Children.Add(btnToggle);

            border.Child = inner;
            parentGrid.Children.Add(border);
            return (pb, tb);
        }

        private static string GetPasswordFieldText(PasswordBox pb, TextBox tb)
        {
            return tb.Visibility == Visibility.Visible ? tb.Text : pb.Password;
        }
    }
}
