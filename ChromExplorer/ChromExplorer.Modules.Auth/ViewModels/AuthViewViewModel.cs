using ChromExplorer.Infrastructure;
using log4net;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.Modules.Auth.ViewModels
{
    [Export(typeof(AuthViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class AuthViewViewModel : IPartImportsSatisfiedNotification, INavigationAware
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Uri DefaultUri = new Uri("HomeScreenView", UriKind.Relative);


        private readonly IRegionManager _regionManager;

        [ImportingConstructor]
        public AuthViewViewModel(IRegionManager regionManager)
        {
            log.Info("AuthViewViewModel created.");

            _regionManager = regionManager;

            Username = "admin";
            Password = "password";

            LoginCommand = new DelegateCommand<object>(OnLogin, OnLoginCanExecute);
            
        }

        private bool OnLoginCanExecute(object arg)
        {
            return true;

            ///TODO: Implement logic to determine if login can be executed (e.g., check if username and password are not empty)
        }

        private void OnLogin(object obj)
        {
            log.Info($"Login attempted with Username: {Username} and Password: {Password}");

            ///TODO Check here, if OK navigete further
            ///

            _regionManager.RequestNavigate(RegionNames.MainContentArea, DefaultUri);

        }

        public string Username { get; set; }
        public string Password { get; set; }

        public DelegateCommand<object> LoginCommand { get; private set; }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        public void OnImportsSatisfied()
        {
            log.Info("OnImportsSatisfied called.");
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            log.Info("OnNavigatedFrom called.");
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            log.Info("OnNavigatedTo called");
        }
    }
}
