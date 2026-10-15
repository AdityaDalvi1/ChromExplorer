using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.Syringe.ViewModels;
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

namespace ChromExplorer.Modules.Syringe.Views
{
    /// <summary>
    /// Interaction logic for SyringeControlView.xaml
    /// </summary>
    [Export("SyringeControlView", typeof(SyringeControlView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class SyringeControlView : UserControl, IPartImportsSatisfiedNotification
    {
        public SyringeControlView()
        {
            InitializeComponent();
        }

        [Import]
        public IRegionManager RegionManager;

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        SyringeControlViewViewModel ViewModel
        {
            get { return (SyringeControlViewViewModel)this.DataContext; }
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

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            var mainRegion = this.RegionManager.Regions[RegionNames.MainContentArea];

            if (mainRegion != null && mainRegion.NavigationService != null)
            {
                _mainJournal = mainRegion.NavigationService.Journal;
            }
        }
    }
}
