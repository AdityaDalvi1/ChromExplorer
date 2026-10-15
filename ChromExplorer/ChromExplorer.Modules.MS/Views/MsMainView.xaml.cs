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
using ChromExplorer.Modules.Ms.ViewModels;

namespace ChromExplorer.Modules.Ms.Views
{
    /// <summary>
    /// Interaction logic for MsMainView.xaml
    /// </summary>
    ///


    [Export("MsMainView", typeof(MsMainView))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class MsMainView : UserControl
    {
        public MsMainView()
        {
            InitializeComponent();
        }

        [Import]
        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly", Justification = "Needs to be a property to be composed by MEF")]
        MsMainViewViewModel ViewModel
        {
            get { return (MsMainViewViewModel)this.DataContext; }
            set
            {
                this.DataContext = value;
            }
        }
    }
}
