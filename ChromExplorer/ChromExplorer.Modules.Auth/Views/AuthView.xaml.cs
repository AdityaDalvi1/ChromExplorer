using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.Auth.ViewModels;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
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
    /// Interaction logic for AuthView.xaml
    /// </summary>
    [Export("AuthView", typeof(AuthView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class AuthView : UserControl
    {
        public AuthView()
        {
            InitializeComponent();
        }

        [Import]
        public IRegionManager RegionManager;

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        AuthViewViewModel ViewModel
        {
            get { return (AuthViewViewModel)this.DataContext; }
            set
            {
                this.DataContext = value;
            }
        }


        private IRegionNavigationJournal _mainJournal = null;
        private static Uri _defaultUri = new Uri("HomeScreenView", UriKind.Relative);
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Uri toNav = _defaultUri;

            if (_mainJournal != null && _mainJournal.CurrentEntry != null)
                toNav = _mainJournal.CurrentEntry.Uri;

            this.RegionManager.RequestNavigate(RegionNames.MainContentArea, toNav);
        }

    }
}
