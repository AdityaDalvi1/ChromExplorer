using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Regions;
using System.Reflection;

namespace ChromExplorer.Modules.ModasGrpc.Views
{
    [Export("ModasGrpcDashboardView", typeof(ModasGrpcDashboardView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ModasGrpcDashboardView : System.Windows.Controls.UserControl, INavigationAware
    {
        private readonly MtRemoteGrpcDashboardViewModel _viewModel;
        private MtRemoteGrpcDashboardPanel _panel;

        [ImportingConstructor]
        public ModasGrpcDashboardView(MtRemoteGrpcDashboardViewModel vm)
        {
            _viewModel = vm;
            InitializeComponent();
        }

        void INavigationAware.OnNavigatedTo(NavigationContext navigationContext)
        {
            EnsurePanelAndViewModel();
            BindPanel();
        }

        bool INavigationAware.IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext navigationContext)
        {
            TearDown();
        }

        private void TearDown()
        {
            try
            {
                _panel?.Bind(null);
            }
            catch
            {
                // ignore teardown errors
            }

            try
            {
                //_viewModel?.Dispose();
            }
            catch
            {
                // ignore teardown errors
            }
            finally
            {
                //_viewModel = null;
            }
        }

        private void EnsurePanelAndViewModel()
        {
            if (_panel != null && _viewModel != null)
            {
                return;
            }

            if (_panel == null)
                _panel = new MtRemoteGrpcDashboardPanel();

            //if (_viewModel == null)
            //    _viewModel = new MtRemoteGrpcDashboardViewModel();

            if (Host != null)
            {
                Host.Children.Clear();
                Host.Children.Add(_panel);
            }
        }

        private void BindPanel()
        {
            if (_panel == null || _viewModel == null)
            {
                return;
            }

            _panel.Bind(_viewModel);
        }
    }
}
