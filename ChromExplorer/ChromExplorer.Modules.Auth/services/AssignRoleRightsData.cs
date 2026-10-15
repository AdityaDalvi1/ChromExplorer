using ChromExplorer.Modules.Auth.models;
using ChromExplorer.Modules.Auth.Models;
using ChromExplorer.Modules.Auth.Services;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static ChromExplorer.Modules.Auth.Models.User;

namespace ChromExplorer.Modules.Auth.services
{
    public class AssignRoleRightsData
    {
        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        // Friendly display overrides for specific menu_name values. The underlying DB value
        // can't change (it's matched literally by rights-check code elsewhere), so this only
        // affects what's shown in the Assign Rights grids/dropdown.
        private static readonly Dictionary<string, string> FriendlyMenuNames = new Dictionary<string, string>
        {
            { "MT30Controlebutton", "MT30 Server" },
            { "AuditorReport", "ViewAuditReport" },
            { "ChromatogramGenerateReport", "Chromatogram - Generate Report" },
            { "AuditorExportPdf", "Auditor Report - Export PDF" }
        };

        // Only these menu_name values should be selectable in the Add Assigned Rights dropdown.
        // Everything else in menuM (nav-icon-only entries, Syringe/Ms toolbuttons, etc.) is left
        // out of this list on purpose, per product decision, but stays untouched in the DB.
        private static readonly HashSet<string> AssignableMenuNames = new HashSet<string>
        {
            "ChromExplorer.Modules.Auth.Views.UserDetails",
            "ChromExplorer.Modules.Auth.Views.ViewAssignRole",
            "ChromExplorer.Modules.Auth.Views.ViewAssignRights",
            "ChromExplorer.Modules.Auth.Views.ReviewSavedFiles",
            "DataProviderFileLoad",
            "DataProcessingOnChromatogram",
            "QuanData",
            "DataProcessingSettings",
            "MT30Controlebutton",
            "AuditorReport",
            "ChromatogramGenerateReport",
            "AuditorExportPdf"
        };

        // Some menu_name values are stored as a fully-qualified type name (e.g.
        // "ChromExplorer.Modules.Auth.Views.UserDetails") because the rights-check code
        // matches against typeof(X).FullName. That value can't change without breaking
        // access checks, so this only shortens it for display in the Assign Rights grids.
        private static string ShortenMenuName(string menuName)
        {
            if (string.IsNullOrEmpty(menuName))
                return menuName;

            if (FriendlyMenuNames.TryGetValue(menuName, out string friendlyName))
                return friendlyName;

            int lastDot = menuName.LastIndexOf('.');
            return lastDot >= 0 && lastDot < menuName.Length - 1
                ? menuName.Substring(lastDot + 1)
                : menuName;
        }

        public List<AssignRoleData> GetAssignedRoleList()
        {
            List<AssignRoleData> listdata = new List<AssignRoleData>();

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();



                    string sql = @"SELECT
ur.userwise_role_detail_id,
                            u.user_id,
                            u.user_fullname,
                            r.role_description as role_name,
                            ur.is_active
                           FROM user_wise_role_details ur
                           INNER JOIN UserM u ON u.user_id = ur.user_id AND u.is_active = 1
                           INNER JOIN roleM r ON r.role_id = ur.role_id AND r.is_active = 1;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            int srNo = 1;

                            while (reader.Read())
                            {
                                listdata.Add(new AssignRoleData
                                {
                                    SrNo = srNo++,
                                    userrole_id = reader["userwise_role_detail_id"].ToString(),
                                    UserName = reader["user_fullname"].ToString(),
                                    RoleName = reader["role_name"].ToString(),
                                    IsActive = reader["is_active"] == DBNull.Value || Convert.ToInt32(reader["is_active"]) == 1
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching user. Database Connection Failed.", ex);
            }

            return listdata;
        }

        public List<DropDownData> GetRoles()
        {
            List<DropDownData> listdata = new List<DropDownData>();
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();



                    string sql = @"SELECT 
                            role_id as Id,
                            role_description as Text
                           FROM roleM
                           WHERE is_active = 1;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            int srNo = 1;

                            while (reader.Read())
                            {
                                listdata.Add(new DropDownData
                                {

                                    Id = reader["Id"].ToString(),
                                    Text = reader["Text"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return listdata;
        }

        public List<DropDownData> GetUsername()
        {
            List<DropDownData> listdata = new List<DropDownData>();
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();



                    string sql = @"SELECT 
                                 user_id as Id,
                                 user_name as Text
                           FROM userM
                           WHERE is_active = 1;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            int srNo = 1;

                            while (reader.Read())
                            {
                                listdata.Add(new DropDownData
                                {

                                    Id = reader["Id"].ToString(),
                                    Text = reader["Text"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return listdata;
        }
        public List<AssignRoleData> SaveAssignRole(AssignRoleData assignRolesData, string reason = "")
        {
            List<AssignRoleData> list = new List<AssignRoleData>();

            try
            {
                int userId = 0;

                if (Application.Current.Properties["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    string checkQuery = "SELECT COUNT(*) FROM user_wise_role_details WHERE is_active = 1 AND user_id = @user_id";

                    using (var checkCmd = new SQLiteCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@user_id", assignRolesData.UserName);

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            // ✅ UPDATE user_wise_role_details
                            string updateQuery = @"UPDATE user_wise_role_details
                                   SET role_id = @role_id,
                                       updated_by = @userId,
                                       updated_date = datetime('now')
                                   WHERE user_id = @user_id";

                            using (var updateCmd = new SQLiteCommand(updateQuery, con))
                            {
                                updateCmd.Parameters.AddWithValue("@user_id", assignRolesData.UserName);
                                updateCmd.Parameters.AddWithValue("@role_id", assignRolesData.RoleName);
                                updateCmd.Parameters.AddWithValue("@userId", userId);

                                updateCmd.ExecuteNonQuery();
                            }

                            // ✅ ALSO UPDATE userM
                            UpdateUserM(con, assignRolesData);

                            assignRolesData.Status = "Updated";

                            new AuthService().Log("Assign Role Updated",
                                $"Role '{assignRolesData.RoleName}' assigned to user '{assignRolesData.UserName}' updated | Reason: {reason}");
                        }
                        else
                        {
                            // ✅ INSERT user_wise_role_details
                            string insertQuery = @"INSERT INTO user_wise_role_details
                                   (user_id, role_id, is_active, inserted_by, inserted_date)
                                   VALUES (@user_id, @role_id, 1, @userId, datetime('now'))";

                            using (var insertCmd = new SQLiteCommand(insertQuery, con))
                            {
                                insertCmd.Parameters.AddWithValue("@user_id", assignRolesData.UserName);
                                insertCmd.Parameters.AddWithValue("@role_id", assignRolesData.RoleName);
                                insertCmd.Parameters.AddWithValue("@userId", userId);

                                int rows = insertCmd.ExecuteNonQuery();

                                // ✅ ALSO UPDATE userM
                                if (rows > 0)
                                    UpdateUserM(con, assignRolesData);

                                assignRolesData.Status = rows > 0 ? "Inserted" : "Failed";

                                if (rows > 0)
                                {
                                    new AuthService().Log("Assign Role Inserted",
                                        $"Role '{assignRolesData.RoleName}' assigned to user '{assignRolesData.UserName}' | Reason: {reason}");
                                }
                            }
                        }
                    }
                }

                list.Add(assignRolesData);
            }
            catch (Exception ex)
            {
                assignRolesData.Status = "Error";
                list.Add(assignRolesData);
                MessageBox.Show(ex.Message);
            }

            return list;
        }

        private void UpdateUserM(SQLiteConnection con, AssignRoleData data)
        {
            string updateUserM = @"UPDATE userM 
                           SET role_id = @role_id ,
                                 updated_by = @user_id,
                                 updated_date = datetime('now') 
                           WHERE user_id = @user_id";

            using (var cmd = new SQLiteCommand(updateUserM, con))
            {
                cmd.Parameters.AddWithValue("@user_id", data.UserName);
                cmd.Parameters.AddWithValue("@role_id", data.RoleName);

                cmd.ExecuteNonQuery();
            }
        }

        //public List<AssignRoleData> SaveAssignRole(AssignRoleData assignRolesData)
        //{
        //    List<AssignRoleData> list = new List<AssignRoleData>();

        //    try
        //    {
        //        int userId = 0;

        //        if (Application.Current.Properties["UserId"] != null)
        //            userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

        //        using (var con = new SQLiteConnection(_connectionString))
        //        {
        //            con.Open();

        //            string checkQuery = "SELECT COUNT(*) FROM user_wise_role_details WHERE is_active = 1 and user_id = @user_id";

        //            using (var checkCmd = new SQLiteCommand(checkQuery, con))
        //            {
        //                checkCmd.Parameters.AddWithValue("@user_id", assignRolesData.UserName);
        //                checkCmd.Parameters.AddWithValue("@userId", userId);

        //                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

        //                if (count > 0)
        //                {
        //                    string updateQuery = @"UPDATE user_wise_role_details
        //                                   SET role_id = @role_id,
        //                                        updated_by = @userId,
        //                                        updated_date = datetime('now')
        //                                   WHERE user_id = @user_id";

        //                    using (var updateCmd = new SQLiteCommand(updateQuery, con))
        //                    {
        //                        updateCmd.Parameters.AddWithValue("@user_id", assignRolesData.UserName);
        //                        updateCmd.Parameters.AddWithValue("@role_id", assignRolesData.RoleName);
        //                        updateCmd.Parameters.AddWithValue("@userId", userId);

        //                        updateCmd.ExecuteNonQuery();

        //                        assignRolesData.Status = "Updated";
        //                    }
        //                }
        //                else
        //                {
        //                    string insertQuery = @"INSERT INTO user_wise_role_details (user_id, role_id,is_active, inserted_by,inserted_date)
        //                                   VALUES (@user_id, @role_id,1,@userId,datetime('now'))";

        //                    using (var insertCmd = new SQLiteCommand(insertQuery, con))
        //                    {
        //                        insertCmd.Parameters.AddWithValue("@user_id", assignRolesData.UserName);
        //                        insertCmd.Parameters.AddWithValue("@role_id", assignRolesData.RoleName);
        //                        insertCmd.Parameters.AddWithValue("@userId", userId);

        //                        int rows = insertCmd.ExecuteNonQuery();

        //                        assignRolesData.Status = rows > 0 ? "Inserted" : "Failed";
        //                    }
        //                }
        //            }
        //        }

        //        list.Add(assignRolesData);
        //    }
        //    catch (Exception ex)
        //    {
        //        assignRolesData.Status = "Error";
        //        list.Add(assignRolesData);
        //        MessageBox.Show(ex.Message);
        //    }

        //    return list;
        //}

        public bool DeleteAssignRole(AssignRoleData row, string reason = "")
        {
            try
            {
                int userId = 0;

                if (Application.Current.Properties["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    string query = @"UPDATE user_wise_role_details
                             SET is_active = 0 ,
                                 updated_by = @userId,
                                 updated_date = datetime('now')
                             WHERE userwise_role_detail_id = @id";

                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", row.userrole_id);
                        cmd.Parameters.AddWithValue("@userId", userId);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            new AuthService().Log("Assign Role Deleted",
                                $"Assigned role '{row.RoleName}' for user '{row.UserName}' deleted | Reason: {reason}");
                        }

                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public bool ActivateAssignRole(AssignRoleData row, string reason = "")
        {
            try
            {
                int userId = 0;

                if (Application.Current.Properties["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    string query = @"UPDATE user_wise_role_details
                             SET is_active = 1 ,
                                 updated_by = @userId,
                                 updated_date = datetime('now')
                             WHERE userwise_role_detail_id = @id";

                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", row.userrole_id);
                        cmd.Parameters.AddWithValue("@userId", userId);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            new AuthService().Log("Assign Role Activated",
                                $"Assigned role '{row.RoleName}' for user '{row.UserName}' activated | Reason: {reason}");
                        }

                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public List<AssignRoleData> GetAssignedRoleDetailsById(string id)
        {
            List<AssignRoleData> list = new List<AssignRoleData>();

            try
            {
                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    string query = @"SELECT 
                                ur.userwise_role_detail_id,
                                u.user_id,
                                u.user_fullname,
                                r.role_id,
                                r.role_description
                             FROM user_wise_role_details ur
                             LEFT JOIN userM u ON u.user_id = ur.user_id
                             LEFT JOIN roleM r ON r.role_id = ur.role_id
                             WHERE ur.userwise_role_detail_id = @id 
                             AND ur.is_active = 1";

                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new AssignRoleData
                                {
                                    userrole_id = reader["userwise_role_detail_id"].ToString(),
                                    UserName = reader["user_id"].ToString(),
                                    RoleName = reader["role_id"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return list;
        }


        public object DeleteAssignRights(AssignRoleRightsDO row, string reason = "")
        {
            try
            {
                int userId = 0;

                if (Application.Current.Properties["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    string query = @"UPDATE role_wise_rights_details
                             SET is_active = 0 ,
                                 updated_by = @userId,
                                 updated_date = datetime('now')
                             WHERE rolewise_right_detail_id = @id";

                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", row.user_rights_id);
                        cmd.Parameters.AddWithValue("@userId", userId);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            new AuthService().Log("Assign Rights Deleted",
                                $"Assigned right for menu '{row.MenuName}' and role '{row.RoleName}' deleted | Reason: {reason}");
                        }

                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public bool ActivateAssignRights(AssignRoleRightsDO row, string reason = "")
        {
            try
            {
                int userId = 0;

                if (Application.Current.Properties["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    string query = @"UPDATE role_wise_rights_details
                             SET is_active = 1 ,
                                 updated_by = @userId,
                                 updated_date = datetime('now')
                             WHERE rolewise_right_detail_id = @id";

                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", row.user_rights_id);
                        cmd.Parameters.AddWithValue("@userId", userId);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            new AuthService().Log("Assign Rights Activated",
                                $"Assigned right for menu '{row.MenuName}' and role '{row.RoleName}' activated | Reason: {reason}");
                        }

                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public List<AssignRoleRightsDO> GetAssignedRightsDetailsById(string id)
        {
            List<AssignRoleRightsDO> list = new List<AssignRoleRightsDO>();

            try
            {
                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    string query = @"SELECT 
rd.rolewise_right_detail_id,
m.menu_name,
rd.menu_id,
rd.role_id,
                            r.role_description as role_name
                           FROM role_wise_rights_details rd 
                           LEFT JOIN menuM m ON m.menu_id = rd.menu_id AND m.is_active = 1
                           LEFT JOIN roleM r ON r.role_id = rd.role_id AND r.is_active = 1
                           WHERE rd.is_active = 1 and rd.rolewise_right_detail_id = @id";

                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new AssignRoleRightsDO
                                {
                                    user_rights_id = reader["rolewise_right_detail_id"].ToString(),
                                    MenuName = reader["menu_id"].ToString(),
                                    RoleName = reader["role_id"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return list;
        }

        public List<AssignRoleRightsDO> GetAssignedRightsDetails()
        {
            List<AssignRoleRightsDO> listdata = new List<AssignRoleRightsDO>();

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();



                    string sql = @"SELECT
rd.rolewise_right_detail_id,
m.menu_name,
                            r.role_description as role_name,
                            rd.is_active
                           FROM role_wise_rights_details rd
                           INNER JOIN menuM m ON m.menu_id = rd.menu_id AND m.is_active = 1
                           INNER JOIN roleM r ON r.role_id = rd.role_id AND r.is_active = 1;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            int srNo = 1;

                            while (reader.Read())
                            {
                                listdata.Add(new AssignRoleRightsDO
                                {
                                    SrNo = srNo++,
                                    user_rights_id = reader["rolewise_right_detail_id"].ToString(),
                                    MenuName = ShortenMenuName(reader["menu_name"].ToString()),
                                    RoleName = reader["role_name"].ToString(),
                                    IsActive = reader["is_active"] == DBNull.Value || Convert.ToInt32(reader["is_active"]) == 1
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching user. Database Connection Failed.", ex);
            }

            return listdata;
        }

        public List<DropDownData> GetMenu()
        {
            List<DropDownData> listdata = new List<DropDownData>();
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();



                    string sql = @"SELECT 
                                 menu_id as Id,
                                 menu_name as Text
                           FROM menuM
                           WHERE is_active = 1;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            int srNo = 1;

                            while (reader.Read())
                            {
                                string rawMenuName = reader["Text"].ToString();

                                if (!AssignableMenuNames.Contains(rawMenuName))
                                    continue;

                                listdata.Add(new DropDownData
                                {

                                    Id = reader["Id"].ToString(),
                                    Text = ShortenMenuName(rawMenuName)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return listdata;
        }

        private static string GetMenuNameById(SQLiteConnection conn, string menuId)
        {
            using (var cmd = new SQLiteCommand("SELECT menu_name FROM menuM WHERE menu_id = @menu_id", conn))
            {
                cmd.Parameters.AddWithValue("@menu_id", menuId);
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? menuId : ShortenMenuName(result.ToString());
            }
        }

        private static string GetRoleNameById(SQLiteConnection conn, string roleId)
        {
            using (var cmd = new SQLiteCommand("SELECT role_description FROM roleM WHERE role_id = @role_id", conn))
            {
                cmd.Parameters.AddWithValue("@role_id", roleId);
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? roleId : result.ToString();
            }
        }

        public List<AssignRoleRightsDO> SaveAssignRights(AssignRoleRightsDO assignRoleRightsDO, string reason = "")
        {
            List<AssignRoleRightsDO> list = new List<AssignRoleRightsDO>();

            try
            {
                int userId = 0;

                if (Application.Current.Properties["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    string checkQuery = "SELECT COUNT(*) FROM role_wise_rights_details WHERE is_active = 1 and role_id = @role_id and menu_id = @menu_id";

                    using (var checkCmd = new SQLiteCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@role_id", assignRoleRightsDO.RoleName);
                        checkCmd.Parameters.AddWithValue("@menu_id", assignRoleRightsDO.MenuName);

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            assignRoleRightsDO.Status = "Already Assigned";
                        }
                        else
                        {
                            string insertQuery = @"INSERT INTO role_wise_rights_details ( role_id,menu_id,sub_menu_id,is_active, inserted_by,inserted_date)
                                           VALUES (@role_id, @menu_id,0,1,@userId,datetime('now'))";

                            using (var insertCmd = new SQLiteCommand(insertQuery, con))
                            {
                                insertCmd.Parameters.AddWithValue("@menu_id", assignRoleRightsDO.MenuName);
                                insertCmd.Parameters.AddWithValue("@role_id", assignRoleRightsDO.RoleName);
                                insertCmd.Parameters.AddWithValue("@userId", userId);

                                int rows = insertCmd.ExecuteNonQuery();

                                assignRoleRightsDO.Status = rows > 0 ? "Inserted" : "Failed";

                                if (rows > 0)
                                {
                                    string menuNameText = GetMenuNameById(con, assignRoleRightsDO.MenuName);
                                    string roleNameText = GetRoleNameById(con, assignRoleRightsDO.RoleName);

                                    new AuthService().Log("Assign Rights Inserted",
                                        $"Menu '{menuNameText}' assigned to role '{roleNameText}' | Reason: {reason}");
                                }
                            }
                        }
                    }
                }

                list.Add(assignRoleRightsDO);
            }
            catch (Exception ex)
            {
                assignRoleRightsDO.Status = "Error";
                list.Add(assignRoleRightsDO);
                MessageBox.Show(ex.Message);
            }

            return list;
        }
    }
}
