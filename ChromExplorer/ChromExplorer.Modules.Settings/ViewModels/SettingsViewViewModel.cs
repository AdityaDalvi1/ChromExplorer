using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Windows;
using ChromExplorer.DataModel.MethodFile;
using ChromExplorer.DataModel.PostProcessing;
using ChromExplorer.NistLibrary;
using log4net;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Win32;

namespace ChromExplorer.Modules.Settings.ViewModels
{
    // TODO [Log(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error)]
    [Export(typeof(SettingsViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class SettingsViewViewModel : IPartImportsSatisfiedNotification, INotifyPropertyChanged
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SettingsViewViewModel));

        [Import] private CompositionContainer _container;

        public SettingsViewViewModel()
        {
            TestValue = "Test value";
            NistLibraryVisibility = Visibility.Collapsed;
            CreateMethodCommand = new DelegateCommand<object>(OnCreateMethodCommandExecute);
        }

        public DelegateCommand<object> CreateMethodCommand { get; private set; }

        private PeakPickingSettingsViewViewModel _ppModel;
        private PostProcessingViewViewModel _processingModel;
        private NistLibrarySearchViewModel _nistLibrarySettings;
        private PeakIntegrationViewViewModel _integrationModel;
        private SNRSettingsViewViewModel _snrModel;

        [Import]
        public PeakPickingSettingsViewViewModel PPModel
        {
            get => _ppModel;
            private set { _ppModel = value; OnPropertyChanged("PPModel"); }
        }

        [Import]
        public PostProcessingViewViewModel ProcessingModel
        {
            get => _processingModel;
            private set { _processingModel = value; OnPropertyChanged("ProcessingModel"); }
        }

        [Import]
        public NistLibrarySearchViewModel NistLibrarySettings
        {
            get => _nistLibrarySettings;
            private set { _nistLibrarySettings = value; OnPropertyChanged("NistLibrarySettings"); }
        }

        [Import]
        public PeakIntegrationViewViewModel IntegrationModel
        {
            get => _integrationModel;
            private set { _integrationModel = value; OnPropertyChanged("IntegrationModel"); }
        }

        [Import]
        public SNRSettingsViewViewModel SNRModel
        {
            get => _snrModel;
            private set { _snrModel = value; OnPropertyChanged("SNRModel"); }
        }

        public string TestValue { get; set; }
        public Visibility NistLibraryVisibility { get; set; }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            NistLibraryVisibility = NistLibrarySettings != null ? Visibility.Visible : Visibility.Collapsed;
            OnPropertyChanged("NistLibraryVisibility");
        }

        // "Create Method": snapshots whatever is currently selected/typed across all 5 tabs (not
        // just whatever was already Applied) into one portable .cmethod file, so it can be handed
        // to "Apply Saved Method" on the graph screen later, on this machine or another one.
        private void OnCreateMethodCommandExecute(object obj)
        {
            try
            {
                var method = new ChromMethodFile { MethodName = "New Method" };

                if (PPModel != null)
                {
                    method.PeakPicking.AlgorithmObjectKey = PPModel.Current?.ObjectKey;
                    method.PeakPicking.ThresholdType = PPModel.ThresholdType.ToString();
                    method.PeakPicking.Threshold = PPModel.Threshold;
                    method.PeakPicking.Analyzer = PPModel.Analyzer.ToString();
                    method.PeakPicking.LabelFormat = PPModel.LabelFormat;
                    method.PeakPicking.LabelOrientation = PPModel.LabelOrientation.ToString();
                    method.PeakPicking.LabelPlacement = PPModel.LabelPlacement.ToString();

                    if (MethodSettingsReflector.TryGetPeakPickerHalfSizes(PPModel.Current, out var minHalf, out var peakHalf))
                    {
                        method.PeakPicking.MinPeakHalfSize = minHalf;
                        method.PeakPicking.PeakHalfSize = peakHalf;
                    }
                }

                if (ProcessingModel != null)
                {
                    foreach (var op in (ProcessingModel.MsOperations ?? Enumerable.Empty<IPostProcessingOperation>())
                        .Concat(ProcessingModel.EicOperations ?? Enumerable.Empty<IPostProcessingOperation>()))
                    {
                        method.PostProcessingOperations.Add(new PostProcessingOperationSettings
                        {
                            ObjectKey = op.ObjectKey,
                            IsActive = op.IsActive,
                            Parameters = MethodSettingsReflector.CaptureConfigViewParameters(op)
                        });
                    }
                }

                if (IntegrationModel != null)
                {
                    method.PeakIntegration.StartTime = IntegrationModel.StartTime;
                    method.PeakIntegration.EndTime = IntegrationModel.EndTime;
                }

                if (SNRModel != null)
                {
                    method.SNR.SignalDefinition = SNRModel.SignalDefinition;
                    method.SNR.NoiseDefinition = SNRModel.NoiseDefinition;
                    method.SNR.Multiplier = SNRModel.Multiplier;
                    method.SNR.IsAutomaticDetection = SNRModel.IsAutomaticDetection;
                    method.SNR.DesiredLength = SNRModel.DesiredLength;
                    method.SNR.MinimumLength = SNRModel.MinimumLength;
                    method.SNR.SpecificTimeRange = SNRModel.SpecificTimeRange;
                    method.SNR.AutoCompute = SNRModel.AutoCompute;

                    // Whatever the last "Calculate" click on the SNR tab produced (0 if never
                    // calculated this session) � saved alongside the inputs that produced it.
                    method.SNR.SignalValue = SNRModel.SignalValue;
                    method.SNR.NoiseValue = SNRModel.NoiseValue;
                    method.SNR.SNRatio = SNRModel.SNRatio;
                    method.SNR.NoiseRegionUsed = SNRModel.NoiseRegionUsed;
                }

                if (NistLibrarySettings != null)
                {
                    method.NistLibrary.NistExecutablePath = NistLibrarySettings.NistExecutablePath;
                    method.NistLibrary.FirstLocatorFile = NistLibrarySettings.FirstLocatorFile;
                    method.NistLibrary.SecondLocatorFile = NistLibrarySettings.SecondLocatorFile;
                    method.NistLibrary.WorkingDir = NistLibrarySettings.WorkingDir;
                }

                var dlg = new SaveFileDialog
                {
                    DefaultExt = ChromMethodFileService.FileExtension,
                    Filter = ChromMethodFileService.FileDialogFilter,
                    FileName = "Method" + ChromMethodFileService.FileExtension,
                    // OverwritePrompt off: an already-tracked file is never overwritten anyway
                    // (CanSaveTo below), so the native "replace?" prompt would only mislead.
                    OverwritePrompt = false
                };

                // Editing a method that was applied from an existing file (e.g. a Rejected one)
                // proposes a NEW name next to it — never the original's own name.
                var openedFrom = ChromExplorer.Infrastructure.SavedFileRegistry.GetOpenedFile(
                    ChromExplorer.Infrastructure.SavedFileRegistry.TypeDataProcessingMethod);
                if (!string.IsNullOrEmpty(openedFrom))
                {
                    var suggested = ChromExplorer.Infrastructure.SavedFileRegistry.SuggestRevisionPath(openedFrom);
                    dlg.FileName = System.IO.Path.GetFileName(suggested);
                    dlg.InitialDirectory = System.IO.Path.GetDirectoryName(suggested);
                }

                if (!ChromExplorer.Infrastructure.FileStorage.ShowSaveDialog(dlg,
                        ChromExplorer.Infrastructure.StorageCategory.DataProcessingMethods, "data processing method"))
                    return;

                if (!ChromExplorer.Infrastructure.FileSaveGuard.CanSaveTo(dlg.FileName, "method file"))
                    return;

                method.MethodName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                ChromMethodFileService.Save(method, dlg.FileName);

                if (!string.IsNullOrEmpty(openedFrom) && !string.Equals(openedFrom, dlg.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    ChromExplorer.Infrastructure.SavedFileRegistry.WriteAudit("Create Method - Revised Copy",
                        $"Data Processing Method saved as a new file | Revised from: {System.IO.Path.GetFileName(openedFrom)} | New file: {System.IO.Path.GetFileName(dlg.FileName)}",
                        System.IO.Path.GetFileName(dlg.FileName));
                }
                else
                {
                    ChromExplorer.Infrastructure.SavedFileRegistry.WriteAudit("Create Method",
                        $"Data Processing Method saved | File: {System.IO.Path.GetFileName(dlg.FileName)}",
                        System.IO.Path.GetFileName(dlg.FileName));
                }

                ChromExplorer.Infrastructure.SavedFileRegistry.Record(
                    ChromExplorer.Infrastructure.SavedFileRegistry.TypeDataProcessingMethod, dlg.FileName);

                // The new file is now the one these settings correspond to.
                ChromExplorer.Infrastructure.SavedFileRegistry.SetOpenedFile(
                    ChromExplorer.Infrastructure.SavedFileRegistry.TypeDataProcessingMethod, dlg.FileName);

                MessageBox.Show("Method saved to " + dlg.FileName, "Create Method", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                log.Error("Create Method failed", ex);
                ChromExplorer.Infrastructure.SavedFileRegistry.WriteAudit("Create Method - Failed",
                    "Data Processing Method could not be saved | Error: " + ex.Message);
                MessageBox.Show("Could not save the method: " + ex.Message, "Create Method", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}