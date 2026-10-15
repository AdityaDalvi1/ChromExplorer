using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.Syringe.Settings;
using ChromExplorer.Syringe;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Linq.Expressions;
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

namespace ChromExplorer.Modules.Syringe.Views
{
    /// <summary>
    /// Interaction logic for SyringeStartToolbutton.xaml
    /// </summary>
    /// 

    //[ViewExport(RegionName = RegionNames.LeftToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("10")]
    public partial class SyringeStartToolbutton : UserControl
    {
        private readonly ISyringeControl _syringeControl;

        [ImportingConstructor]
        public SyringeStartToolbutton(ISyringeControl syringeControl)
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
                        cmd.Parameters.AddWithValue("@page_name", "SyringeStartToolbutton");

                        long count = (long)cmd.ExecuteScalar();
                        hasRights = count > 0;
                    }
                }

                // 3️⃣ if no rights
                if (!hasRights)
                {
                    MessageBox.Show("You are not authorized to start the syringe.",
                        "Access Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    if (!_syringeControl.IsConnected)
                        _syringeControl.Connect();

                    if (_syringeControl.IsConnected || !_syringeControl.IsRunning)
                        _syringeControl.Start(SyringeModuleSettings.Default.SyringeDiameter, SyringeModuleSettings.Default.SyringeRate, SyringeModuleSettings.Default.Volume);

                    Log("Syringe Start", "Syringe Started");

                }
            }
            catch (Exception ex)
            {
                Log("Syringe Start", ex.Message);
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
