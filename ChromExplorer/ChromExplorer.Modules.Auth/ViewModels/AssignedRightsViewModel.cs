using ChromExplorer.Modules.Auth.models;
using ChromExplorer.Modules.Auth.services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.Modules.Auth.ViewModels
{
    public class AssignedRightsViewModel
    {
        private readonly AssignRoleRightsData _assignRoleRightsData;
        private AssignedRoleViewModel _model;
        public ObservableCollection<DropDownData> Users { get; set; }
        public ObservableCollection<DropDownData> Roles { get; set; }

        public string SelectedUserId { get; set; }
        public string SelectedRoleId { get; set; }
        public AssignedRightsViewModel()
        {
            _assignRoleRightsData = new AssignRoleRightsData();
            //_model = new AssignedRoleViewModel();

        }
        public List<AssignRoleRightsDO> GetAssignedRightsDetails()
        {
            List<AssignRoleRightsDO> lisdata = new List<AssignRoleRightsDO>();

            lisdata = _assignRoleRightsData.GetAssignedRightsDetails();

            return lisdata;
        }

        public List<DropDownData> BindUsername()
        {
            List<DropDownData> listdata = new List<DropDownData>();
            try
            {
                listdata = _assignRoleRightsData.GetUsername();
            }
            catch (Exception ex)
            {

            }
            return listdata;
        }

        public List<DropDownData> BindRoles()
        {
            List<DropDownData> listdata = new List<DropDownData>();
            try
            {
                listdata = _assignRoleRightsData.GetRoles();
            }
            catch (Exception ex)
            {

            }
            return listdata;
        }

        public List<AssignRoleRightsDO> SaveAssignRights(AssignRoleRightsDO assignRightsData, string reason = "")
        {
            List<AssignRoleRightsDO> listdata = new List<AssignRoleRightsDO>();
            try
            {
                listdata = _assignRoleRightsData.SaveAssignRights(assignRightsData, reason);
            }
            catch (Exception ex)
            {

            }
            return listdata;
        }

        //public bool DeleteAssignRole(AssignRoleRightsDO row)
        //{
        //    List<AssignRoleRightsDO> listdata = new List<AssignRoleRightsDO>();
        //    try
        //    {
        //        listdata = _assignRoleRightsData.DeleteAssignRole(row);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return listdata;
        //}

        public bool DeleteAssignRights(AssignRoleRightsDO row, string reason = "")
        {
            try
            {
                var result = _assignRoleRightsData.DeleteAssignRights(row, reason);

                if (result != null)
                {
                    return true; // deleted
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public List<AssignRoleRightsDO> GetAssignedRightsDetailsById(string id)
        {
            List<AssignRoleRightsDO> listdata = new List<AssignRoleRightsDO>();
            try
            {
                listdata = _assignRoleRightsData.GetAssignedRightsDetailsById(id);
            }
            catch (Exception ex)
            {

            }
            return listdata;
        }

        public bool ActivateAssignRights(AssignRoleRightsDO row, string reason = "")
        {
            try
            {
                return _assignRoleRightsData.ActivateAssignRights(row, reason);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        //public List<AssignRoleRightsDO> SaveAssignRights(AssignRoleRightsDO assignRoleRightsDO)
        //{
        //    List<AssignRoleRightsDO> listdata = new List<AssignRoleRightsDO>();
        //    try
        //    {
        //        listdata = _assignRoleRightsData.SaveAssignRights(assignRoleRightsDO);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return listdata;
        //}

        public List<DropDownData> BindMenu()
        {
            List<DropDownData> listdata = new List<DropDownData>();
            try
            {
                listdata = _assignRoleRightsData.GetMenu();
            }
            catch (Exception ex)
            {

            }
            return listdata;
        }
    }
}
