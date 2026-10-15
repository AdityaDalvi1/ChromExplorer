using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
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

namespace ChromExplorer.Modules.Auth.Views
{
    /// <summary>
    /// Interaction logic for AuditorReportButton.xaml
    /// </summary>
    /// 
    [ViewExport(RegionName = RegionNames.RightToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("5")]
    public partial class AuditorReportButton : UserControl
    {
        public AuditorReportButton()
        {
            InitializeComponent();
        }
        [Import]
        public IRegionManager RegionManager { get; set; }

        //private readonly string _connectionString = @"Data Source=D:\NewChromExplorer\ChromExplorer\source.db3;Version=3;";

        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";
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

                // 2?? check permission directly from DB
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
                        cmd.Parameters.AddWithValue("@page_name", "AuditorReport");

                        long count = (long)cmd.ExecuteScalar();
                        hasRights = count > 0;
                    }
                }

                // 3?? if no rights
                if (!hasRights)
                {
                    MessageBox.Show("You are not authorized to open this report data.",
                        "Access Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    var uri = new Uri("AuditorReport", UriKind.Relative);
                    RegionManager.RequestNavigate(RegionNames.OverContentRegion, uri);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
