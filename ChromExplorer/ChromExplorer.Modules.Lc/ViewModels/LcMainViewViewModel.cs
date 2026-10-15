using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.Modules.Lc.ViewModels
{
    [Export(typeof(LcMainViewViewModel))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class LcMainViewViewModel
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [ImportingConstructor]
        public LcMainViewViewModel()
        {
            log.Debug("LcMainViewViewModel() created");
        }
    }
}
