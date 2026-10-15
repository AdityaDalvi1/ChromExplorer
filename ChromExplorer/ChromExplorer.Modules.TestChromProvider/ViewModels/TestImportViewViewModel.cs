using System;
using System.ComponentModel.Composition;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Modules.TestChromProvider.Services;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;

namespace ChromExplorer.Modules.TestChromProvider.ViewModels
{
   
    [Export(typeof(TestImportViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TestImportViewViewModel
    {
        readonly InteractionRequest<Notification> _generationInteractionRequest;
       
        public readonly TestChromDataProvider _provider;

        public IInteractionRequest GenerationInteractionRequest
        {
            get { return _generationInteractionRequest; }
        }

        // TODO [Log(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error)]
        [ImportingConstructor]
        public TestImportViewViewModel(ChromDataProviderFactory factory)
        {
            _provider = factory.GetProvider<TestChromDataProvider>();


            _generationInteractionRequest = new InteractionRequest<Notification>();

            Title = "Test DataSet";
            Description = "Test chromatogram.\n * Spectrum are positive sinus waves with different freqs.\n * Chromotogram is total ion current";

            ChromPoints = 100;
            SpectrumPoints = 1000;

            GenerateCommand = new DelegateCommand<object>(OnGenerateExecute);
        }

        public DelegateCommand<object> GenerateCommand { get; private set; }

        public string Title { get; set; }
        public string Description { get; set; }

        public int ChromPoints { get; set; }
        public int SpectrumPoints { get; set; }

        // TODO [Log(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error)]
        private void OnGenerateExecute(object obj)
        {
            _provider.BuildTestChrom(ChromPoints, SpectrumPoints, Title, Description);
            RaiseProgressDialog("Generating test data source");
        }

        private void RaiseProgressDialog(String message)
        {
            _generationInteractionRequest.Raise(
                new Notification { Content = message, Title = "Progress" },
                UserAbortAction);
        }

        // TODO [Log(EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error)]
        private void UserAbortAction(Notification notification)
        {
            
        }
    }
}
