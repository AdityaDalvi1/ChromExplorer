using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using ChromExplorer.Modules.ModasChromProvider.ViewModels;
using Microsoft.Practices.Prism.Regions;

namespace ChromExplorer.Modules.ModasChromProvider.Views
{
    [Export("ModasImportView", typeof(ModasImportView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ModasImportView : UserControl, INavigationAware
    {
        private ModasImportViewViewModel _viewModel;

        public ModasImportView()
        {
            InitializeComponent();
        }

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        ModasImportViewViewModel ViewModel
        {
            set
            {
                _viewModel = value;
                this.DataContext = value;
            }
        }

        // Re-sync File/Points/PPD/Mass-range every time this screen is actually shown, rather
        // than relying only on catching a live ChromatogramChangedEvent — the ViewModel is a
        // shared singleton that may not exist yet when a file auto-loads before Import is ever
        // opened, so it needs to actively pull current state here instead of just reacting.
        void INavigationAware.OnNavigatedTo(NavigationContext navigationContext)
        {
            _viewModel?.RefreshFromCurrentChromatogram();
        }

        bool INavigationAware.IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext navigationContext)
        {
        }
    }
}
