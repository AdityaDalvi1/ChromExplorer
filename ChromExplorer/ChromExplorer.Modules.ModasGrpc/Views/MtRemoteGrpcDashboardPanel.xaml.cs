using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.ModasGrpc.Models;
using Microsoft.Practices.Prism.Regions;
using System;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;

namespace ChromExplorer.Modules.ModasGrpc.Views
{
    public partial class MtRemoteGrpcDashboardPanel : UserControl
    {
        public MtRemoteGrpcDashboardPanel()
        {
            InitializeComponent();
        }

        // The Instrument Method/Data Folder "..." buttons in the Sample Sequence grid: DataContext
        // on the clicked button is the SequenceItem for that row (from the CellTemplate binding).
        private void BrowseInstrumentMethod_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MtRemoteGrpcDashboardViewModel vm && (sender as FrameworkElement)?.DataContext is SequenceItem item)
                vm.BrowseInstrumentMethodFor(item);
        }

        private void BrowseDataFolder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MtRemoteGrpcDashboardViewModel vm && (sender as FrameworkElement)?.DataContext is SequenceItem item)
                vm.BrowseDataFolderFor(item);
        }

        // Per-row "remove" button in the Sample Sequence grid.
        private void RemoveSequenceRow_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MtRemoteGrpcDashboardViewModel vm && (sender as FrameworkElement)?.DataContext is SequenceItem item)
                vm.RemoveSequenceItem(item);
        }

        // "Fill down" context menu on each Sample Sequence row — MenuItem.DataContext resolves to
        // the row's own SequenceItem (a row's ContextMenu inherits DataContext through the logical
        // tree), so `sender`'s DataContext identifies which row was right-clicked.
        private void FillDownSampleType_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MtRemoteGrpcDashboardViewModel vm && (sender as FrameworkElement)?.DataContext is SequenceItem item)
                vm.FillDownSampleClassification(item);
        }

        private void FillDownAnalysisSampleType_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MtRemoteGrpcDashboardViewModel vm && (sender as FrameworkElement)?.DataContext is SequenceItem item)
                vm.FillDownAnalysisSampleType(item);
        }

        private void FillDownInstrumentMethod_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MtRemoteGrpcDashboardViewModel vm && (sender as FrameworkElement)?.DataContext is SequenceItem item)
                vm.FillDownInstrumentMethod(item);
        }

        private void FillDownDataFolder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MtRemoteGrpcDashboardViewModel vm && (sender as FrameworkElement)?.DataContext is SequenceItem item)
                vm.FillDownDataFolder(item);
        }

        // The small checkbox in each Sample Sequence column header — check it to apply the
        // currently SELECTED row's value for that column to every other Pending row. It's a
        // one-shot trigger, not a persistent toggle, so it un-checks itself right after.
        private void FillDownHeaderCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox)) return;

            if (DataContext is MtRemoteGrpcDashboardViewModel vm)
            {
                switch (checkBox.Tag as string)
                {
                    case "SampleType": vm.FillDownSampleClassification(vm.SelectedSequenceItem); break;
                    case "AnalysisSampleType": vm.FillDownAnalysisSampleType(vm.SelectedSequenceItem); break;
                    case "InstrumentMethod": vm.FillDownInstrumentMethod(vm.SelectedSequenceItem); break;
                    case "DataFolder": vm.FillDownDataFolder(vm.SelectedSequenceItem); break;
                }
            }

            checkBox.IsChecked = false;
        }

        public void Bind(MtRemoteGrpcDashboardViewModel model)
        {
            DataContext = model;
            if (model == null)
            {
                StatusView.DataContext = null;
                MessageView.DataContext = null;
                SyringeView.DataContext = null;
                IonSourceControlView.DataContext = null;
                MethodOverridesView.DataContext = null;
                return;
            }

            StatusView.DataContext = model.StatusViewModel;
            NewStatusView.DataContext = model.StatusViewModel;
            MessageView.DataContext = model.RemoteMessageVM;
            SyringeView.DataContext = model.SyringeControlVM;
            IonSourceControlView.DataContext = model.IonSourceControlVM;
            MethodOverridesView.DataContext = model.MethodOverridesVM;
        }
    }
}

