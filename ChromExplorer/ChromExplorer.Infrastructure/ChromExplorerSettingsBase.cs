using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.Infrastructure
{
    public  class ChromExplorerSettingsBase : ApplicationSettingsBase
    {
        public virtual bool IsValid()
        {
            return true;
        }
    }
}
