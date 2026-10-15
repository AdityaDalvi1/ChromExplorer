using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace ChromExplorer.Infrastructure
{
    // Shared "don't silently overwrite" gate for every Save dialog in the app (sequences, saved
    // methods, PDF/CSV reports, exported data files, locator files, etc.) — call right after the
    // user picks a location via SaveFileDialog, before actually writing anything.
    public static class FileSaveGuard
    {
        private const string ConnectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        // Returns true if it's OK to save to `path` (nothing there yet). Returns false and shows
        // a warning dialog — and records the blocked attempt in the audit log — if a file already
        // exists at that exact location. The caller must not write to it; the user has to pick a
        // different name/folder and try again.
        public static bool CanSaveTo(string path, string itemDescription = "file")
        {
            // Strict storage rule: nothing is ever written outside the app's storage folders.
            if (!string.IsNullOrWhiteSpace(path) && !FileStorage.IsInsideAnyAllowedFolder(path))
            {
                MessageBox.Show(
                    $"A {itemDescription} can only be saved inside the application's storage folders on the D: drive.\n\n{path}\n\nChoose a location inside the allowed folder, then try again.",
                    "Location Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);

                InsertAuditLog("Save Blocked - Outside Storage Folder",
                    $"Attempted to save a {itemDescription} outside the allowed storage folders: {path}", path);

                return false;
            }

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return true;

            MessageBox.Show(
                $"File with the same name already exists:\n{path}\n\nChoose a different file name. Overwriting is not allowed.",
                "File Already Exists", MessageBoxButton.OK, MessageBoxImage.Warning);

            InsertAuditLog("Save Blocked - File Already Exists",
                $"Attempted to save a {itemDescription} but a file already exists at this location: {path}", path);

            return false;
        }

        // Same "gate before the app writes anything" pattern as CanSaveTo, but for the tracked
        // file types (Sample Sequence, Data Processing Method — Report already enforces this via
        // ReportStorage) that must only ever be saved on the D: drive, same as reports. Call this
        // right after the user picks a location via SaveFileDialog, alongside CanSaveTo.
        public static bool IsOnAllowedDrive(string path, string itemDescription = "file", string allowedDrive = "D")
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            string root;
            try
            {
                root = Path.GetPathRoot(path);
            }
            catch
            {
                return true; // an unparsable path isn't this check's problem — let the actual save attempt surface that.
            }

            // Path.GetPathRoot returns e.g. "D:\" — compare just the drive letter.
            if (!string.IsNullOrEmpty(root) && root.Length > 0 &&
                char.ToUpperInvariant(root[0]) == char.ToUpperInvariant(allowedDrive[0]))
                return true;

            MessageBox.Show(
                $"A {itemDescription} can only be saved on the {allowedDrive}: drive:\n{path}\n\nChoose a location on {allowedDrive}: drive, then try again.",
                "Drive Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);

            InsertAuditLog("Save Blocked - Wrong Drive",
                $"Attempted to save a {itemDescription} outside the {allowedDrive}: drive: {path}", path);

            return false;
        }

        private static void InsertAuditLog(string action, string description, string path)
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
                              (action, description, file_name, is_active, inserted_by, inserted_date)
                              VALUES
                              (@action, @desc, @fileName, 1, @uid, datetime('now','localtime'))";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@desc", description);
                        cmd.Parameters.AddWithValue("@fileName", Path.GetFileName(path) ?? string.Empty);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Best-effort audit logging — a logging failure must never block/crash the save
                // flow itself (the user already saw the warning dialog either way).
            }
        }
    }
}
