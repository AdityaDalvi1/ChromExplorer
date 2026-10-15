using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.Licensing.ViewModels;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
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

namespace ChromExplorer.Modules.Licensing.Views
{
    /// <summary>
    /// Interaction logic for LicensingView.xaml
    /// </summary>
    [Export("LicensingView", typeof(LicensingView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class LicensingView : UserControl, IPartImportsSatisfiedNotification
    {
        public LicensingView()
        {
            InitializeComponent();
//            lv = new QLM.LicenseValidator();
//
//            bool needsActivation = false;
//            string errorMsg = string.Empty;
//            string licenseMessage = string.Empty;
//
//            bool licenseValid = lv.ValidateLicenseAtStartup(Environment.MachineName, ref needsActivation, ref errorMsg);
//
//            QlmActivationControl.QlmCommunicationEncryptionKey = "{afcab787-770c-43ad-93ec-0c42a1bfad37}";
//            QlmActivationControl.QlmProductID = 7;
//            QlmActivationControl.QlmProductName = "ChromExplorer";
//            QlmActivationControl.QlmMajorVersion = 2;
//            QlmActivationControl.QlmMinorVersion = 0;
//            QlmActivationControl.QlmEncryptionKey = "";
//            QlmActivationControl.QlmGUID = "{4f2e062c-c5d5-47e7-b8ba-dcd8f1c920cf}";

            //(7, "ChromExplorer", 2, 0, "", "{4f2e062c-c5d5-47e7-b8ba-dcd8f1c920cf}"
            //this.QlmProductID, this.QlmProductName, this.QlmMajorVersion, this.QlmMinorVersion, this.QlmEncryptionKey, this.QlmGUID
        }

        [Import]
        public IRegionManager RegionManager;

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        LicensingViewViewModel ViewModel
        {
            get { return (LicensingViewViewModel)this.DataContext; }
            set
            {
                this.DataContext = value;
            }
        }

        private static Uri _defaultUri = new Uri("HomeScreenView", UriKind.Relative);
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var popupRegion = this.RegionManager.Regions[RegionNames.OverContentRegion];

            if(popupRegion != null && popupRegion.ActiveViews.Contains(this))
                popupRegion.Deactivate(this);
            else
                this.RegionManager.RequestNavigate(RegionNames.MainContentArea, _defaultUri);

        }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {

        }

        
    }
}
