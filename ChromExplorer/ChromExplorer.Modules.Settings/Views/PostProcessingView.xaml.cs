using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.Modules.Settings.ViewModels;

namespace ChromExplorer.Modules.Settings.Views
{
    /// <summary>
    /// Interaction logic for PostProcessingView.xaml
    /// </summary>
    public partial class PostProcessingView : UserControl
    {
        public PostProcessingView()
        {
            InitializeComponent();
        }

        private void CheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox == null) return;

            var operation = checkBox.DataContext as IPostProcessingOperation;
            if (operation == null) return;

            var viewModel = this.DataContext as PostProcessingViewViewModel;
            if (viewModel == null) return;

            if (operation.Type == PostProcessingType.MS)
            {
                viewModel.SelectedMsOperation = operation;
            }
            else if (operation.Type == PostProcessingType.EIC)
            {
                viewModel.SelectedEicOperation = operation;
            }
        }
    }
}
