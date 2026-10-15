using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.Auth.models;
using ChromExplorer.Modules.Auth.ViewModels;
using MathNet.Filtering.Windowing;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChromExplorer.Modules.Auth.Views
{
    [Export("AssignRights", typeof(AssignRights))]

    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class AssignRights : UserControl, INavigationAware
    {
        private readonly AssignedRightsViewModel _assignRightsDetails;
        private bool isEditMode = false;
        [Import]
        public IRegionManager RegionManager { get; set; }
        public AssignRights()
        {
            InitializeComponent();
            //LoadData();
            _assignRightsDetails = new AssignedRightsViewModel();

            this.DataContext = _assignRightsDetails;


            //BindUserName();   
            //BindRoles();
        }
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // no need anything here for now
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            string pageName = typeof(ViewAssignRights).FullName;
            RegionManager.RequestNavigate(RegionNames.OverContentRegion, new Uri(pageName, UriKind.Relative));
        }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            string id = navigationContext.Parameters["id"]?.ToString();
            string mode = navigationContext.Parameters["mode"]?.ToString();
            string reason = navigationContext.Parameters["reason"]?.ToString();

            if (mode == "edit" && !string.IsNullOrEmpty(id))
            {
                isEditMode = true; // 🔥 important
            }

            try
            {
                _editReason = !string.IsNullOrEmpty(reason) ? Uri.UnescapeDataString(reason) : "";
            }
            catch (Exception)
            {
                _editReason = reason ?? "";
            }

            BindMenu();
            BindRoles();

            if (isEditMode)
            {
                LoadRoleRightsData(id);
            }
        }

        private string _editId = null; // 🔥 store id globally
        private string _editReason = ""; // 🔥 reason for edit, passed from ViewAssignRights

        private void LoadRoleRightsData(string id)
        {
            try
            {
                List<AssignRoleRightsDO> listdata = _assignRightsDetails.GetAssignedRightsDetailsById(id);


                if (listdata != null)
                {
                    // 🔥 store id for update
                    _editId = listdata[0].user_rights_id;

                    cmbMenu.SelectedValue = listdata[0].MenuName;
                    cmbRole.SelectedValue = listdata[0].RoleName;




                    // 🔥 change button names
                    btnSubmit.Content = "Update";
                    btnCancel.Content = "Reset";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssignRoleRightsDO assignRightssData = new AssignRoleRightsDO();

                // 🔥 if edit mode → pass id
                if (!string.IsNullOrEmpty(_editId))
                {
                    assignRightssData.user_rights_id = _editId;
                }

                if (cmbMenu.SelectedItem != null)
                {
                    var selectedUser = (DropDownData)cmbMenu.SelectedItem;
                    assignRightssData.MenuName = selectedUser.Id;
                }
                else
                {
                    MessageBox.Show("Please select menu");
                    return;
                }

                if (cmbRole.SelectedItem != null)
                {
                    var selectedRole = (DropDownData)cmbRole.SelectedItem;
                    assignRightssData.RoleName = selectedRole.Id;
                }
                else
                {
                    MessageBox.Show("Please select role");
                    return;
                }

                List<AssignRoleRightsDO> listdata = _assignRightsDetails.SaveAssignRights(assignRightssData, _editReason);

                if (listdata != null && listdata.Count > 0)
                {
                    var result = listdata[0];

                    if (result.Status == "Inserted")
                    {
                        MessageBox.Show("Rights assigned successfully");
                    }
                    else if (result.Status == "Updated")
                    {
                        MessageBox.Show("Rights updated successfully");
                    }
                    isEditMode = false;

                    string pageName = typeof(ViewAssignRights).FullName;
                    RegionManager.RequestNavigate(RegionNames.OverContentRegion, new Uri(pageName, UriKind.Relative));
                }

                Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //private void SubmitButton_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        AssignRightsData AssignRightssData = new AssignRightsData();

        //        if (cmbMenu.SelectedItem != null)
        //        {
        //            var selectedUser = (DropDownData)cmbMenu.SelectedItem;
        //            AssignRightssData.UserName = selectedUser.Id;    
        //        }
        //        else
        //        {
        //            MessageBox.Show("Please select user");
        //            return;
        //        }

        //        if (cmbRole.SelectedItem != null)
        //        {
        //            var selectedRole = (DropDownData)cmbRole.SelectedItem;
        //            AssignRightssData.RoleName = selectedRole.Id;
        //        }
        //        else
        //        {
        //            MessageBox.Show("Please select role");
        //            return;
        //        }

        //        if (AssignRightssData.UserName == "0" || AssignRightssData.RoleName == "0")
        //        {
        //            MessageBox.Show("Please select valid user and role");
        //            return;
        //        }

        //       List<AssignRightsData> listdata = _AssignRightsDetails.SaveAssignRights(AssignRightssData);

        //        if (listdata != null && listdata.Count > 0)
        //        {
        //            var result = listdata[0];

        //            if (result.Status == "Inserted")
        //            {
        //                MessageBox.Show("Role assigned successfully");
        //                string pageName = typeof(ViewAssignRights).FullName;

        //                var uri = new Uri(pageName, UriKind.Relative);

        //                RegionManager.RequestNavigate(RegionNames.OverContentRegion, uri);
        //            }
        //            else if (result.Status == "Updated")
        //            {
        //                MessageBox.Show("Role already exists, updated");
        //                string pageName = typeof(ViewAssignRights).FullName;

        //                var uri = new Uri(pageName, UriKind.Relative);

        //                RegionManager.RequestNavigate(RegionNames.OverContentRegion, uri);
        //            }
        //            else
        //            {
        //                MessageBox.Show("Operation failed");
        //            }
        //        }
        //        Clear();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}

        private void Clear()
        {
            try
            {
                cmbRole.SelectedIndex = 0;
                cmbMenu.SelectedIndex = 0;
            }
            catch (Exception ex)
            {

            }
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pageName = typeof(ViewAssignRights).FullName;

                var uri = new Uri(pageName, UriKind.Relative);

                RegionManager.RequestNavigate(RegionNames.OverContentRegion, uri);
            }
            catch (Exception ex)
            {

            }
        }
        //private void BindUserName()
        //{
        //    try
        //    {
        //        var listdata = _AssignRightsDetails.BindUsername();

        //        if (listdata != null && listdata.Count > 0)
        //        {
        //            _AssignRightsDetails.Users = new ObservableCollection<User>(
        //                listdata.Select(x => new User
        //                {
        //                    UserId = x.Id,
        //                    Username = x.Text
        //                }).ToList()
        //            );
        //        }
        //        else
        //        {
        //            _AssignRightsDetails.Users = new ObservableCollection<User>();
        //            MessageBox.Show("No users found");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}

        private void BindRoles()
        {
            try
            {
                List<DropDownData> listdata = new List<DropDownData>();
                listdata = _assignRightsDetails.BindRoles();

                if (listdata != null && listdata.Count > 0)
                {
                    listdata.Insert(0, new DropDownData
                    {
                        Id = "0",
                        Text = "--Select Role--"
                    });

                    cmbRole.ItemsSource = listdata;
                    cmbRole.DisplayMemberPath = "Text";
                    cmbRole.SelectedValuePath = "Id";

                    if (!isEditMode)
                    {
                        cmbRole.SelectedIndex = 0;
                    }
                }
                else
                {
                    cmbRole.ItemsSource = null;
                    MessageBox.Show("No roles found");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void BindMenu()
        {
            try
            {
                List<DropDownData> listdata = new List<DropDownData>();
                listdata = _assignRightsDetails.BindMenu();

                if (listdata != null && listdata.Count > 0)
                {
                    listdata.Insert(0, new DropDownData
                    {
                        Id = "0",
                        Text = "--Select Menu--"
                    });

                    cmbMenu.ItemsSource = listdata;
                    cmbMenu.DisplayMemberPath = "Text";
                    cmbMenu.SelectedValuePath = "Id";

                    if (!isEditMode)
                    {
                        cmbMenu.SelectedIndex = 0;
                    }
                }
                else
                {
                    cmbMenu.ItemsSource = null;
                    MessageBox.Show("No users found");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
