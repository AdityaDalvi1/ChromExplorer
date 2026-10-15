using ChromExplorer.Modules.Auth.Models;
using ChromExplorer.Modules.Auth.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using static ChromExplorer.Modules.Auth.Models.User;
using static ChromExplorer.Modules.Auth.ViewModels.LoginViewModel;


namespace ChromExplorer.Modules.Auth.Services
{
    public class AuthService
    {

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";
        // private readonly string _connectionString = @"Data Source=D:\chromatograpy 23 feb 2026\chromexplorer-master 21-02 (1)\chromexplorer-master (2) (1)\chromexplorer-master\ChromExplorer\source.db3;Version=3;";
        // private readonly string _connectionString = @"Data Source=D:\chromexplorer-master (2) (1)\chromexplorer-master\ChromExplorer\source.db3;Version=3;";


        //        public bool AuthenticateUser(LoginModel user)
        //        {
        //            try
        //            {
        //                using (var conn = new SQLiteConnection(_connectionString))
        //                {
        //                    conn.Open();

        //                    string sql = @"
        //                       SELECT count(*)
        //FROM UserM u
        //INNER JOIN PasswordM p ON u.user_id = p.user_id
        //WHERE u.user_name = @u 
        //  AND p.password = @p 
        //  AND u.is_active = 1
        //  AND p.is_active = 1";

        //                    using (var cmd = new SQLiteCommand(sql, conn))
        //                    {
        //                        cmd.Parameters.AddWithValue("@u",user.Username);
        //                        cmd.Parameters.AddWithValue("@p", user.Password);

        //                        long count = (long)cmd.ExecuteScalar();
        //                        return count > 0;
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {

        //                throw new Exception("Database connection failed: " + ex.Message);
        //            }
        //        }
        public bool AuthenticateUser(LoginModel user, out string errorMessage)
        {
            errorMessage = "";
            bool loginSuccess = false;
            bool userExists = false;

            if (IsAccountLocked(user.Username, out DateTime lockedUntil))
            {
                Log("Login Blocked", $"Login blocked for username '{user.Username}' — account locked after {MaxFailedLoginAttempts} failed attempts | Locked until: {lockedUntil:dd-MM-yyyy HH:mm:ss}", "", user.Username);
                errorMessage = $"Account locked due to {MaxFailedLoginAttempts} failed login attempts. Try again after {lockedUntil:dd-MM-yyyy hh:mm tt}, or contact an admin to unlock it.";
                return false;
            }

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    // Check if user exists and is active (regardless of password)
                    string checkSql = "SELECT COUNT(*) FROM UserM WHERE user_name=@u AND is_active=1";
                    using (var cmd = new SQLiteCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", user.Username);
                        userExists = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                    }


                    string sql = @"
            SELECT u.user_id, u.role_id
            FROM UserM u
            INNER JOIN PasswordM p ON u.user_id = p.user_id
            WHERE u.user_name = @u
            AND p.password = @p
            AND u.is_active = 1
            AND p.is_active = 1";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", user.Username);
                        cmd.Parameters.AddWithValue("@p", user.Password);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Application.Current.Properties["UserId"] =
                                    Convert.ToInt32(reader["user_id"]);

                                Application.Current.Properties["RoleId"] =
                                    Convert.ToInt32(reader["role_id"]);

                                Application.Current.Properties["Username"] =
                                    user.Username;

                                loginSuccess = true;
                            }
                        }
                    }
                } // connection CLOSED here

                if (loginSuccess)
                {
                    Log("Login Success", $"User '{user.Username}' logged in successfully | {DateTime.Now:dd-MM-yyyy HH:mm:ss}", "", user.Username);
                    return true;
                }
                else
                {
                    string reason = userExists ? "Wrong password entered" : "Username not found or account inactive";
                    Log("Login Failed", $"Failed login attempt for username '{user.Username}' | Reason: {reason} | {DateTime.Now:dd-MM-yyyy HH:mm:ss}", "", user.Username);

                    if (IsAccountLocked(user.Username, out DateTime justLockedUntil))
                    {
                        errorMessage = $"Account locked due to {MaxFailedLoginAttempts} failed login attempts. Try again after {justLockedUntil:dd-MM-yyyy hh:mm tt}, or contact an admin to unlock it.";
                    }
                    else
                    {
                        errorMessage = "Invalid username, password, or inactive account.";
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                errorMessage = ex.Message;
                return false;
            }
        }

        // Re-verifies a re-entered password against the CURRENTLY logged-in user (from
        // Application.Current.Properties["Username"]), without needing the username again —
        // used to gate saving/deactivating records (e.g. "confirm your password before this
        // change is saved"). PasswordM.password always stores CryptoHelper.Encrypt(plaintext)
        // (see CreateNewUser/ResetPassword/etc., and LoginViewModel.PerformLogin which encrypts
        // before calling AuthenticateUser) — so the re-entered password must be encrypted the
        // same way before comparing, otherwise a correct password never matches.
        public bool VerifyCurrentUserPassword(string password)
        {
            try
            {
                if (string.IsNullOrEmpty(password))
                    return false;

                var username = Application.Current?.Properties?["Username"] as string;

                if (string.IsNullOrEmpty(username))
                    return false;

                string encryptedPassword = CryptoHelper.Encrypt(password);

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT COUNT(*)
                        FROM UserM u
                        INNER JOIN PasswordM p ON u.user_id = p.user_id
                        WHERE u.user_name = @u
                        AND p.password = @p
                        AND u.is_active = 1
                        AND p.is_active = 1";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@p", encryptedPassword);

                        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Password verification error: " + ex.Message);
                return false;
            }
        }

        // Editing/deactivating a saved-file record is restricted to the "Reviewer" role
        // specifically (not just anyone whose role happens to have page access to Review Saved
        // Files) — checked against the CURRENTLY logged-in user's RoleId, same session property
        // every other role check in this app already uses.
        public bool IsCurrentUserReviewer()
        {
            try
            {
                if (Application.Current?.Properties?["RoleId"] == null)
                    return false;

                int roleId = Convert.ToInt32(Application.Current.Properties["RoleId"]);

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT COUNT(*) FROM roleM
                                   WHERE role_id = @roleId AND role_description = 'Reviewer' AND is_active = 1";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@roleId", roleId);
                        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // The role name text for an electronic signature — only RoleId is kept in the session,
        // so this resolves it to the human-readable role_description (e.g. "Reviewer") at the
        // moment of signing.
        public string GetCurrentUserRoleName()
        {
            try
            {
                if (Application.Current?.Properties?["RoleId"] == null)
                    return "";

                int roleId = Convert.ToInt32(Application.Current.Properties["RoleId"]);

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    using (var cmd = new SQLiteCommand("SELECT role_description FROM roleM WHERE role_id = @roleId", conn))
                    {
                        cmd.Parameters.AddWithValue("@roleId", roleId);
                        var result = cmd.ExecuteScalar();
                        return result == null || result == DBNull.Value ? "" : result.ToString();
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        // The electronic signature should carry the person's actual full name, not their login
        // username — falls back to the username if no full name was ever entered for that user.
        public string GetCurrentUserFullName()
        {
            try
            {
                var username = Application.Current?.Properties?["Username"] as string;
                if (string.IsNullOrEmpty(username))
                    return "";

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    using (var cmd = new SQLiteCommand(
                        "SELECT user_fullname, user_name FROM UserM WHERE user_name = @u", conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var fullName = reader["user_fullname"] == DBNull.Value ? "" : reader["user_fullname"].ToString();
                                return string.IsNullOrWhiteSpace(fullName) ? reader["user_name"].ToString() : fullName;
                            }
                        }
                    }
                }

                return username;
            }
            catch
            {
                return "";
            }
        }

        //        public bool AuthenticateUser(LoginModel user)
        //        {
        //            bool loginSuccess = false;

        //            try
        //            {
        //                using (var conn = new SQLiteConnection(_connectionString))
        //                {
        //                    conn.Open();
        //                    string sql = @"
        //                       SELECT u.user_id,u.role_id
        //FROM UserM u
        //INNER JOIN PasswordM p ON u.user_id = p.user_id
        //WHERE u.user_name = @u 
        //  AND p.password = @p 
        //  AND u.is_active = 1
        //  AND p.is_active = 1";

        //                    using (var cmd = new SQLiteCommand(sql, conn))
        //                    {
        //                        cmd.Parameters.AddWithValue("@u", user.Username);
        //                        cmd.Parameters.AddWithValue("@p", user.Password);


        //                        using (var reader = cmd.ExecuteReader())
        //                        {
        //                            if (reader.Read())
        //                            {
        //                                Application.Current.Properties["UserId"] =
        //                                    Convert.ToInt32(reader["user_id"]);

        //                                Application.Current.Properties["RoleId"] =
        //                                    Convert.ToInt32(reader["role_id"]);

        //                                Application.Current.Properties["Username"] =
        //                                    user.Username;


        //                                return true;
        //                            }

        //                            if (loginSuccess)
        //                            {
        //                                Log("LOGIN", "User " + user.Username + " logged into system");
        //                                return true;
        //                            }

        //                            return false;
        //                        }
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                throw new Exception("Database connection failed: " + "Database Connection Failed.");
        //            }
        //        }
        public string CreateNewUser(string username, string fullName, string email, string password, int roleId, int adminId)
        {
            try
            {
                EnsureFirstLoginColumns();
                EnsurePasswordExpiryColumn();
                EnsurePassTransactionTable();

                string plainPassword = password;

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string checkSql = "SELECT count(*) FROM UserM WHERE user_name = @u OR user_fullname = @fn";
                            using (var cmdCheck = new SQLiteCommand(checkSql, conn, transaction))
                            {
                                cmdCheck.Parameters.AddWithValue("@u", username);
                                cmdCheck.Parameters.AddWithValue("@fn", fullName);

                                long count = (long)cmdCheck.ExecuteScalar();

                                if (count > 0)
                                {
                                    return "UserName Or User Full Name already exits.";
                                }
                            }

                            long newUserId = 0;
                            string insertUserSql = @"
                                INSERT INTO UserM (user_name, user_fullname, user_mail_id, role_id, inserted_by, is_active, is_first_login, inserted_date)
                                VALUES (@u, @fn, @em, @ro, @ins, 1, 1, datetime('now','localtime'));
                                SELECT last_insert_rowid();";

                            using (var cmdUser = new SQLiteCommand(insertUserSql, conn, transaction))
                            {
                                cmdUser.Parameters.AddWithValue("@u", username);
                                cmdUser.Parameters.AddWithValue("@fn", fullName);
                                cmdUser.Parameters.AddWithValue("@em", email);
                                // cmdUser.Parameters.AddWithValue("@co", contact);
                                cmdUser.Parameters.AddWithValue("@ro", roleId);
                                cmdUser.Parameters.AddWithValue("@ins", adminId);

                                newUserId = (long)cmdUser.ExecuteScalar();
                            }

                            string NewPassword = password;
                            string encryptedPassword = CryptoHelper.Encrypt(NewPassword);

                            string insertPassSql = @"
                                INSERT INTO PasswordM (user_id, password, inserted_by, is_active)
                                VALUES (@uid, @pass, @ins, 1)";

                            using (var cmdPass = new SQLiteCommand(insertPassSql, conn, transaction))
                            {
                                cmdPass.Parameters.AddWithValue("@uid", newUserId);
                                cmdPass.Parameters.AddWithValue("@pass", encryptedPassword);
                                cmdPass.Parameters.AddWithValue("@ins", adminId);

                                cmdPass.ExecuteNonQuery();
                            }

                            // Log to pass_transaction
                            LogPassTransaction(conn, transaction, newUserId, encryptedPassword, adminId);

                            string insertRoleSql = @"
                        INSERT INTO user_wise_role_details 
                        (user_id, role_id, is_active, inserted_by, inserted_date)
                        VALUES (@uid, @rid, 1, @ins, datetime('now'))";

                            using (var cmdRole = new SQLiteCommand(insertRoleSql, conn, transaction))
                            {
                                cmdRole.Parameters.AddWithValue("@uid", newUserId);
                                cmdRole.Parameters.AddWithValue("@rid", roleId);
                                cmdRole.Parameters.AddWithValue("@ins", adminId);

                                cmdRole.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            Log("User Created",
                                $"New user '{fullName}' created successfully | " +
                                $"Username: {username} | Email: {email} | " +
                                $"Created on: {DateTime.Now:dd-MM-yyyy HH:mm:ss} | " +
                                $"Account will be deactivated if first login not done within 2 hours");
                            SendWelcomeEmail(email, username, plainPassword, fullName);
                            return "Success";
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error creating user: " + "Database Connection Failed.";
            }
        }

        public string GetPlainPassword(string username)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT p.password FROM UserM u
                                   INNER JOIN PasswordM p ON u.user_id = p.user_id
                                   WHERE u.user_name = @u AND p.is_active = 1";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        var enc = cmd.ExecuteScalar()?.ToString();
                        if (string.IsNullOrEmpty(enc)) return "User not found.";
                        return CryptoHelper.Decrypt(enc);
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public void Log(string action, string description, string fileName = "", string parameter = "")
        {
            try
            {
                int userId = 0;

                if (Application.Current.Properties["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    string sql = @"INSERT INTO AuditLogM
                          (action, description, is_active, inserted_by, inserted_date, parameter)
                          VALUES
                          (@action, @desc, 1, @uid, datetime('now','localtime'), @param)";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@desc", description);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@param", parameter ?? "");

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Log Error: " + ex.Message);
            }
        }

        // Call once at startup. If the previous session's last lifecycle entry is
        // "Application Started" with no matching "Application Closed"/"Application Crashed"
        // after it, that session never got to record its own end — the only way that happens
        // is a hard kill (Task Manager "End Task"), a power loss, or an OS-level crash, none of
        // which run any in-process code. We can't catch those live, but we can detect them
        // retroactively the next time the app starts.
        public void RecordApplicationStart()
        {
            string abnormalLastStart = null;

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT action, inserted_date FROM AuditLogM
                                  WHERE action IN ('Application Started', 'Application Closed', 'Application Crashed')
                                  ORDER BY audit_log_id DESC LIMIT 1";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && reader["action"]?.ToString() == "Application Started")
                        {
                            abnormalLastStart = reader["inserted_date"]?.ToString();
                        }
                    }
                    // Connection/reader are fully closed by the end of this block, before any
                    // write below opens its own connection to the same file — Log() must never
                    // be called while a read connection to the same DB is still open, or the
                    // write can end up waiting on the read lock that read holds.
                }
            }
            catch { }

            if (abnormalLastStart != null)
            {
                Log("Application Terminated Unexpectedly",
                    $"Previous session (started {abnormalLastStart}) has no recorded normal close or caught crash — " +
                    $"likely force-closed, killed, or lost power | Detected on next startup: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            }

            Log("Application Started", $"ChromExplorer started | {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
        }

        public void RecordApplicationCrash(string details)
        {
            try
            {
                Log("Application Crashed", $"Unhandled exception caused application crash | {DateTime.Now:dd-MM-yyyy HH:mm:ss} | {details}");
            }
            catch { }
        }

        // Failed-login lockout: 5 failed attempts (since the last success or admin unlock)
        // locks the account for 1 hour, or until an admin unlocks it early.
        private const int MaxFailedLoginAttempts = 3;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromHours(1);

        public bool IsAccountLocked(string username, out DateTime lockedUntil)
        {
            lockedUntil = DateTime.MinValue;

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    DateTime? resetPoint = null;
                    string resetSql = @"SELECT MAX(inserted_date) FROM AuditLogM
                                        WHERE parameter = @u AND action IN ('Login Success','Account Unlocked')";
                    using (var cmd = new SQLiteCommand(resetSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        var val = cmd.ExecuteScalar()?.ToString();
                        if (!string.IsNullOrEmpty(val) && DateTime.TryParse(val, out DateTime rp))
                            resetPoint = rp;
                    }

                    string sql = @"SELECT inserted_date FROM AuditLogM
                                  WHERE parameter = @u AND action = 'Login Failed'"
                                  + (resetPoint.HasValue ? " AND inserted_date > @resetPoint" : "") + @"
                                  ORDER BY inserted_date DESC
                                  LIMIT @maxAttempts";

                    var fails = new List<DateTime>();
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@maxAttempts", MaxFailedLoginAttempts);
                        if (resetPoint.HasValue)
                            cmd.Parameters.AddWithValue("@resetPoint", resetPoint.Value.ToString("yyyy-MM-dd HH:mm:ss"));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (DateTime.TryParse(reader["inserted_date"].ToString(), out DateTime d))
                                    fails.Add(d);
                            }
                        }
                    }

                    if (fails.Count < MaxFailedLoginAttempts) return false;

                    DateTime oldestOfWindow = fails[fails.Count - 1]; // ordered DESC, last = oldest of the 5
                    DateTime unlockAt = oldestOfWindow.Add(LockoutDuration);

                    if (DateTime.Now < unlockAt)
                    {
                        lockedUntil = unlockAt;
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        // Admin action: unlocks the account early by resetting the failed-attempt window.
        public void UnlockAccount(string username, string reason)
        {
            Log("Account Unlocked", $"Account for username '{username}' unlocked by admin | Reason: {reason} | {DateTime.Now:dd-MM-yyyy HH:mm:ss}", "", username);
        }

        /// <summary>
        /// Formats a raw SQLite datetime string to a fixed, human-readable form that always
        /// keeps second-level precision — used for every audit report entry (grid + PDF export).
        /// </summary>
        private static string FormatAuditDateTime(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            return DateTime.TryParse(raw, out DateTime dt)
                ? dt.ToString("dd-MMM-yyyy hh:mm:ss tt")
                : raw;
        }

        public List<ReportDO> BindData()
        {
            List<ReportDO> list = new List<ReportDO>();

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                SELECT 
                    a.audit_log_id,
                    IFNULL(u.user_name,'Unknown User') AS user_name,
                    IFNULL(r.role_description,'N/A') AS role_description,
                    a.action,
                    a.description,
                    a.file_name,
                    a.inserted_date,
                    a.auditor_remark,
                    a.inserted_date,
                    a.parameter
                FROM AuditLogM a
                LEFT JOIN UserM u ON a.inserted_by = u.user_id AND u.is_active = 1
                LEFT JOIN roleM r ON r.role_id = u.role_id AND r.is_active = 1
                WHERE a.is_active = 1
                ORDER BY a.audit_log_id DESC";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        int sr = 1;

                        while (reader.Read())
                        {
                            list.Add(new ReportDO
                            {
                                SrNo = sr++,
                                AuditLogId = Convert.ToInt32(reader["audit_log_id"]),
                                Username = reader["user_name"]?.ToString(),
                                Role = reader["role_description"]?.ToString(),
                                Action = reader["action"]?.ToString(),
                                Description = reader["description"]?.ToString(),
                                FileName = reader["file_name"]?.ToString(),
                                //InsertedDate = Convert.ToDateTime(reader["inserted_date"]),
                                AuditorRemark = reader["auditor_remark"]?.ToString(),
                                InsertedDate = FormatAuditDateTime(reader["inserted_date"]?.ToString()),
                                Parameters = reader["parameter"]?.ToString(),
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Auth Service - BindReportData", ex.Message);
            }

            return list;
        }

        public List<ReportDO> FilterByText(string type, string value)
        {
            List<ReportDO> list = new List<ReportDO>();
            string column = "";
            if (type == "Username") column = "u.user_name";
            else if (type == "Role") column = "r.role_description";
            else if (type == "Action") column = "a.action";
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = $@" SELECT a.audit_log_id, IFNULL(u.user_name,'Unknown') AS user_name, IFNULL(r.role_description,'N/A') AS role_description, a.action, a.description, a.file_name, a.inserted_date,a.parameter, IFNULL(a.auditor_remark,'') AS auditor_remark FROM AuditLogM a LEFT JOIN UserM u ON a.inserted_by = u.user_id LEFT JOIN roleM r ON r.role_id = u.role_id WHERE a.is_active = 1 AND {column} LIKE @value ORDER BY a.audit_log_id DESC";
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@value", "%" + value + "%");
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        int sr = 1;
                        while (reader.Read())
                        {
                            ReportDO obj = new ReportDO();
                            obj.SrNo = sr++;
                            obj.AuditLogId = Convert.ToInt32(reader["audit_log_id"]);
                            obj.Username = reader["user_name"].ToString();
                            obj.Role = reader["role_description"].ToString();
                            obj.Action = reader["action"].ToString();
                            obj.Description = reader["description"].ToString();
                            obj.FileName = reader["file_name"].ToString();
                            obj.InsertedDate = FormatAuditDateTime(reader["inserted_date"].ToString());
                            obj.AuditorRemark = reader["auditor_remark"].ToString();
                            obj.Parameters = reader["parameter"].ToString();
                            list.Add(obj);
                        }
                    }
                }
            }
            return list;
        }

        public List<ReportDO> FilterByDate(DateTime? from, DateTime? to)
        {
            List<ReportDO> list = new List<ReportDO>();
            if (from == null || to == null) return list;
            using (SQLiteConnection conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = @" SELECT a.audit_log_id, IFNULL(u.user_name,'Unknown') AS user_name, IFNULL(r.role_description,'N/A') AS role_description, a.action, a.description,a.parameter, a.file_name, a.inserted_date, IFNULL(a.auditor_remark,'') AS auditor_remark FROM AuditLogM a LEFT JOIN UserM u ON a.inserted_by = u.user_id LEFT JOIN roleM r ON r.role_id = u.role_id WHERE a.is_active = 1 AND date(a.inserted_date) BETWEEN date(@from) AND date(@to) ORDER BY a.audit_log_id DESC";
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    // SQLite date format must be yyyy-MM-dd
                    cmd.Parameters.AddWithValue("@from", from.Value.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@to", to.Value.ToString("yyyy-MM-dd"));
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        int sr = 1;
                        while (reader.Read())
                        {
                            ReportDO obj = new ReportDO();
                            obj.SrNo = sr++;
                            obj.AuditLogId = Convert.ToInt32(reader["audit_log_id"]);
                            obj.Username = reader["user_name"].ToString();
                            obj.Role = reader["role_description"].ToString();
                            obj.Action = reader["action"].ToString();
                            obj.Description = reader["description"].ToString();
                            obj.FileName = reader["file_name"].ToString();
                            obj.InsertedDate = FormatAuditDateTime(reader["inserted_date"].ToString());
                            obj.AuditorRemark = reader["auditor_remark"].ToString();
                            obj.Parameters = reader["parameter"].ToString();

                            list.Add(obj);
                        }
                    }
                }
            }
            return list;
        }

        public bool SaveAuditorRemark(int auditId, string remark)
        {
            try
            {
                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    // important for SQLite write conflicts
                    using (var cmd = new SQLiteCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = @"UPDATE AuditLogM
                                    SET auditor_remark = @remark
                                    WHERE audit_log_id = @id";

                        cmd.Parameters.AddWithValue("@remark", remark.Trim());
                        cmd.Parameters.AddWithValue("@id", auditId);

                        int rows = cmd.ExecuteNonQuery();

                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("SaveAuditorRemark", ex.Message);
                return false;
            }
        }

        public List<UserAccount> GetUserList()
        {
            List<UserAccount> userList = new List<UserAccount>();

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"
            SELECT
                u.user_id,
                u.user_name,
                u.user_fullname,
                u.user_mail_id,
                u.role_id,
                u.is_active,
                p.password,
                r.role_description AS role_name
            FROM UserM u
            LEFT JOIN PasswordM p ON p.user_id = u.user_id
            LEFT JOIN roleM r on r.role_id = u.role_id and r.is_active = 1
            ORDER BY u.is_active DESC, u.user_id;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            int srNo = 1;
                            while (reader.Read())
                            {
                                string userName = reader["user_name"].ToString();
                                bool isLocked = IsAccountLocked(userName, out DateTime lockedUntil);

                                userList.Add(new UserAccount
                                {
                                    SrNo = srNo++,
                                    UserId = Convert.ToInt32(reader["user_id"]),
                                    UserName = userName,
                                    FullName = reader["user_fullname"].ToString(),
                                    Email = reader["user_mail_id"].ToString(),
                                    Password = CryptoHelper.Decrypt(reader["password"].ToString()),
                                    RoleName = reader["role_name"].ToString(),
                                    RoleId = Convert.ToInt32(reader["role_id"]),
                                    IsActive = Convert.ToInt32(reader["is_active"]) == 1,
                                    IsLocked = isLocked,
                                    LockedUntil = isLocked ? (DateTime?)lockedUntil : null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching user " + "Database Connection Failed.");
            }

            return userList;
        }

        public UserAccount GetUserById(int userId)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT u.user_id, u.user_name, u.user_fullname, u.user_mail_id, u.role_id, p.password
                        FROM UserM u
                        LEFT JOIN PasswordM p ON p.user_id = u.user_id
                        WHERE u.user_id = @uid";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new UserAccount
                                {
                                    UserId = Convert.ToInt32(reader["user_id"]),
                                    UserName = reader["user_name"].ToString(),
                                    FullName = reader["user_fullname"].ToString(),
                                    Email = reader["user_mail_id"].ToString(),
                                    Password = CryptoHelper.Decrypt(reader["password"].ToString()),
                                    RoleId = Convert.ToInt32(reader["role_id"])
                                };
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public bool DeleteUser(int userId, string fullName, string reason)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE UserM SET is_active = 0 WHERE user_id = @uid";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }

                    SetAssignedRoleActive(conn, userId, false);
                }
                Log("Delete User", $"User '{fullName}' deleted | Reason: {reason}");
                return true;
            }
            catch (Exception ex)
            {
                Log("Delete User Error", ex.Message);
                return false;
            }
        }

        public bool ActivateUser(int userId, string fullName, string reason)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE UserM SET is_active = 1 WHERE user_id = @uid";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }

                    SetAssignedRoleActive(conn, userId, true);
                }
                Log("Activate User", $"User '{fullName}' activated | Reason: {reason}");
                return true;
            }
            catch (Exception ex)
            {
                Log("Activate User Error", ex.Message);
                return false;
            }
        }

        private void SetAssignedRoleActive(SQLiteConnection conn, int userId, bool isActive)
        {
            int updatedBy = 0;

            if (Application.Current.Properties["UserId"] != null)
                updatedBy = Convert.ToInt32(Application.Current.Properties["UserId"]);

            string sql = @"UPDATE user_wise_role_details
                           SET is_active = @isActive,
                               updated_by = @updatedBy,
                               updated_date = datetime('now')
                           WHERE user_id = @uid";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@updatedBy", updatedBy);
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public string UpdateUser(int userId, string username, string fullName, string email, int roleId, string reason)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string updateUser = "UPDATE UserM SET user_name=@un, user_fullname=@fn, user_mail_id=@em, role_id=@rid WHERE user_id=@uid";
                            using (var cmd = new SQLiteCommand(updateUser, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@un", username);
                                cmd.Parameters.AddWithValue("@fn", fullName);
                                cmd.Parameters.AddWithValue("@em", email);
                                cmd.Parameters.AddWithValue("@rid", roleId);
                                cmd.Parameters.AddWithValue("@uid", userId);
                                cmd.ExecuteNonQuery();
                            }
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                Log("Edit User", $"User '{fullName}' updated | Reason: {reason}");
                return "Success";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public void EnsurePassTransactionTable()
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"
                        CREATE TABLE IF NOT EXISTS pass_transaction (
                            passtrans_id    INTEGER PRIMARY KEY AUTOINCREMENT,
                            user_id         INTEGER NOT NULL,
                            password        TEXT    NOT NULL,
                            is_active       INTEGER DEFAULT 1,
                            inserted_by     INTEGER,
                            inserted_date   TEXT    DEFAULT (datetime('now','localtime'))
                        )";
                    using (var cmd = new SQLiteCommand(sql, conn))
                        cmd.ExecuteNonQuery();

                    // If table was created with old 'pass_name' column, add 'password' column
                    bool hasPassword = false;
                    using (var cmd = new SQLiteCommand("PRAGMA table_info(pass_transaction)", conn))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            if (r["name"].ToString() == "password") { hasPassword = true; break; }

                    if (!hasPassword)
                        new SQLiteCommand("ALTER TABLE pass_transaction ADD COLUMN password TEXT", conn).ExecuteNonQuery();
                }
            }
            catch { }
        }

        // Idempotent seed for the Reviewer feature: makes the "Review Saved Files" page
        // assignable in menuM (no code path ever inserted menu rows before this) and gives the
        // app a ready-made "Reviewer" role, since neither existed on disk. Safe to call on every
        // startup — both inserts are guarded by an existence check.
        public void EnsureReviewerFeatureSeed()
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    const string menuName = "ChromExplorer.Modules.Auth.Views.ReviewSavedFiles";
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM menuM WHERE menu_name = @name", conn))
                    {
                        cmd.Parameters.AddWithValue("@name", menuName);
                        if (Convert.ToInt64(cmd.ExecuteScalar()) == 0)
                        {
                            using (var ins = new SQLiteCommand(
                                @"INSERT INTO menuM (sub_menu_id, menu_name, menu_link, icon, is_active, inserted_date)
                                  VALUES (0, @name, 'ReviewSavedFiles.xaml', '', 1, datetime('now','localtime'))", conn))
                            {
                                ins.Parameters.AddWithValue("@name", menuName);
                                ins.ExecuteNonQuery();
                            }
                        }
                    }

                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM roleM WHERE role_description = 'Reviewer'", conn))
                    {
                        if (Convert.ToInt64(cmd.ExecuteScalar()) == 0)
                        {
                            new SQLiteCommand(
                                @"INSERT INTO roleM (role_description, is_active, inserted_date)
                                  VALUES ('Reviewer', 1, datetime('now','localtime'))", conn).ExecuteNonQuery();
                        }
                    }

                    // The Reviewer role needs the Audit Trail (AuditorReport) and its own Review
                    // Saved Files page granted by default — otherwise a brand-new Reviewer role
                    // has zero page rights until an Admin manually visits Assign Rights.
                    GrantRoleMenuRightIfMissing(conn, "Reviewer", "AuditorReport");
                    GrantRoleMenuRightIfMissing(conn, "Reviewer", menuName);

                    // The Analyst role is the one actually generating reports from the
                    // chromatogram viewer day to day — without this, "Generate Report" throws
                    // "You are not authorized to generate this report" (gated on the
                    // "ChromatogramGenerateReport" page in ChromatogramViewViewModel.HasGenerateReportAccess)
                    // for every Analyst until an Admin manually grants it via Assign Rights.
                    GrantRoleMenuRightIfMissing(conn, "Analyst", "ChromatogramGenerateReport");

                    // Every role gets this page too, not just Reviewer — a regular analyst needs
                    // to open it to see the files THEY saved (scoped to their own rows in
                    // ReviewSavedFilesViewModel.GetSavedFiles); only a Reviewer sees every
                    // creator's files once inside. Without this, non-Reviewer roles have no menu
                    // right at all and can't navigate here.
                    var roleNames = new System.Collections.Generic.List<string>();
                    using (var roleCmd = new SQLiteCommand("SELECT role_description FROM roleM WHERE is_active = 1", conn))
                    using (var roleReader = roleCmd.ExecuteReader())
                    {
                        while (roleReader.Read())
                            roleNames.Add(roleReader["role_description"]?.ToString());
                    }

                    // The reader above must be fully closed before running more commands on the
                    // same connection (SQLite's ADO.NET provider doesn't support an open reader
                    // and other commands running concurrently), hence the two separate loops.
                    foreach (var roleName in roleNames)
                    {
                        if (string.IsNullOrEmpty(roleName)) continue;
                        GrantRoleMenuRightIfMissing(conn, roleName, menuName);
                    }

                    // RSD had no menuM row at all before now (see RsdNavigationView's own comment
                    // on why it used to navigate ungated) — seed it, restricted to Admin and
                    // Analyst only, not every role.
                    const string rsdMenuName = "RsdView";
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM menuM WHERE menu_name = @name", conn))
                    {
                        cmd.Parameters.AddWithValue("@name", rsdMenuName);
                        if (Convert.ToInt64(cmd.ExecuteScalar()) == 0)
                        {
                            using (var ins = new SQLiteCommand(
                                @"INSERT INTO menuM (sub_menu_id, menu_name, menu_link, icon, is_active, inserted_date)
                                  VALUES (0, @name, 'RsdView.xaml', '', 1, datetime('now','localtime'))", conn))
                            {
                                ins.Parameters.AddWithValue("@name", rsdMenuName);
                                ins.ExecuteNonQuery();
                            }
                        }
                    }

                    GrantRoleMenuRightIfMissing(conn, "Admin", rsdMenuName);
                    GrantRoleMenuRightIfMissing(conn, "Analyst", rsdMenuName);

                    ApplyDefaultRoleRights(conn);
                }
            }
            catch { }
        }

        // ONE place that states which screens each role gets by default. Only ADDS missing grants
        // (it never removes or re-activates anything), so an Admin's later changes on the Assign
        // Rights screen are respected, and a role or screen that does not exist is skipped.
        //   Admin    - runs the system: users, roles, rights, audit report + export, and every
        //              working screen.
        //   Analyst  - does the lab work: MT30, load data, processing, settings, Quan, RSD,
        //              reports, the audit report, and their own saved files.
        //   Reviewer - reviews and signs saved files (sees all users' files) and the audit report.
        //   Approver - the approval stage: the review list.
        //   Auditor  - independent inspector: the review list, the audit report and its PDF
        //              export (read-only; cannot sign).
        private static readonly System.Collections.Generic.Dictionary<string, string[]> DefaultRoleRights =
            new System.Collections.Generic.Dictionary<string, string[]>
            {
                ["Admin"] = new[]
                {
                    "ChromExplorer.Modules.Auth.Views.UserDetails",
                    "ChromExplorer.Modules.Auth.Views.ViewAssignRole",
                    "ChromExplorer.Modules.Auth.Views.ViewAssignRights",
                    "ChromExplorer.Modules.Auth.Views.ReviewSavedFiles",
                    "AuditorReport", "AuditorExportPdf",
                    "MT30Controlebutton", "DataProviderFileLoad", "DataProcessingOnChromatogram",
                    "DataProcessingSettings", "QuanData", "ChromatogramGenerateReport", "RsdView"
                },
                ["Analyst"] = new[]
                {
                    "ChromExplorer.Modules.Auth.Views.ReviewSavedFiles",
                    "AuditorReport",
                    "MT30Controlebutton", "DataProviderFileLoad", "DataProcessingOnChromatogram",
                    "DataProcessingSettings", "QuanData", "ChromatogramGenerateReport", "RsdView"
                },
                ["Reviewer"] = new[]
                {
                    "ChromExplorer.Modules.Auth.Views.ReviewSavedFiles",
                    "AuditorReport"
                },
                ["Approver"] = new[]
                {
                    "ChromExplorer.Modules.Auth.Views.ReviewSavedFiles"
                },
                ["Auditor"] = new[]
                {
                    "ChromExplorer.Modules.Auth.Views.ReviewSavedFiles",
                    "AuditorReport", "AuditorExportPdf"
                }
            };

        private static void ApplyDefaultRoleRights(SQLiteConnection conn)
        {
            foreach (var role in DefaultRoleRights)
            {
                foreach (var menu in role.Value)
                    GrantRoleMenuRightIfMissing(conn, role.Key, menu);
            }
        }

        private static void GrantRoleMenuRightIfMissing(SQLiteConnection conn, string roleDescription, string menuName)
        {
            try
            {
                int roleId = -1;
                using (var cmd = new SQLiteCommand("SELECT role_id FROM roleM WHERE role_description = @role", conn))
                {
                    cmd.Parameters.AddWithValue("@role", roleDescription);
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return;
                    roleId = Convert.ToInt32(result);
                }

                int menuId = -1;
                using (var cmd = new SQLiteCommand("SELECT menu_id FROM menuM WHERE menu_name = @menu", conn))
                {
                    cmd.Parameters.AddWithValue("@menu", menuName);
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return;
                    menuId = Convert.ToInt32(result);
                }

                using (var cmd = new SQLiteCommand(
                    "SELECT COUNT(*) FROM role_wise_rights_details WHERE role_id = @roleId AND menu_id = @menuId", conn))
                {
                    cmd.Parameters.AddWithValue("@roleId", roleId);
                    cmd.Parameters.AddWithValue("@menuId", menuId);
                    if (Convert.ToInt64(cmd.ExecuteScalar()) > 0) return;
                }

                using (var cmd = new SQLiteCommand(
                    @"INSERT INTO role_wise_rights_details (role_id, menu_id, sub_menu_id, is_active, inserted_date)
                      VALUES (@roleId, @menuId, 0, 1, datetime('now','localtime'))", conn))
                {
                    cmd.Parameters.AddWithValue("@roleId", roleId);
                    cmd.Parameters.AddWithValue("@menuId", menuId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private void LogPassTransaction(SQLiteConnection conn, SQLiteTransaction tx, long userId, string encryptedPassword, long insertedBy)
        {
            try
            {
                EnsurePassTransactionTable();

                // Deactivate previous active password in history
                string deactivateSql = "UPDATE pass_transaction SET is_active=0 WHERE user_id=@uid AND is_active=1";
                using (var cmd = tx != null
                    ? new SQLiteCommand(deactivateSql, conn, tx)
                    : new SQLiteCommand(deactivateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.ExecuteNonQuery();
                }

                // Insert new active password row with today's date
                string insertSql = @"
                    INSERT INTO pass_transaction (user_id, password, is_active, inserted_by, inserted_date)
                    VALUES (@uid, @pass, 1, @ins, datetime('now','localtime'))";
                using (var cmd = tx != null
                    ? new SQLiteCommand(insertSql, conn, tx)
                    : new SQLiteCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@pass", encryptedPassword);
                    cmd.Parameters.AddWithValue("@ins", insertedBy);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Log("Pass Transaction Error", "Failed to log pass_transaction: " + ex.Message);
            }
        }

        // Rejects a new password if it matches any of the user's last N (default 5) passwords,
        // read from pass_transaction (which already stores every prior password on change).
        public bool IsPasswordReused(int userId, string newEncryptedPassword, int historyCount = 5)
        {
            try
            {
                EnsurePassTransactionTable();

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT password FROM pass_transaction
                                  WHERE user_id = @uid
                                  ORDER BY inserted_date DESC, passtrans_id DESC
                                  LIMIT @limit";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@limit", historyCount);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (string.Equals(reader["password"]?.ToString(), newEncryptedPassword, StringComparison.Ordinal))
                                    return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fail closed on an unexpected error here would block legitimate password
                // changes on any transient DB hiccup, but a silent swallow is what let a
                // wrong column name bypass this whole check undetected — so at minimum, log it.
                Log("Password Reuse Check Error", $"IsPasswordReused failed for user {userId}: {ex.Message}");
            }

            return false;
        }

        public bool IsPasswordExpired(int userId)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT inserted_date FROM pass_transaction WHERE user_id=@uid AND is_active=1";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        var val = cmd.ExecuteScalar()?.ToString();
                        if (string.IsNullOrEmpty(val)) return false;
                        if (DateTime.TryParse(val, out DateTime insertedDate))
                            return DateTime.Now > insertedDate.AddDays(90);
                    }
                }
            }
            catch { }
            return false;
        }

        // Warns the user on login once their password is within warningWindowDays of hitting the
        // 90-day expiry (but hasn't expired yet — that's IsPasswordExpired's job). Shows on every
        // login during that window, not just once, so the reminder doesn't get missed.
        public bool TryGetPasswordExpiryWarning(int userId, int warningWindowDays, out int daysRemaining)
        {
            daysRemaining = 0;
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT inserted_date FROM pass_transaction WHERE user_id=@uid AND is_active=1";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        var val = cmd.ExecuteScalar()?.ToString();
                        if (string.IsNullOrEmpty(val)) return false;
                        if (DateTime.TryParse(val, out DateTime insertedDate))
                        {
                            DateTime expiryDate = insertedDate.AddDays(90);
                            TimeSpan remaining = expiryDate - DateTime.Now;
                            if (remaining.TotalSeconds <= 0) return false; // already expired — handled by IsPasswordExpired

                            daysRemaining = (int)Math.Ceiling(remaining.TotalDays);
                            return daysRemaining <= warningWindowDays;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public void EnsureFirstLoginColumns()
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    bool hasFirstLogin = false;
                    using (var cmd = new SQLiteCommand("PRAGMA table_info(UserM)", conn))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            if (r["name"].ToString() == "is_first_login") { hasFirstLogin = true; break; }

                    if (!hasFirstLogin)
                        new SQLiteCommand("ALTER TABLE UserM ADD COLUMN is_first_login INTEGER DEFAULT 0", conn).ExecuteNonQuery();
                }
            }
            catch { }
        }

        public bool IsFirstLogin(int userId)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT is_first_login FROM UserM WHERE user_id=@uid AND is_active=1", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        var val = cmd.ExecuteScalar();
                        return val != null && Convert.ToInt32(val) == 1;
                    }
                }
            }
            catch { return false; }
        }

        public bool CheckAndDeactivateExpired(int userId)
        {
            try
            {
                string fullName = "Unknown";
                bool isFirst = false;
                DateTime created = DateTime.MinValue;

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT is_first_login, inserted_date, user_fullname FROM UserM WHERE user_id=@uid AND is_active=1";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) return false;
                            isFirst = Convert.ToInt32(r["is_first_login"]) == 1;
                            fullName = r["user_fullname"]?.ToString() ?? "Unknown";
                            string createdStr = r["inserted_date"]?.ToString();

                            if (!isFirst) return false;
                            if (string.IsNullOrEmpty(createdStr)) return false;
                            if (!DateTime.TryParse(createdStr, out created)) return false;
                        }
                    }
                }

                DateTime loginAttemptTime = DateTime.Now;
                TimeSpan elapsed = loginAttemptTime - created;

                if (loginAttemptTime > created.AddHours(2))
                {
                    // Deactivate user
                    using (var conn = new SQLiteConnection(_connectionString))
                    {
                        conn.Open();
                        using (var cmd = new SQLiteCommand("UPDATE UserM SET is_active=0 WHERE user_id=@uid", conn))
                        {
                            cmd.Parameters.AddWithValue("@uid", userId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    Log("Account Deactivated",
                        $"User '{fullName}' (ID:{userId}) account deactivated | " +
                        $"Account created: {created:dd-MM-yyyy HH:mm:ss} | " +
                        $"Login attempted: {loginAttemptTime:dd-MM-yyyy HH:mm:ss} | " +
                        $"Time elapsed: {(int)elapsed.TotalHours}h {elapsed.Minutes}m — exceeded 2 hour limit");
                    return true;
                }
                else
                {
                    // Within 2 hours — log the first login attempt
                    Log("First Login Attempt",
                        $"User '{fullName}' (ID:{userId}) attempting first login | " +
                        $"Account created: {created:dd-MM-yyyy HH:mm:ss} | " +
                        $"Login time: {loginAttemptTime:dd-MM-yyyy HH:mm:ss} | " +
                        $"Time remaining: {(int)(created.AddHours(2) - loginAttemptTime).TotalMinutes} minutes before expiry");
                }
            }
            catch { }
            return false;
        }

        public string ChangeFirstTimePassword(int userId, string currentPlainPassword, string newPassword)
        {
            try
            {
                // Verify current password — try new key first, fallback to legacy key
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT password FROM PasswordM WHERE user_id=@uid AND is_active=1", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        var stored = cmd.ExecuteScalar()?.ToString();
                        if (string.IsNullOrEmpty(stored))
                            return "User not found.";
                        if (stored != CryptoHelper.Encrypt(currentPlainPassword))
                            return "Current password is incorrect.";
                    }
                }

                // Update password + clear first-login flag + set 90-day expiry
                string newEnc = CryptoHelper.Encrypt(newPassword);

                if (IsPasswordReused(userId, newEnc))
                {
                    Log("First Login Password Change Failed", $"User (ID:{userId}) attempted to reuse one of their last 5 passwords | {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                    return "New password cannot be the same as any of your last 5 passwords.";
                }

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("UPDATE PasswordM SET password=@p WHERE user_id=@uid AND is_active=1", conn))
                    {
                        cmd.Parameters.AddWithValue("@p", newEnc);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand("UPDATE UserM SET is_first_login=0 WHERE user_id=@uid", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Log to pass_transaction using fresh connection
                using (var conn2 = new SQLiteConnection(_connectionString))
                {
                    conn2.Open();
                    LogPassTransaction(conn2, null, userId, newEnc, userId);
                }

                string fullName = "Unknown";
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT user_fullname FROM UserM WHERE user_id=@uid", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        fullName = cmd.ExecuteScalar()?.ToString() ?? "Unknown";
                    }
                }

                Log("First Login Password Changed",
                    $"User '{fullName}' (ID:{userId}) changed password on first login | " +
                    $"Changed on: {DateTime.Now:dd-MM-yyyy HH:mm:ss} | Password valid for 90 days");
                return "Success";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        // Used from the "Change Password" menu option for already-logged-in users
        public string ChangePassword(int userId, string currentPlainPassword, string newPassword)
        {
            string fullName = "Unknown";
            try
            {
                // Get fullName + verify current password
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    using (var cmd = new SQLiteCommand("SELECT user_fullname FROM UserM WHERE user_id=@uid", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        fullName = cmd.ExecuteScalar()?.ToString() ?? "Unknown";
                    }

                    using (var cmd = new SQLiteCommand("SELECT password FROM PasswordM WHERE user_id=@uid AND is_active=1", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        var stored = cmd.ExecuteScalar()?.ToString();
                        if (string.IsNullOrEmpty(stored))
                        {
                            Log("Password Change Failed", $"User '{fullName}' (ID:{userId}) — user not found in PasswordM | {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                            return "User not found.";
                        }
                        if (stored != CryptoHelper.Encrypt(currentPlainPassword))
                        {
                            Log("Password Change Failed", $"User '{fullName}' (ID:{userId}) entered incorrect current password | {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                            return "Current password is incorrect.";
                        }
                    }
                }

                string newEnc = CryptoHelper.Encrypt(newPassword);

                if (IsPasswordReused(userId, newEnc))
                {
                    Log("Password Change Failed", $"User '{fullName}' (ID:{userId}) attempted to reuse one of their last 5 passwords | {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                    return "New password cannot be the same as any of your last 5 passwords.";
                }

                // Step 1 — Update PasswordM
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("UPDATE PasswordM SET password=@p WHERE user_id=@uid AND is_active=1", conn))
                    {
                        cmd.Parameters.AddWithValue("@p", newEnc);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Step 2 — Deactivate old pass_transaction row, insert new row
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    using (var cmd = new SQLiteCommand("UPDATE pass_transaction SET is_active=0 WHERE user_id=@uid AND is_active=1", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SQLiteCommand(@"INSERT INTO pass_transaction (user_id, password, is_active, inserted_by, inserted_date)
                                                         VALUES (@uid, @pass, 1, @ins, datetime('now','localtime'))", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@pass", newEnc);
                        cmd.Parameters.AddWithValue("@ins", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Step 3 — Audit log after both tables updated successfully
                Log("Password Changed", $"User '{fullName}' (ID:{userId}) changed password successfully | Changed on: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");

                return "Success";
            }
            catch (Exception ex)
            {
                Log("Password Change Error", $"User '{fullName}' (ID:{userId}) — unexpected error during password change | Error: {ex.Message} | {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                return "Error: " + ex.Message;
            }
        }

        private void SendWelcomeEmail(string toEmail, string username, string plainPassword, string fullName)
        {
            try
            {
                string smtpHost = System.Configuration.ConfigurationManager.AppSettings["SenderHost"] ?? "";
                string smtpPort = System.Configuration.ConfigurationManager.AppSettings["SenderPort"] ?? "587";
                string smtpUser = System.Configuration.ConfigurationManager.AppSettings["SenderEmail"] ?? "";
                string smtpPass = (System.Configuration.ConfigurationManager.AppSettings["SenderPassword"] ?? "").Replace(" ", "");
                string fromName = System.Configuration.ConfigurationManager.AppSettings["SenderName"] ?? "ChromExplorer";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
                {
                    Log("Email Skipped", $"SMTP not configured — welcome email not sent to '{toEmail}'");
                    return;
                }

                var msg = new System.Net.Mail.MailMessage();
                msg.From = new System.Net.Mail.MailAddress(smtpUser, fromName);
                msg.To.Add(toEmail);
                msg.Subject = "Welcome to ChromExplorer — Your Account Details";
                msg.IsBodyHtml = false;
                msg.Body =
                    $"Dear {fullName},\r\n\r\n" +
                    $"Your ChromExplorer account has been created.\r\n\r\n" +
                    $"Username : {username}\r\n" +
                    $"Password : {plainPassword}\r\n\r\n" +
                    $"IMPORTANT: You must login and change your password within 2 hours.\r\n" +
                    $"After 2 hours your account will be automatically deactivated.\r\n\r\n" +
                    $"Regards,\r\nChromExplorer Admin";

                var smtp = new System.Net.Mail.SmtpClient(smtpHost, int.Parse(smtpPort))
                {
                    Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };
                smtp.Send(msg);
                Log("Welcome Email Sent", $"Welcome email sent to '{toEmail}' for user '{fullName}'");
            }
            catch (Exception ex)
            {
                Log("Email Error", $"Failed to send welcome email to '{toEmail}': {ex.Message}");
            }
        }

        public void EnsurePasswordExpiryColumn()
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    bool exists = false;
                    using (var cmd = new SQLiteCommand("PRAGMA table_info(PasswordM)", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["name"].ToString() == "expiry_date") { exists = true; break; }
                        }
                    }
                    if (!exists)
                    {
                        using (var cmd = new SQLiteCommand("ALTER TABLE PasswordM ADD COLUMN expiry_date TEXT", conn))
                            cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public bool ResetPassword(int userId, string newPassword)
        {
            try
            {
                string enc = CryptoHelper.Encrypt(newPassword);
                int adminId = 0;
                if (Application.Current.Properties.Contains("UserId"))
                    adminId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE PasswordM SET password=@p WHERE user_id=@uid AND is_active=1";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@p", enc);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                    LogPassTransaction(conn, null, userId, enc, adminId);
                }

                // get full name for audit log
                string fullName = "Unknown";
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT user_fullname FROM UserM WHERE user_id=@uid", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        fullName = cmd.ExecuteScalar()?.ToString() ?? "Unknown";
                    }
                }

                Log("Reset Password", $"Password reset by '{fullName}' | Expires in 90 days");
                return true;
            }
            catch (Exception ex)
            {
                Log("Reset Password Error", ex.Message);
                return false;
            }
        }

        public void ResetAllPasswords()
        {
            EnsureFirstLoginColumns();
            string defaultEnc = CryptoHelper.Encrypt("Pass@12345");
            int successCount = 0;
            var errors = new System.Text.StringBuilder();

            // Step 1 — Bulk reactivate ALL users and reset PasswordM in one shot
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("UPDATE UserM SET is_active=1, is_first_login=0, inserted_date=datetime('now','localtime')", conn))
                        cmd.ExecuteNonQuery();
                    using (var cmd = new SQLiteCommand("UPDATE PasswordM SET password=@p, is_active=1", conn))
                    {
                        cmd.Parameters.AddWithValue("@p", defaultEnc);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bulk reset ERROR: " + ex.Message, "Reset Error");
                return;
            }

            // Step 2 — Per-user pass_transaction (isolated so one failure doesn't stop others)
            var userIds = new List<long>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT user_id FROM UserM", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        userIds.Add(Convert.ToInt64(r["user_id"]));
            }

            foreach (long uid in userIds)
            {
                try
                {
                    using (var conn = new SQLiteConnection(_connectionString))
                    {
                        conn.Open();
                        using (var cmd = new SQLiteCommand("UPDATE Pass_Transaction SET is_active=0 WHERE user_id=@uid", conn))
                        {
                            cmd.Parameters.AddWithValue("@uid", uid);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand(@"INSERT INTO Pass_Transaction (user_id, password, is_active, inserted_by, inserted_date)
                                                             VALUES (@uid, @pass, 1, 0, datetime('now','localtime'))", conn))
                        {
                            cmd.Parameters.AddWithValue("@uid", uid);
                            cmd.Parameters.AddWithValue("@pass", defaultEnc);
                            cmd.ExecuteNonQuery();
                        }
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    errors.AppendLine($"user_id={uid}: {ex.Message}");
                }
            }

            Log("Password Reset", $"Reset done: {successCount}/{userIds.Count} users reset to Pass@12345 | {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
        }

        public void FixPasswordsDirectly()
        {
            try
            {
                string enc = CryptoHelper.Encrypt("Pass@12345");
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    int rows;
                    using (var cmd = new SQLiteCommand("UPDATE PasswordM SET password=@p, is_active=1", conn))
                    {
                        cmd.Parameters.AddWithValue("@p", enc);
                        rows = cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SQLiteCommand("UPDATE UserM SET is_active=1", conn))
                        cmd.ExecuteNonQuery();
                    try
                    {
                        using (var cmd = new SQLiteCommand("UPDATE UserM SET is_first_login=0", conn))
                            cmd.ExecuteNonQuery();
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fix ERROR: " + ex.Message);
            }
        }

        public void ReactivateUser(string username)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(@"UPDATE UserM
                        SET is_active=1, is_first_login=0,
                            inserted_date=datetime('now','localtime')
                        WHERE user_name=@u", conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public List<MenuModel> getmenudata(int roleId)
        {
            List<MenuModel> MenuItems = new List<MenuModel>();

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT m.menu_id, m.menu_name, m.menu_link
                           FROM role_wise_rights_details r
                           INNER JOIN menuM m 
                           ON r.menu_id = m.menu_id 
                           AND m.is_active = 1
                           WHERE r.role_id = @role_id
                           AND r.is_active = 1";

                    SQLiteCommand cmd = new SQLiteCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@role_id", roleId);

                    SQLiteDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        MenuItems.Add(new MenuModel
                        {
                            MenuName = reader["menu_name"].ToString(),
                            ViewName = reader["menu_link"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return MenuItems;
        }
    }

}