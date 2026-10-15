using ChromExplorer.Modules.Auth.Services;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static ChromExplorer.Modules.Auth.Models.User;


namespace ChromExplorer.Modules.Auth.Views
{
    /// <summary>
    /// Interaction logic for AuditorReport.xaml
    /// </summary>
    /// 
    [Export("AuditorReport", typeof(AuditorReport))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class AuditorReport : UserControl
    {
        public AuditorReport()
        {
            InitializeComponent();
            BindGridView();
        }
        //private readonly string _connectionString = @"Data Source=D:\NewChromExplorer\ChromExplorer\source.db3;Version=3;";
        //        private readonly string _connectionString =
        //@"Data Source=D:\Chromatography 24 feb 2026\ChromExplorer\source.db3;
        //Version=3;
        //Journal Mode=WAL;
        //BusyTimeout=5000;
        //Pooling=True;";
        private int _selectedAuditId;
        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        [Import]
        public IRegionManager RegionManager { get; set; }

        private bool HasReportAccess()
        {
            if (!Application.Current.Properties.Contains("RoleId"))
            {
                MessageBox.Show("Session expired. Please login again.");
                return false;
            }

            int roleId = Convert.ToInt32(Application.Current.Properties["RoleId"]);
            bool hasRights = false;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
SELECT COUNT(*)
FROM role_wise_rights_details r
INNER JOIN menuM m ON r.menu_id = m.menu_id
WHERE r.role_id = @role_id
AND m.menu_name = @page_name
AND r.is_active = 1
AND m.is_active = 1";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@role_id", roleId);
                    cmd.Parameters.AddWithValue("@page_name", "AuditorExportPdf");

                    long count = (long)cmd.ExecuteScalar();
                    hasRights = count > 0;
                }
            }

            if (!hasRights)
            {
                MessageBox.Show("You are not authorized to generate this report.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            return hasRights;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            RegionManager.RequestNavigate("MainContentArea", "HomeScreenView");
        }
        private void BindGridView()
        {
            try
            {
                AuthService authService = new AuthService();
                List<ReportDO> listdata = authService.BindData();
                dgAudit.ItemsSource = listdata;
            }
            catch (Exception ex)
            {
                AuthService authService = new AuthService();
                authService.Log("AuditorReport", ex.Message);
            }
        }
        //private void AddRemark_Click(object sender, RoutedEventArgs e)
        //{
        //    Hyperlink link = sender as Hyperlink;

        //    _selectedAuditId = Convert.ToInt32(link.CommandParameter);

        //    txtRemark.Text = "";
        //    RemarkPopup.Visibility = Visibility.Visible;
        //}
        private void AddRemark_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                if (btn == null || btn.Tag == null)
                    return;
                _selectedAuditId = Convert.ToInt32(btn.Tag);
                txtRemark.Text = "";
                RemarkPopup.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                AuthService authService = new AuthService();
                authService.Log("AuditorReport", ex.Message);
            }

        }
        private async void SaveRemark_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRemark.Text))
                return;

            try
            {
                // VERY IMPORTANT: release DataGrid read lock
                dgAudit.ItemsSource = null;
                dgAudit.Items.Refresh();

                AuthService authService = new AuthService();

                bool result = authService.SaveAuditorRemark(_selectedAuditId, txtRemark.Text);

                if (!result)
                {
                    MessageBox.Show("Failed to save remark.");
                    return;
                }

                RemarkPopup.Visibility = Visibility.Collapsed;

                // Give SQLite time to release file
                await Task.Delay(300);

                // reload grid
                BindGridView();
            }
            catch (Exception ex)
            {
                AuthService authService = new AuthService();
                authService.Log("AuditorReport", ex.Message);
            }
        }
        private void CancelRemark_Click(object sender, RoutedEventArgs e)
        {
            RemarkPopup.Visibility = Visibility.Collapsed;
        }
        private void cmbFilterType_Changed(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;

            if (combo == null || combo.SelectedItem == null)
                return;

            string selected = (combo.SelectedItem as ComboBoxItem)?.Content?.ToString();

            // Hide all first
            txtFilter.Visibility = System.Windows.Visibility.Collapsed;
            dpFrom.Visibility = System.Windows.Visibility.Collapsed;
            dpTo.Visibility = System.Windows.Visibility.Collapsed;

            // Show based on filter type
            if (selected == "Date")
            {
                dpFrom.Visibility = System.Windows.Visibility.Visible;
                dpTo.Visibility = System.Windows.Visibility.Visible;
            }
            else if (selected != "Please Select")
            {
                txtFilter.Visibility = System.Windows.Visibility.Visible;
            }
        }
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            if (cmbFilterType.SelectedItem == null)
            {
                MessageBox.Show("Please select filter type");
                return;
            }

            ComboBoxItem item = cmbFilterType.SelectedItem as ComboBoxItem;
            if (item == null) return;

            string type = item.Content.ToString();

            AuthService service = new AuthService();

            if (type == "Date")
            {
                if (dpFrom.SelectedDate == null || dpTo.SelectedDate == null)
                {
                    MessageBox.Show("Please select date range");
                    return;
                }

                dgAudit.ItemsSource = service.FilterByDate(dpFrom.SelectedDate, dpTo.SelectedDate);
            }
            else
            {
                dgAudit.ItemsSource = service.FilterByText(type, txtFilter.Text ?? "");
            }
            btnExportExcel.IsEnabled = true;
        }


        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                cmbFilterType.SelectedIndex = 0;
                txtFilter.Text = "";
                dpFrom.SelectedDate = null;
                dpTo.SelectedDate = null;
                txtFilter.Visibility = Visibility.Collapsed;
                dpFrom.Visibility = Visibility.Collapsed;
                dpTo.Visibility = Visibility.Collapsed;

                BindGridView();

                btnExportExcel.IsEnabled = false;
            }
            catch (Exception ex)
            {
                AuthService authService = new AuthService();
                authService.Log("AuditorReport", ex.Message);
            }

        }
        //private void Button_ExportExcel_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        if (dgAudit.Items.Count == 0)
        //        {
        //            MessageBox.Show("No data available to export.");
        //            return;
        //        }

        //        string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmm");

        //        string filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        //                          + "\\AuditReport_" + timeStamp + ".csv";

        //        using (StreamWriter sw = new StreamWriter(filePath))
        //        {
        //            // Write headers
        //            for (int i = 0; i < dgAudit.Columns.Count; i++)
        //            {
        //                string header = dgAudit.Columns[i].Header.ToString();

        //                // Skip Add Remark column
        //                if (header == "Add Remark")
        //                    continue;

        //                sw.Write(header);

        //                if (i < dgAudit.Columns.Count - 1)
        //                    sw.Write(",");
        //            }

        //            sw.WriteLine();

        //            // Write rows
        //            foreach (var item in dgAudit.Items)
        //            {
        //                for (int i = 0; i < dgAudit.Columns.Count; i++)
        //                {
        //                    string header = dgAudit.Columns[i].Header.ToString();

        //                    // Skip Add Remark column
        //                    if (header == "Add Remark")
        //                        continue;

        //                    TextBlock txt = dgAudit.Columns[i].GetCellContent(item) as TextBlock;

        //                    if (txt != null)
        //                        sw.Write(txt.Text);

        //                    if (i < dgAudit.Columns.Count - 1)
        //                        sw.Write(",");
        //                }

        //                sw.WriteLine();
        //            }
        //        }

        //        MessageBox.Show("Excel exported successfully.\n\nYou can check the file here:\n" + filePath);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error exporting: " + ex.Message);
        //    }
        //}
        //private void Button_ExportExcel_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        if (dgAudit.Items.Count == 0)
        //        {
        //            MessageBox.Show("No data available to export.");
        //            return;
        //        }

        //        string filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\AuditReport.csv";

        //        using (StreamWriter sw = new StreamWriter(filePath))
        //        {
        //            // Write headers
        //            for (int i = 0; i < dgAudit.Columns.Count; i++)
        //            {
        //                sw.Write(dgAudit.Columns[i].Header);

        //                if (i < dgAudit.Columns.Count - 1)
        //                    sw.Write(",");
        //            }

        //            sw.WriteLine();

        //            // Write rows
        //            foreach (var item in dgAudit.Items)
        //            {
        //                for (int i = 0; i < dgAudit.Columns.Count; i++)
        //                {
        //                    TextBlock txt = dgAudit.Columns[i].GetCellContent(item) as TextBlock;

        //                    if (txt != null)
        //                        sw.Write(txt.Text);

        //                    if (i < dgAudit.Columns.Count - 1)
        //                        sw.Write(",");
        //                }

        //                sw.WriteLine();
        //            }
        //        }

        //        MessageBox.Show("Excel exported successfully to Desktop.");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error exporting: " + ex.Message);
        //    }
        //}

        private void dgAudit_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true; // block edit mode
        }

        private void dgAudit_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true; // block typing in grid
        }

        private void Button_ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!HasReportAccess())
                    return;

                if (dgAudit.ItemsSource == null)
                {
                    MessageBox.Show("No data available to export.");
                    return;
                }

                // Gather the visible bound columns (skip the hidden "Add Remark" action column),
                // using each column's live ActualWidth as its relative weight in the PDF layout.
                var exportColumns = new List<(string Header, string PropertyName, string StringFormat, double Width)>();
                foreach (var column in dgAudit.Columns)
                {
                    string header = column.Header?.ToString();
                    if (string.IsNullOrEmpty(header) || header == "Add Remark")
                        continue;

                    var boundColumn = column as DataGridBoundColumn;
                    var binding = boundColumn?.Binding as Binding;
                    double width = column.ActualWidth > 0 ? column.ActualWidth : 100;

                    if (binding != null)
                    {
                        exportColumns.Add((header, binding.Path.Path, binding.StringFormat, width));
                    }
                    else if (header == "Parameters")
                    {
                        // Parameters is a DataGridTemplateColumn (its value is blanked out on screen
                        // only, for Login Success/Failed rows) so it has no Binding to reflect off —
                        // bind it directly to the underlying property, which is untouched.
                        exportColumns.Add((header, "Parameters", null, width));
                    }
                }

                var items = dgAudit.ItemsSource as IEnumerable;
                var rows = new List<string[]>();

                foreach (var item in items)
                {
                    var values = new string[exportColumns.Count];
                    for (int i = 0; i < exportColumns.Count; i++)
                    {
                        var col = exportColumns[i];
                        var prop = item.GetType().GetProperty(col.PropertyName);
                        var raw = prop?.GetValue(item, null);

                        string cell;
                        if (raw != null && !string.IsNullOrEmpty(col.StringFormat))
                        {
                            try { cell = string.Format("{0:" + col.StringFormat + "}", raw); }
                            catch { cell = raw.ToString(); }
                        }
                        else
                        {
                            cell = raw?.ToString() ?? "";
                        }

                        if (col.Header == "Parameters")
                        {
                            string action = item.GetType().GetProperty("Action")?.GetValue(item, null)?.ToString();
                            if (action == "Login Success" || action == "Login Failed" || action == "Login Blocked")
                                cell = "";
                        }

                        values[i] = cell;
                    }
                    rows.Add(values);
                }

                // Audit Report PDFs are only ever saved on the D: drive, same as every other
                // report the app writes (see ChromExplorer.Infrastructure.ReportStorage) — this
                // export has no fixed "kind" of its own there, so the folder is built directly
                // here instead of going through ReportStorage's ReportKind enum.
                string auditReportFolder = System.IO.Path.Combine(ChromExplorer.Infrastructure.ReportStorage.RootFolder, "AuditReport");
                try
                {
                    string root = System.IO.Path.GetPathRoot(auditReportFolder);
                    if (!string.IsNullOrEmpty(root) && !System.IO.Directory.Exists(root))
                        throw new System.IO.DirectoryNotFoundException($"Drive '{root}' is not available on this computer.");

                    System.IO.Directory.CreateDirectory(auditReportFolder);
                }
                catch (Exception driveEx)
                {
                    new AuthService().Log("Audit Trail Export Blocked",
                        $"Could not use audit report folder '{auditReportFolder}' | Error: {driveEx.Message}");

                    MessageBox.Show(
                        $"Audit reports are saved to:\n{auditReportFolder}\n\nThis folder could not be created or written to:\n{driveEx.Message}",
                        "Report Folder Unavailable", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
                string filePath = System.IO.Path.Combine(auditReportFolder, "AuditReport_" + timeStamp + ".pdf");

                var exporter = new AuditPdfExporter();
                exporter.Export(
                    "ChromExplorer Audit Report",
                    exportColumns.Select(c => c.Header).ToArray(),
                    exportColumns.Select(c => c.Width).ToArray(),
                    rows,
                    filePath);

                string exportedBy = Application.Current.Properties.Contains("Username")
                    ? Application.Current.Properties["Username"].ToString()
                    : "Unknown";

                new AuthService().Log(
                    "Audit Trail Exported",
                    $"Audit trail exported to PDF by '{exportedBy}' | File: {System.IO.Path.GetFileName(filePath)} | Path: {filePath} | Exported on: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");

                MessageBox.Show("PDF exported successfully.\n\nFile location:\n" + filePath);
            }
            catch (Exception ex)
            {
                AuthService authService = new AuthService();
                authService.Log("AuditorReport", ex.Message);
                MessageBox.Show("Export failed: " + ex.Message);
            }
        }

    }
}

