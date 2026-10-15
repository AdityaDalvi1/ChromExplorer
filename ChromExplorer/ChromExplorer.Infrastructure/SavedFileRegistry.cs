using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace ChromExplorer.Infrastructure
{
    // Every file this app actually writes for the record (a saved Sample Sequence, a generated
    // Report, or a Sample Analysis acquisition file) gets one row here — this is the data source
    // for the Reviewer's "Review Saved Files" screen (ChromExplorer.Modules.Auth). Call Record()
    // right after a save genuinely succeeds (after FileSaveGuard.CanSaveTo already passed).
    public static class SavedFileRegistry
    {
        // Keep these in sync with the record_type values the Reviewer screen filters/displays by.
        public const string TypeSampleSequence = "Sample Sequence";
        public const string TypeReport = "Report";
        public const string TypeSampleAnalysis = "Sample Analysis";
        public const string TypeMethodFile = "Method File";
        public const string TypeDataProcessingMethod = "Data Processing Method";

        // The three-stage workflow Method File records go through (Sequence/Report/Sample
        // Analysis records don't use this — they're tracked purely via is_reviewed).
        public const string StatusNotSubmitted = "Not Submitted";
        public const string StatusPendingReview = "Pending Review";
        public const string StatusReviewed = "Reviewed";

        // The Reviewer's verdict on a file — a review isn't always a rubber stamp; the data can
        // genuinely be wrong, so the outcome itself has to be recorded, not just that a review
        // happened.
        public const string VerdictApproved = "Approved";
        public const string VerdictRejected = "Rejected";

        private const string ConnectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        // Called by the Reviewer screen before it queries FileM, so the table exists even
        // if no save has happened yet in this install.
        public static void EnsureTableExists()
        {
            try
            {
                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();
                    EnsureTable(con);
                }
            }
            catch
            {
            }
        }

        // Reviewer "Edit" action: toggle a record's Active/Inactive flag, stamping who/when.
        public static void SetActive(long fileId, bool isActive, int updatedByUserId)
        {
            try
            {
                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();
                    EnsureTable(con);

                    var sql = @"UPDATE FileM
                              SET is_active = @active, updated_by = @uid, updated_date = datetime('now','localtime')
                              WHERE file_id = @id";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@active", isActive ? 1 : 0);
                        cmd.Parameters.AddWithValue("@uid", updatedByUserId);
                        cmd.Parameters.AddWithValue("@id", fileId);
                        cmd.ExecuteNonQuery();
                    }
                }

                LogAudit(isActive ? "Saved File - Activate" : "Saved File - Deactivate",
                    $"FileM record #{fileId} set to {(isActive ? "Active" : "Inactive")} | Updated by user #{updatedByUserId}", updatedByUserId);
            }
            catch (Exception ex)
            {
                LogAudit(isActive ? "Saved File - Activate - Failed" : "Saved File - Deactivate - Failed",
                    $"FileM record #{fileId} could not be updated | Error: {ex.Message}", updatedByUserId);
            }
        }

        // Reviewer submits a remark on an opened file — stamps the reviewed flag plus an
        // electronic signature: who (username AND role, captured as plain text at the moment of
        // signing so the signature stays accurate even if that user's role changes later), when,
        // and the remark itself.
        public static void SubmitReview(long fileId, string remark, string verdict, int reviewedByUserId, string reviewedByUsername, string reviewedByRole)
        {
            try
            {
                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();
                    EnsureTable(con);

                    var sql = @"UPDATE FileM
                              SET is_reviewed = 1,
                                  review_status = @status,
                                  review_verdict = @verdict,
                                  reviewer_remark = @remark,
                                  reviewed_by = @uid,
                                  reviewed_by_username = @uname,
                                  reviewed_by_role = @urole,
                                  reviewed_date = datetime('now','localtime')
                              WHERE file_id = @id";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@status", StatusReviewed);
                        cmd.Parameters.AddWithValue("@verdict", verdict ?? VerdictApproved);
                        cmd.Parameters.AddWithValue("@remark", remark ?? "");
                        cmd.Parameters.AddWithValue("@uid", reviewedByUserId);
                        cmd.Parameters.AddWithValue("@uname", reviewedByUsername ?? "");
                        cmd.Parameters.AddWithValue("@urole", reviewedByRole ?? "");
                        cmd.Parameters.AddWithValue("@id", fileId);
                        cmd.ExecuteNonQuery();
                    }
                }

                LogAudit("Saved File - Reviewed",
                    $"FileM record #{fileId} reviewed — Verdict: {verdict} | Reviewed by: {reviewedByUsername} ({reviewedByRole}) | Remark: {remark}",
                    reviewedByUserId);
            }
            catch (Exception ex)
            {
                LogAudit("Saved File - Reviewed - Failed",
                    $"FileM record #{fileId} review could not be saved | Error: {ex.Message}", reviewedByUserId);
            }
        }

        // Stage 1 of the Method File workflow: "Submit by analyst for review" — the analyst who
        // used/selected the method file explicitly hands it off to the Reviewer.
        public static void SubmitForReview(long fileId, int submittedByUserId, string submittedByUsername)
        {
            try
            {
                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();
                    EnsureTable(con);

                    var sql = @"UPDATE FileM
                              SET review_status = @status,
                                  submitted_by = @uid,
                                  submitted_by_username = @uname,
                                  submitted_date = datetime('now','localtime')
                              WHERE file_id = @id";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@status", StatusPendingReview);
                        cmd.Parameters.AddWithValue("@uid", submittedByUserId);
                        cmd.Parameters.AddWithValue("@uname", submittedByUsername ?? "");
                        cmd.Parameters.AddWithValue("@id", fileId);
                        cmd.ExecuteNonQuery();
                    }
                }

                LogAudit("Method File - Submitted for Review",
                    $"FileM record #{fileId} submitted for review by {submittedByUsername}", submittedByUserId);
            }
            catch (Exception ex)
            {
                LogAudit("Method File - Submitted for Review - Failed",
                    $"FileM record #{fileId} could not be submitted for review | Error: {ex.Message}", submittedByUserId);
            }
        }

        // Stage 2 ("Intimation to reviewer") is read, not written, here — the Reviewer screen
        // calls this on load to tell the Reviewer how many Method Files are waiting on them.
        public static int GetPendingReviewCount(string recordType)
        {
            try
            {
                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();
                    EnsureTable(con);

                    var sql = @"SELECT COUNT(*) FROM FileM
                              WHERE record_type = @type AND review_status = @status AND is_active = 1";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@type", recordType ?? "");
                        cmd.Parameters.AddWithValue("@status", StatusPendingReview);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        // "Intimation to reviewer" for the non-Method-File categories (Sample Sequence, Report,
        // Sample Analysis), which don't have the analyst's explicit submit step — anything
        // active and not yet reviewed counts as waiting on the Reviewer.
        public static int GetUnreviewedCount(string recordType)
        {
            try
            {
                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();
                    EnsureTable(con);

                    var sql = @"SELECT COUNT(*) FROM FileM
                              WHERE record_type = @type AND is_reviewed = 0 AND is_active = 1";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@type", recordType ?? "");
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        // "Whoever created/submitted the file also gets a notification" — one row per file that
        // this exact user created (inserted_by) and that has since been reviewed but not yet
        // shown to them. The Reviewer screen calls this on load for whoever is logged in (not
        // just Reviewers — the creator could be any role), then marks each one notified so it
        // isn't shown again.
        public static System.Collections.Generic.List<(string fileName, string verdict, string reviewedBy)> GetUnnotifiedReviewsForCreator(int userId)
        {
            var result = new System.Collections.Generic.List<(string, string, string)>();

            try
            {
                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();
                    EnsureTable(con);

                    var sql = @"SELECT file_id, file_name, file_ext, review_verdict, reviewed_by_username
                              FROM FileM
                              WHERE inserted_by = @uid AND is_reviewed = 1 AND creator_notified = 0";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var name = reader["file_name"]?.ToString() + reader["file_ext"]?.ToString();
                                var verdict = reader["review_verdict"] == DBNull.Value ? "" : reader["review_verdict"].ToString();
                                var reviewedBy = reader["reviewed_by_username"] == DBNull.Value ? "" : reader["reviewed_by_username"].ToString();
                                result.Add((name, verdict, reviewedBy));
                            }
                        }
                    }

                    using (var cmd = new SQLiteCommand(
                        "UPDATE FileM SET creator_notified = 1 WHERE inserted_by = @uid AND is_reviewed = 1 AND creator_notified = 0", con))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
            }

            return result;
        }

        // Returns the new FileM row's file_id (0 if the insert didn't happen/failed) — callers
        // that need to immediately act on the row they just created (e.g. GenerateGeneralReport
        // calling SubmitForReview right after Record, so a Report goes straight into the Pending
        // Review queue instead of sitting there freely usable/unreviewed) use the return value;
        // every other existing call site just ignores it, unchanged.
        public static long Record(string recordType, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return 0;

            int userId = 0;
            if (Application.Current?.Properties?["UserId"] != null)
                userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

            try
            {
                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();
                    EnsureTable(con);

                    var sql = @"INSERT INTO FileM
                              (record_type, file_path, file_name, file_ext, is_active, inserted_by, inserted_date)
                              VALUES
                              (@type, @path, @name, @ext, 1, @uid, datetime('now','localtime'))";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@type", recordType ?? "");
                        cmd.Parameters.AddWithValue("@path", Path.GetDirectoryName(fullPath) ?? "");
                        cmd.Parameters.AddWithValue("@name", Path.GetFileNameWithoutExtension(fullPath) ?? "");
                        cmd.Parameters.AddWithValue("@ext", Path.GetExtension(fullPath) ?? "");
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }

                    LogAudit("Saved File - Recorded",
                        $"{recordType} file tracked for review: {Path.GetFileName(fullPath)}", userId);

                    return con.LastInsertRowId;
                }
            }
            catch (Exception ex)
            {
                // Best-effort — a Reviewer-tracking failure must never block the save itself,
                // which has already completed successfully by the time this is called — but it
                // still has to be visible somewhere, since that file is now invisible to the
                // Reviewer until someone notices and fixes it.
                LogAudit("Saved File - Recorded - Failed",
                    $"Could not track {recordType} file for review: {Path.GetFileName(fullPath)} | Error: {ex.Message}", userId);
                return 0;
            }
        }

        // The file each category was last opened/applied FROM (Data Processing Method applied to
        // the graph, ...) — so when the analyst later saves their edited version, the Save dialog
        // can propose a new "_revN" name next to it instead of the original's name.
        private static readonly System.Collections.Generic.Dictionary<string, string> OpenedFiles =
            new System.Collections.Generic.Dictionary<string, string>();

        public static void SetOpenedFile(string recordType, string fullPath)
        {
            OpenedFiles[recordType ?? ""] = fullPath;
        }

        public static string GetOpenedFile(string recordType)
        {
            return OpenedFiles.TryGetValue(recordType ?? "", out var p) ? p : null;
        }

        public static void ClearOpenedFile(string recordType)
        {
            OpenedFiles.Remove(recordType ?? "");
        }

        // "<name>_rev1.ext", "<name>_rev2.ext", ... — first one that doesn't exist yet, in the
        // same folder as the original.
        public static string SuggestRevisionPath(string fullPath)
        {
            string folder = Path.GetDirectoryName(fullPath) ?? "";
            string ext = Path.GetExtension(fullPath);
            string baseName = System.Text.RegularExpressions.Regex.Replace(Path.GetFileNameWithoutExtension(fullPath), @"_rev\d+$", "");
            int rev = 1;
            while (File.Exists(Path.Combine(folder, baseName + "_rev" + rev + ext)))
                rev++;
            return Path.Combine(folder, baseName + "_rev" + rev + ext);
        }

        // Same dialog for every category: who rejected it, and the reviewer's remark in RED.
        public static void ShowRejectedNotice(string fileName, string reviewedBy, string remark, string howToProceed)
        {
            var window = new Window
            {
                Title = "Rejected File",
                SizeToContent = SizeToContent.WidthAndHeight,
                MaxWidth = 560,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "✖ " + fileName + " was Rejected" + (string.IsNullOrWhiteSpace(reviewedBy) ? "" : " by " + reviewedBy),
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.Firebrick,
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Reviewer remark: " + (string.IsNullOrWhiteSpace(remark) ? "(none)" : remark),
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.Firebrick,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0)
            });
            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = howToProceed,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0)
            });
            var ok = new System.Windows.Controls.Button { Content = "OK", Width = 80, Margin = new Thickness(0, 16, 0, 0), HorizontalAlignment = HorizontalAlignment.Right, IsDefault = true };
            ok.Click += (s, e) => window.Close();
            panel.Children.Add(ok);
            window.Content = panel;
            window.ShowDialog();
        }

        public class FileReviewState
        {
            public bool Found { get; set; }
            public bool IsActive { get; set; }
            public bool IsReviewed { get; set; }
            public string Verdict { get; set; } = "";
            public string Remark { get; set; } = "";
            public string ReviewedBy { get; set; } = "";

            public bool IsApproved => Found && IsActive && IsReviewed
                && string.Equals(Verdict, VerdictApproved, StringComparison.OrdinalIgnoreCase);
            public bool IsRejected => Found && IsReviewed
                && string.Equals(Verdict, VerdictRejected, StringComparison.OrdinalIgnoreCase);
        }

        // Latest FileM row for this exact file (path + name + extension) of the given type — used
        // by the Sample Sequence tab to decide whether a sequence may be run (Approved only) and
        // to tell the analyst why a Rejected one was rejected.
        public static FileReviewState GetReviewState(string recordType, string fullPath)
        {
            var state = new FileReviewState();
            if (string.IsNullOrWhiteSpace(fullPath))
                return state;

            try
            {
                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();
                    EnsureTable(con);

                    var sql = @"SELECT is_active, is_reviewed, review_verdict, reviewer_remark, reviewed_by_username
                              FROM FileM
                              WHERE record_type = @type AND file_path = @path AND file_name = @name AND file_ext = @ext
                              ORDER BY file_id DESC LIMIT 1";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@type", recordType ?? "");
                        cmd.Parameters.AddWithValue("@path", Path.GetDirectoryName(fullPath) ?? "");
                        cmd.Parameters.AddWithValue("@name", Path.GetFileNameWithoutExtension(fullPath) ?? "");
                        cmd.Parameters.AddWithValue("@ext", Path.GetExtension(fullPath) ?? "");
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                state.Found = true;
                                state.IsActive = r["is_active"] != DBNull.Value && Convert.ToInt32(r["is_active"]) == 1;
                                state.IsReviewed = r["is_reviewed"] != DBNull.Value && Convert.ToInt32(r["is_reviewed"]) == 1;
                                state.Verdict = r["review_verdict"] == DBNull.Value ? "" : r["review_verdict"].ToString();
                                state.Remark = r["reviewer_remark"] == DBNull.Value ? "" : r["reviewer_remark"].ToString();
                                state.ReviewedBy = r["reviewed_by_username"] == DBNull.Value ? "" : r["reviewed_by_username"].ToString();
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return state;
        }

        // Public audit writer for the file-review workflow in other modules (Settings, Viewer,
        // General Settings). Never throws — logging must not break the action it records.
        public static void WriteAudit(string action, string description, string fileName = "")
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
                              (@action, @desc, @file, 1, @uid, datetime('now','localtime'))";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@desc", description);
                        cmd.Parameters.AddWithValue("@file", fileName ?? "");
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
            }
        }

        // Shared audit-log writer for every FileM write above — logs both success and failure, so
        // there's always a trail even when a step silently can't complete.
        private static void LogAudit(string action, string description, int userId)
        {
            try
            {
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
                // Truly best-effort — logging itself must never throw back into the caller.
            }
        }

        private static void EnsureTable(SQLiteConnection con)
        {
            var sql = @"CREATE TABLE IF NOT EXISTS FileM (
                        file_id         INTEGER PRIMARY KEY AUTOINCREMENT,
                        record_type     TEXT,
                        file_path       TEXT,
                        file_name       TEXT,
                        file_ext        TEXT,
                        is_active       INTEGER DEFAULT 1 CHECK(is_active IN (0, 1)),
                        inserted_by     INT DEFAULT NULL,
                        inserted_date   DATETIME DEFAULT CURRENT_TIMESTAMP,
                        updated_by      INT,
                        updated_date    DATETIME,
                        is_reviewed     INTEGER DEFAULT 0 CHECK(is_reviewed IN (0, 1)),
                        reviewer_remark TEXT,
                        reviewed_by     INT,
                        reviewed_by_username TEXT,
                        reviewed_by_role TEXT,
                        reviewed_date   DATETIME,
                        review_verdict  TEXT,
                        review_status   TEXT DEFAULT 'Not Submitted',
                        submitted_by    INT,
                        submitted_by_username TEXT,
                        submitted_date  DATETIME,
                        creator_notified INTEGER DEFAULT 0 CHECK(creator_notified IN (0, 1))
                    )";

            using (var cmd = new SQLiteCommand(sql, con))
                cmd.ExecuteNonQuery();

            AddColumnIfMissing(con, "is_reviewed", "INTEGER DEFAULT 0 CHECK(is_reviewed IN (0, 1))");
            AddColumnIfMissing(con, "reviewer_remark", "TEXT");
            AddColumnIfMissing(con, "reviewed_by", "INT");
            AddColumnIfMissing(con, "reviewed_by_username", "TEXT");
            AddColumnIfMissing(con, "reviewed_by_role", "TEXT");
            AddColumnIfMissing(con, "reviewed_date", "DATETIME");
            AddColumnIfMissing(con, "review_verdict", "TEXT");
            AddColumnIfMissing(con, "review_status", "TEXT DEFAULT 'Not Submitted'");
            AddColumnIfMissing(con, "submitted_by", "INT");
            AddColumnIfMissing(con, "submitted_by_username", "TEXT");
            AddColumnIfMissing(con, "submitted_date", "DATETIME");
            AddColumnIfMissing(con, "creator_notified", "INTEGER DEFAULT 0 CHECK(creator_notified IN (0, 1))");
        }

        private static void AddColumnIfMissing(SQLiteConnection con, string columnName, string columnDefinition)
        {
            bool exists = false;
            using (var cmd = new SQLiteCommand("PRAGMA table_info(FileM)", con))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    if (string.Equals(r["name"].ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
            }

            if (!exists)
                new SQLiteCommand($"ALTER TABLE FileM ADD COLUMN {columnName} {columnDefinition}", con).ExecuteNonQuery();
        }
    }
}
