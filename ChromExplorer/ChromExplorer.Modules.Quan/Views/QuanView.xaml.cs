using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using ChromExplorer.Modules.Quan.ViewModels;

namespace ChromExplorer.Modules.Quan.Views
{
    /// <summary>
    /// Interaction logic for QuanView.xaml
    /// </summary>
    [Export("QuanView", typeof(QuanView))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class QuanView : UserControl
    {
        public QuanView()
        {
            InitializeComponent();
        }


        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        QuanViewViewModel ViewModel
        {
            set
            {
                this.DataContext = value;
            }
        }


        private void DocumentViewerDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //DocumentViewer.FitToWidth();

        }
    }


}
