using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using ChromExplorer.Modules.Auth;
using ChromExplorer.Modules.DataProvider;
using ChromExplorer.Modules.Help;
using ChromExplorer.Modules.MappedFileProvider.Services;
using ChromExplorer.Modules.MascotExporter.Services;
using ChromExplorer.Modules.ModasChromProvider.Services;
using ChromExplorer.Modules.ModasGrpc;
using ChromExplorer.Modules.Ms;
using ChromExplorer.Modules.Ms.Services;
using ChromExplorer.Modules.MzMLExporter.Services;
using ChromExplorer.Modules.Quan;
using ChromExplorer.Modules.Settings;
using ChromExplorer.Modules.Status;
using ChromExplorer.Modules.Syringe;
using ChromExplorer.Modules.Viewer;
using ChromExplorer.NistLibrary;
using ChromExplorer.PeakPicking.ModasPeakPicker;
using ChromExplorer.PealPicking.EmptyPeakPicker;
using ChromExplorer.PostProcessing.GaussianFilter;
using ChromExplorer.PostProcessing.MedianFilter;
using ChromExplorer.PostProcessing.Normalize;
using ChromExplorer.PostProcessing.RemoveSpikes;
using ChromExplorer.PostProcessing.SavitzkyGolaySmoother;
using ChromExplorer.Services.PickPeakerService.Services;
using ChromExplorer.Services.PostProcessingService.Services;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.MefExtensions;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Modas.Grpc.Client;
using ModuleTracking;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Windows;

namespace ChromExplorer
{
    public class ChromExplorerBootstraper : MefBootstrapper
    {
        private readonly Log4NetLogger _logger = new Log4NetLogger();

        // MefBootstrapper's own Container property is protected — this exposes it publicly so
        // App.xaml.cs can resolve a shared MEF singleton (e.g. ReportSessionService) outside the
        // bootstrapper itself, without changing the base member's own accessibility.
        public CompositionContainer ExportedContainer => this.Container;

        /// <summary>
        /// Creates the shell or main window of the application.
        /// </summary>
        /// <returns>The shell of the application.</returns>
        /// <remarks>
        /// If the returned instance is a <see cref="DependencyObject"/>, the
        /// <see cref="MefBootstrapper"/> will attach the default <seealso cref="IRegionManager"/> of
        /// the application in its <see cref="RegionManager.RegionManagerProperty"/> attached property
        /// in order to be able to add regions by using the <seealso cref=""Microsoft.Practices.Composite.Presentation.Regions.RegionManager.RegionNameProperty"/>
        /// attached property from XAML.
        /// </remarks>
        protected override DependencyObject CreateShell()
        {
            return this.Container.GetExportedValue<Shell>();
        }

        /// <summary>
        /// Initializes the shell.
        /// </summary>
        /// <remarks>
        /// The base implemention ensures the shell is composed in the container.
        /// </remarks>
        // TODO [Log(/*EntryLevel = LogLevel.Debug, ExitLevel = LogLevel.Debug, ExceptionLevel = LogLevel.Error*/)]
        protected override void InitializeShell()
        {
            base.InitializeShell();

            Application.Current.MainWindow = (Shell)this.Shell;
            Application.Current.MainWindow.Show();
            
        }

        /// <summary>
        /// Configures the <see cref="AggregateCatalog"/> used by MEF.
        /// </summary>
        /// <remarks>
        /// The base implementation does nothing.
        /// </remarks>
        protected override void ConfigureAggregateCatalog()
        {
            base.ConfigureAggregateCatalog();
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(ChromExplorerBootstraper).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(RegionNames).Assembly));

            //var catalog = new DirectoryCatalog("Modules");
            //this.AggregateCatalog.Catalogs.Add(catalog);

            //Manually add modules for ClickOnce Deploment

            
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(DataProviderModule).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(HelpModule).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(MappedFileProvider).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(ModasChromProvider).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(MzMLExportService).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(MascotExporter).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(SettingsModule).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(StatusModule).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(ViewerModule).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(ModasPeakPicker).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(EmptyPeakPicker).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(QuanModule).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(MedianFilter).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(NormalizeHandler).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(SavitzkyGolaySmoother).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(GaussianFilter).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(RemoveSpikesOps).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(PeakPickerService).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(PostProcessingService).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(WellKnownModuleNames).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(NistLibrarySearchHandler).Assembly));
            //this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(LicensingModule).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(SyringeModule).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(MsModule).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(AuthModule).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(ModasGrpcModule).Assembly));
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(MTRemoveServiceClientFactory).Assembly));
            //this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(WileyLibraryModule).Assembly));


        }

        /// <summary>
        /// Configures the <see cref="CompositionContainer"/>.
        /// May be overwritten in a derived class to add specific type mappings required by the application.
        /// </summary>
        /// <remarks>
        /// The base implementation registers all the types direct instantiated by the bootstrapper with the container.
        /// The base implementation also sets the ServiceLocator provider singleton.
        /// </remarks>
        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();

            // Because we created the CallbackLogger and it needs to be used immediately, we compose it to satisfy any imports it has.
            //this.Container.ComposeExportedValue<CallbackLogger>(this._callbackLogger);
            this.Container.ComposeExportedValue<Log4NetLogger>(this._logger);
            this.Container.ComposeExportedValue<CompositionContainer>(this.Container);
        }

        /// <summary>
        /// Creates the <see cref="IModuleCatalog"/> used by Prism.
        /// </summary>
        /// <remarks>
        /// The base implementation returns a new ModuleCatalog.
        /// </remarks>
        /// <returns>
        /// A ConfigurationModuleCatalog.
        /// </returns>
        protected override IModuleCatalog CreateModuleCatalog()
        {
            // When using MEF, the existing Prism ModuleCatalog is still the place to configure modules via configuration files.
            return new ConfigurationModuleCatalog();
        }

        /// <summary>
        /// Create the <see cref="ILoggerFacade"/> used by the bootstrapper.
        /// </summary>
        /// <remarks>
        /// The base implementation returns a new TextLogger.
        /// </remarks>
        /// <returns>
        /// A CallbackLogger.
        /// </returns>
        protected override ILoggerFacade CreateLogger()
        {
            // Because the Shell is displayed after most of the interesting boostrapper work has been performed,
            // this quickstart uses a special logger class to hold on to early log entries and display them 
            // after the UI is visible.
            return this._logger;
        }

        protected override IRegionBehaviorFactory ConfigureDefaultRegionBehaviors()
        {
            var factory = base.ConfigureDefaultRegionBehaviors();

            factory.AddIfMissing("AutoPopulateExportedViewsBehavior", typeof(AutoPopulateExportedViewsBehavior));

            return factory;
        }

        
    }
}
