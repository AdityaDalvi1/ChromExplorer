using System.Reflection;
using ChromExplorer.Infrastructure.InteractionRequest;
using log4net;

namespace ChromExplorer.Modules.Viewer.Views
{
    public partial class RsdInfoView : InteractionDialogBase
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public RsdInfoView()
        {
            Log.Debug("RsdInfoView() called");
            InitializeComponent();
        }
    }
}
