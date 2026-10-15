using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using ChromExplorer.Modules.Help.ViewModels;

namespace ChromExplorer.Modules.Help.Views
{
    /// <summary>
    /// Interaction logic for XpsHelpView.xaml
    /// </summary>
    [Export("XpsHelpView", typeof(XpsHelpView))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class XpsHelpView : UserControl
    {
        public XpsHelpView()
        {
            InitializeComponent();
        }

        
        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        XmlHelpViewViewModel ViewModel
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
