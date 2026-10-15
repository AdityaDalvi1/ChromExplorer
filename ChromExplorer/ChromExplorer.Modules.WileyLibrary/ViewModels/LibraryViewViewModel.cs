using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace ChromExplorer.Modules.WileyLibrary.ViewModels
{
    [Export(typeof(LibraryViewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class LibraryViewViewModel : IPartImportsSatisfiedNotification
    {
        [Import]
        private CompositionContainer _container;

        public LibraryViewViewModel()
        {
            TestValue = "Test value";
        }


        public LibraryBrowserViewViewModel BrowserModel { get; private set; }
        public string TestValue { get; set; }

        void IPartImportsSatisfiedNotification.OnImportsSatisfied()
        {
            BrowserModel = _container.GetExportedValue<LibraryBrowserViewViewModel>();
        }
    }
}