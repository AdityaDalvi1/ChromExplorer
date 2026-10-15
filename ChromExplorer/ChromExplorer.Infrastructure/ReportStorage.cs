using System;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace ChromExplorer.Infrastructure
{
    public enum ReportKind
    {
        General,
        Method,
        Forensic
    }

    // Single fixed location for every PDF report the app writes (General / Method / Forensic,
    // plus the reviewed copies downloaded from Review Saved Files). Users can't choose another
    // folder — every report lands in <ReportFolder>\<Kind>\, created on demand.
    // The root comes from appSettings["ReportFolder"] and defaults to D:\ChromReport.
    public static class ReportStorage
    {
        public const string DefaultRootFolder = @"D:\ChromReport";

        public const string GeneralFilePrefix = "ChromExplorer_Report_";
        public const string MethodFilePrefix = "ChromExplorer_MethodReport_";
        public const string ForensicFilePrefix = "ChromExplorer_Forensic_Report_";

        public static string RootFolder
        {
            get
            {
                var configured = ConfigurationManager.AppSettings["ReportFolder"];
                return string.IsNullOrWhiteSpace(configured) ? DefaultRootFolder : configured.Trim();
            }
        }

        public static string GetFolder(ReportKind kind)
        {
            return Path.Combine(RootFolder, kind.ToString());
        }

        public static string GetFilePrefix(ReportKind kind)
        {
            switch (kind)
            {
                case ReportKind.Method: return MethodFilePrefix;
                case ReportKind.Forensic: return ForensicFilePrefix;
                default: return GeneralFilePrefix;
            }
        }

        // Works out which sub-folder an existing report belongs in from its file name — the
        // three exporters each use their own prefix, and Review Saved Files only knows the file
        // name/path of a record, not which report screen produced it.
        public static ReportKind GetKindFromFileName(string fileName)
        {
            var name = Path.GetFileName(fileName ?? "") ?? "";

            if (name.StartsWith(MethodFilePrefix, StringComparison.OrdinalIgnoreCase))
                return ReportKind.Method;
            if (name.StartsWith(ForensicFilePrefix, StringComparison.OrdinalIgnoreCase))
                return ReportKind.Forensic;
            return ReportKind.General;
        }

        // Creates <ReportFolder>\<Kind>\ if needed and returns it. Returns null — after telling
        // the user and writing an audit entry — when it can't be created or written to (e.g. the
        // machine has no D: drive), so the caller just stops instead of saving somewhere else.
        public static string EnsureFolder(ReportKind kind)
        {
            string folder = GetFolder(kind);

            try
            {
                string root = Path.GetPathRoot(folder);
                if (!string.IsNullOrEmpty(root) && !Directory.Exists(root))
                    throw new DirectoryNotFoundException($"Drive '{root}' is not available on this computer.");

                Directory.CreateDirectory(folder);

                // Confirm we can actually write there before the exporter tries to.
                string probe = Path.Combine(folder, ".write_test_" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(probe, "");
                File.Delete(probe);

                return folder;
            }
            catch (Exception ex)
            {
                InsertAuditLog("Report Folder Unavailable",
                    $"Could not use report folder '{folder}' for a {kind} report | Error: {ex.Message}");

                MessageBox.Show(
                    $"Reports are saved to:\n{folder}\n\nThis folder could not be created or written to:\n{ex.Message}\n\n" +
                    "Please ask your administrator to make this location available, then try again.",
                    "Report Folder Unavailable", MessageBoxButton.OK, MessageBoxImage.Error);

                return null;
            }
        }

        // Full path for a new timestamped report, e.g. D:\ChromReport\Method\
        // ChromExplorer_MethodReport_20260928_101500.pdf — or null if the folder isn't usable.
        public static string BuildNewReportPath(ReportKind kind)
        {
            string folder = EnsureFolder(kind);
            if (folder == null)
                return null;

            return Path.Combine(folder, GetFilePrefix(kind) + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
        }

        public static void ShowSavedMessage(string filePath, string title = "Report Saved", string extraText = null)
        {
            string message = "Your report was saved here:\n" + filePath;
            if (!string.IsNullOrEmpty(extraText))
                message += "\n\n" + extraText;

            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private const string ConnectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        private static void InsertAuditLog(string action, string description)
        {
            try
            {
                int userId = 0;
                if (Application.Current?.Properties?["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();

                    var sql = @"INSERT INTO AuditLogM
                              (action, description, is_active, inserted_by, inserted_date)
                              VALUES
                              (@action, @desc, 1, @uid, datetime('now','localtime'))";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@desc", description);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Best-effort — the user has already been shown the error either way.
            }
        }
    }
}
