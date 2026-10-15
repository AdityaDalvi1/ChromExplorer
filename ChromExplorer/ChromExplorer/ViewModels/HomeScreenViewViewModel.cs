using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Windows;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Interfaces;
using log4net;
using Microsoft.Practices.Prism.Regions;

namespace ChromExplorer.ViewModels
{
    [Export(typeof(HomeScreenViewViewModel))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class HomeScreenViewViewModel : IPartImportsSatisfiedNotification
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static Uri _defaultUri = new Uri("ChromatorgramView", UriKind.Relative);
        private static Uri _licenseUri = new Uri("LicensingView", UriKind.Relative);
        private static Uri _homeUri = new Uri("HomeScreenView", UriKind.Relative);

        private static Uri _authUri = new Uri("AuthView", UriKind.Relative);

        [Import]
        private IRegionManager _regionManager;

        [Import]
        private ChromDataProviderFactory _factory;

        [ImportingConstructor]
        public HomeScreenViewViewModel()
        {
            LogoSource = "../Resources/MTE30.png";
            LogoOpacity = 0.85;
            //LogoSource = "../Resources/IonTrap0.png";
            //LogoOpacity = 0.85;
            //LogoSource = "../Resources/sesi_transparent.png";
            //LogoOpacity = 0.75;
        }

        public string AppTitle { get { return CEVersionInfo.ProgramTitle; } }
        public string LogoSource { get; set; }
        public int LogoMargin { get; set; }
        public double LogoOpacity { get; set; }

        #region Implementation of IPartImportsSatisfiedNotification

        public void OnImportsSatisfied()
        {

            _regionManager.RequestNavigate(RegionNames.MainContentArea, _authUri);

            //if (!_licensingService.IsValid)
            //{
            //    _regionManager.RequestNavigate(RegionNames.MainContentArea, _licenseUri);
            //}

            //_regionManager.RequestNavigate(RegionNames.MainContentArea, _homeUri);
            string filename = null;
            var args = Application.Current.Properties["Args"] as string[];
            
            if(args != null && args.Length > 0)
            {
                filename = args[0];
            }
            else
            {
                try
                {
                   
                    if(AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null)
                        args = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;

                    if (args != null && args.Length > 0)
                    {
                        log.DebugFormat("Click Once Activation data:");
                        foreach (var s in args)
                        {
                            log.Debug(s);
                        }
                        filename = args[0];
                    }
                }
                catch
                {
                }
            }

            if (!string.IsNullOrEmpty(filename))
            {
                FileInfo fi = null;
                
                try
                {
                    fi = new FileInfo(filename);
                }
                catch
                {
                    Uri uri;
                    if(Uri.TryCreate(filename, UriKind.RelativeOrAbsolute, out uri))
                    {
                        if (uri.IsFile)
                        {
                            string fn = Path.GetFullPath(uri.LocalPath);
                            fi = new FileInfo(uri.LocalPath);
                        }
                    }
                }

                if(fi == null)
                    return;

                if (!fi.Exists)
                {

                    return;
                }

                foreach (var lazy in _factory.Providers)
                {
                    try
                    {
                        var provider = lazy.Value;
                        if (provider.Load(fi.FullName))
                        {
                            _factory.Current = provider;
                            log.Info("Provider found");
                            _regionManager.RequestNavigate(RegionNames.MainContentArea, _defaultUri);
                            break;
                        }
                        else
                        {
                            log.Info("Format mistmath");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Info("Format mistmath", ex);
                    }
                }
            }
        }

        #endregion
    }
}
