using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using ChromExplorer.Modules.Auth.Services;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ChromExplorer.Modules.Auth.Views
{
    /// <summary>
    /// Interaction logic for LogoutButton.xaml
    /// </summary>
    /// 
    [ViewExport(RegionName = RegionNames.RightToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("5")]
    public partial class LogoutButton : UserControl
    {
        public LogoutButton()
        {
            InitializeComponent();
        }
        [Import]
        public IRegionManager RegionManager { get; set; }
        private void logoutuser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult result = MessageBox.Show(
                    "Are you sure you want to logout?",
                    "Confirm Logout",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Ends only this session and returns to the login window —
                    // the application process itself keeps running (handled by App.xaml.cs,
                    // which subscribes to SessionManager.LogoutRequested).
                    ChromExplorer.Infrastructure.Services.SessionManager.RequestLogout(isAutoLogout: false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        //private void logoutuser_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        AuthService auth = new AuthService();
        //        auth.Log("LOGOUT", "User logged out from system");

        //        Application.Current.Properties.Clear();

        //        Application.Current.Shutdown();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString());
        //    }
        //}
    }

}
