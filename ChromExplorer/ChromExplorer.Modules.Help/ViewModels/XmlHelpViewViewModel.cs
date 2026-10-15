using System;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Xps.Packaging;
using log4net;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;

namespace ChromExplorer.Modules.Help.ViewModels
{
    [Export(typeof(XmlHelpViewViewModel))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class XmlHelpViewViewModel
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        //public static string TemplateFileName = "ChromExplorer.Modules.Help.Documents.graphic_arts_wp.xps";
        public static string TemplateFileName = "ChromExplorer.Modules.Help.Documents.chrom_explorer_1.3.1_docs.xps";
        

        private readonly XpsDocument _document;

        readonly InteractionRequest<Notification> _notificationFromViewModelInteractionRequest;
        readonly InteractionRequest<Confirmation> _confirmationFromViewModelInteractionRequest;

        public IInteractionRequest NotificationFromViewModelInteractionRequest
        {
            get { return _notificationFromViewModelInteractionRequest; }
        }

        /// <summary>
        /// Gets the notification from view model interaction request. View binds to this property
        /// </summary>
        public IInteractionRequest ConfirmationFromViewModelInteractionRequest
        {
            get { return _confirmationFromViewModelInteractionRequest; }
        }

       

        [ImportingConstructor]
        public XmlHelpViewViewModel()
        {
            _notificationFromViewModelInteractionRequest = new InteractionRequest<Notification>();
            _confirmationFromViewModelInteractionRequest = new InteractionRequest<Confirmation>();

            string strMemoryPackageName = string.Format("memorystream://{0}", TemplateFileName);
            Uri packageUri = new Uri(strMemoryPackageName);

            var pkg = PackageStore.GetPackage(packageUri);

            if (pkg == null)
            {

                var a = typeof (XmlHelpViewViewModel).Assembly;
                var names = a.GetManifestResourceNames();
                Stream stream = a.GetManifestResourceStream(TemplateFileName);

                if (stream == null)
                    return;

                int length = (int) stream.Length;
                MemoryStream ms = new MemoryStream();
                ms.Capacity = (int) length;
                byte[] buffer = new byte[length];

                stream.Read(buffer, 0, length);
                ms.Write(buffer, 0, length);

                pkg = Package.Open(ms);


                PackageStore.AddPackage(packageUri, pkg);
            }

            _document = new XpsDocument(pkg, CompressionOption.Maximum, strMemoryPackageName);
            SampleHelpDocument = _document.GetFixedDocumentSequence();

            InitializeCommands();

           

        }

        public FixedDocumentSequence SampleHelpDocument { get; private set; }

        public DelegateCommand<object> NotificationCommand { get; private set; }
        public DelegateCommand<object> ConfirmationCommand { get; private set; }
        public DelegateCommand<object> WaitCursorCommand { get; private set; }

        private void InitializeCommands()
        {
            NotificationCommand = new DelegateCommand<object>(OnNotificationCommand);
            ConfirmationCommand = new DelegateCommand<object>(OnConfirmationCommand);
            WaitCursorCommand = new DelegateCommand<object>(OnWaitCursor);
        }

        private void OnWaitCursor(object obj)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            
        }

        private void OnConfirmationCommand(object obj)
        {
            log.Debug("OnConfirmationCommand(object obj) called");

            _confirmationFromViewModelInteractionRequest.Raise(
               new Confirmation { Content = "Are you sure you want to close this confirmation dialog?", Title = "MVVM Scenario" },
               confirmation =>
               {
                   if (confirmation.Confirmed)
                   {
                       RaiseNotificationFromViewModelInteractionRequest("You just clicked OK");
                   }
               });
        }

        private void OnNotificationCommand(object obj)
        {
            log.Debug("OnNotificationCommand(object obj) called");
            RaiseNotificationFromViewModelInteractionRequest("Hello from view model Prism Interaction Request");
        }


        private void RaiseNotificationFromViewModelInteractionRequest(String message)
        {
            _notificationFromViewModelInteractionRequest.Raise(
                new Notification { Content = message, Title = "MVVM Scenario" },
                dummy => { });
        }
    }
}
