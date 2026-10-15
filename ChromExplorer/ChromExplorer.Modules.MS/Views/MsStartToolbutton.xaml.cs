using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.Ms.Services;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
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

namespace ChromExplorer.Modules.Ms.Views
{
    /// <summary>
    /// Interaction logic for MsStartToolbutton.xaml
    /// </summary>
    /// 

    //[ViewExport(RegionName = RegionNames.RightToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("10")]
    public partial class MsStartToolbutton : UserControl
    {
        private readonly MsControlService _syringeControl;

        [ImportingConstructor]
        public MsStartToolbutton(MsControlService syringeControl)
        {
            InitializeComponent();
            _syringeControl = syringeControl;
        }
        private readonly string _connectionString = @"Data Source=source.db3;Version=3;";
        //private readonly string _connectionString = @"// [MERGED-REMOVED] Data Source=D:\chromatograpy 23 feb 2026\chromexplorer-master 21-02 (1)\chromexplorer-master (2) (1)\chromexplorer-master\ChromExplorer\source.db3;Version=3;";
        private void BtnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Application.Current.Properties.Contains("RoleId"))
                {
                    MessageBox.Show("Session expired. Please login again.");
                    return;
                }

                int roleId = Convert.ToInt32(Application.Current.Properties["RoleId"]);
                bool hasRights = false;

                // 2️⃣ check permission directly from DB
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
                        cmd.Parameters.AddWithValue("@page_name", "MsStartToolbutton");

                        long count = (long)cmd.ExecuteScalar();
                        hasRights = count > 0;
                    }
                }

                // 3️⃣ if no rights
                if (!hasRights)
                {
                    MessageBox.Show("You are not authorized to open this page.",
                        "Access Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    _syringeControl.Start();
                    Log("MS Syringe Start", "MS Syringe Started");

                }
            }
            catch (Exception ex)
            {
                Log("MS Syringe Start", ex.Message);
            }
        }

        public void Log(string action, string description)
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
            catch (Exception ex)
            {
                MessageBox.Show("Log Error: " + ex.Message);
            }
        }
    }
}
