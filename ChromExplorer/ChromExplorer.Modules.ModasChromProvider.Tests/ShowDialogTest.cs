using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Windows.Threading;
using ChromExplorer.DataModel.Interfaces;
using ChromExplorer.Infrastructure.Interfaces;
using ChromExplorer.Mocks;
using ChromExplorer.Modules.DataProvider.ViewModels;
using ChromExplorer.Modules.MappedFileProvider.ViewModels;
using ChromExplorer.Modules.ModasChromProvider.Tests.Forms;
using ChromExplorer.Modules.ModasChromProvider.ViewModels;
using ChromExplorer.PostProcessing.MedianFilter;
using ChromExplorer.PostProcessing.SavitzkyGolaySmoother;
using ChromExplorer.Services.PostProcessingService.Services;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.Regions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChromExplorer.Modules.ModasChromProvider.Tests
{
    [TestClass]
    public class ShowDialogTest
    {
        private CompositionContainer _container;

        [TestInitialize]
        public void Init()
        {

            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(DataProviderMainViewViewModel).Assembly));
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(MappedFileImportViewViewModel).Assembly));
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(ModasImportViewViewModel).Assembly));

            catalog.Catalogs.Add(new AssemblyCatalog(typeof(PostProcessingService).Assembly));
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(SavitzkyGolaySmoother).Assembly));
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(MedianFilter).Assembly));
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(ChromDataProviderFactory).Assembly));
            //catalog.Catalogs.Add(new AssemblyCatalog(typeof(Services.ModasChromProvider).Assembly));
            

           
            _container = new CompositionContainer(catalog);
            _container.ComposeExportedValue<IRegionManager>(new MockRegionManager());
            _container.ComposeExportedValue<IStatusUpdateService>(new MockStatusUpdateService());
            _container.ComposeExportedValue<IEventAggregator>(new MockEventAggregator());
            _container.ComposeExportedValue<ILoggerFacade>(new MockLoggerFacade());
            _container.ComposeExportedValue<ILoggerFacade>(new MockLoggerFacade());
       
                


            //_container.ComposeExportedValue<IChromDataProvider>(new ChromExplorer.Modules.MappedFileProvider.Services.MappedFileProvider());
        }


        [TestMethod]
        public void ShowWindow()
        {
            return;
            //try
            //{
            //    var modasProvider = _container.GetExportedValues<IChromDataProvider>();
            //    var factory = _container.GetExportedValue<ChromDataProviderFactory>();
            //    var eventAggregator = _container.GetExportedValue<IEventAggregator>();
            //    var regionManager = _container.GetExportedValue<IRegionManager>();
            //    var settingsProvider = _container.GetExportedValue<IUserSettingsProvider>();


            //    var vm = new ModasImportViewViewModel(factory, eventAggregator, regionManager, settingsProvider);
                   
            //    var view = new OpenModasFileDialog()
            //    {
            //        DataContext = vm
            //    };

            //    view.ShowDialog();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}
        }

        [TestCleanup]
        public void CleanUp()
        {
            Dispatcher.CurrentDispatcher.InvokeShutdown();

        }
    }
}
