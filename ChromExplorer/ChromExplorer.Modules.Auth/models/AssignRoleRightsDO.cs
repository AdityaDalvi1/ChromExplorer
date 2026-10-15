using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.Modules.Auth.models
{
    public class AssignRoleRightsDO
    {
        public string user_rights_id { get; set; }
        public int SrNo { get; set; }
        public string MenuName { get; set; }
        public string RoleName { get; set; }
        public string Status { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class AssignRoleData
    {
        public string userrole_id { get; set; }
        public int SrNo { get; set; }
        public string UserName { get; set; }
        public string RoleName { get; set; }
        public string Status { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DropDownData
    {

        public string Id { get; set; }
        public string Text { get; set; }
    }
}
