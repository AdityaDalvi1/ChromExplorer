using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using System.Windows.Input;
using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using ChromExplorer.Modules.Status.ViewModels;

namespace ChromExplorer.Modules.Status.Views
{
    /// <summary>
    /// Interaction logic for StatusBarView.xaml
    /// </summary>
    [ViewExport(RegionName = RegionNames.StatusBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class StatusBarView : UserControl
    {
        public StatusBarView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the ViewModel.
        /// </summary>
        /// <remarks>
        /// This set-only property is annotated with the <see cref="ImportAttribute"/> so it is injected by MEF with
        /// the appropriate view model.
        /// </remarks>
        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        StatusBarViewModel ViewModel
        {
            set
            {
                this.DataContext = value;
            }
        }

        private void performancePanel_MouseEnter(object sender, MouseEventArgs e)
        {
            performancePopup.IsOpen = true;
        }

        private void performancePanel_MouseLeave(object sender, MouseEventArgs e)
        {
            performancePopup.IsOpen = false;
        }

    }
}
