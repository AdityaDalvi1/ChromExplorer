using ChromExplorer.Infrastructure;
using ChromExplorer.Infrastructure.Behaviors;
using ChromExplorer.Modules.Auth.Services;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ChromExplorer.Modules.Auth.Views
{
    /// <summary>
    /// Interaction logic for UserRegistrationToolbutton.xaml
    /// </summary>
    [ViewExport(RegionName = RegionNames.LeftToolBarRegion)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ViewSortHint("5")]
    public partial class UserRegistrationToolbutton : UserControl
    {
        public UserRegistrationToolbutton()
        {
            InitializeComponent();
        }

        [Import]
        public IRegionManager RegionManager { get; set; }

        //private void BtnClick(object sender, RoutedEventArgs e)
        //{
        //    if (RegionManager == null)
        //    {
        //        MessageBox.Show("RegionManager is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return;
        //    }

        //    var uri = new Uri("UserRegistrationView", UriKind.Relative);
        //    RegionManager.RequestNavigate(RegionNames.OverContentRegion, uri);
        //}
        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";
        //private readonly string _connectionString = @"Data Source=D:\chromatograpy 23 feb 2026\chromexplorer-master 21-02 (1)\chromexplorer-master (2) (1)\chromexplorer-master\ChromExplorer\source.db3;Version=3;";
        //
        private void BtnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;

                if (button?.ContextMenu != null)
                {
                    button.ContextMenu.PlacementTarget = button;
                    button.ContextMenu.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                Log("User Menu Open", ex.Message);
            }
        }

        private void ViewUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                NavigateIfAuthorized(typeof(UserDetails).FullName, "View User");
                //NavigateIfAuthorized("UserDetails", "View User");
                //var uri = new Uri("UserDetails", UriKind.Relative);
                //RegionManager.RequestNavigate(RegionNames.OverContentRegion, uri);
            }
            catch (Exception ex)
            {
                Log("View User Click", ex.Message);
            }
        }

        private void ViewAssignRole_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                NavigateIfAuthorized(typeof(ViewAssignRole).FullName, "View Assign Role");
            }
            catch (Exception ex)
            {
                Log("View Assign Role Click", ex.Message);
            }
        }

        private void ViewAssignRights_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateIfAuthorized(typeof(ViewAssignRights).FullName, "View Assign Rights");
            }
            catch (Exception ex)
            {
                Log("View Assign Rights Click", ex.Message);
            }
        }

        private void ReviewSavedFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateIfAuthorized(typeof(ReviewSavedFiles).FullName, "Review Saved Files");
            }
            catch (Exception ex)
            {
                Log("Review Saved Files Click", ex.Message);
            }
        }

        private void CheckUserPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Input dialog — ask for username
                var inputWin = new Window
                {
                    Title = "Check User Password",
                    Width = 360,
                    Height = 180,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStyle = WindowStyle.SingleBorderWindow
                };

                var grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var lbl = new TextBlock { Text = "Enter Username:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) };
                Grid.SetRow(lbl, 0);

                var txt = new TextBox { Height = 30, FontSize = 14, Padding = new Thickness(4) };
                Grid.SetRow(txt, 1);

                var btn = new Button { Content = "Get Password", Height = 34, Margin = new Thickness(0, 12, 0, 0), Background = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC)), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
                Grid.SetRow(btn, 2);

                btn.Click += (s, ev) =>
                {
                    string username = txt.Text.Trim();
                    if (string.IsNullOrEmpty(username)) { MessageBox.Show("Please enter a username."); return; }
                    var authService = new AuthService();
                    string plain = authService.GetPlainPassword(username);
                    MessageBox.Show($"Username : {username}\nPassword : {plain}", "User Password", MessageBoxButton.OK, MessageBoxImage.Information);
                    inputWin.Close();
                };

                grid.Children.Add(lbl);
                grid.Children.Add(txt);
                grid.Children.Add(btn);
                inputWin.Content = grid;
                inputWin.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Application.Current.Properties.Contains("UserId"))
                {
                    MessageBox.Show("Session expired. Please login again.");
                    return;
                }

                int userId = Convert.ToInt32(Application.Current.Properties["UserId"]);
                var authService = new AuthService();
                var win = new ChangePasswordWindow(userId, authService);
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                Log("Change Password", ex.Message);
            }
        }

        private void NavigateIfAuthorized(string pageName, string actionName)
        {
            try
            {
                if (RegionManager == null)
                {
                    MessageBox.Show("Navigation service not available.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // ✅ SAFE RoleId check
                if (!Application.Current.Properties.Contains("RoleId"))
                {
                    MessageBox.Show("Session expired. Please login again.");
                    return;
                }

                int roleId = Convert.ToInt32(Application.Current.Properties["RoleId"]);

                if (!HasAccess(roleId, pageName))
                {
                    MessageBox.Show("You are not authorized to access this page.",
                        "Access Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    Log(actionName, "Access Denied");
                    return;
                }
                var uri = new Uri(pageName, UriKind.Relative);
                RegionManager.RequestNavigate(RegionNames.OverContentRegion, uri);

                Log(actionName, $"{actionName} page opened");
            }
            catch (Exception ex)
            {
                Log(actionName, ex.Message);
            }
        }

        private bool HasAccess(int roleId, string pageName)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    const string sql = @"
SELECT COUNT(*)
FROM role_wise_rights_details r
INNER JOIN menuM m ON r.menu_id = m.menu_id
WHERE r.role_id = @role_id
AND m.menu_name = @page_name
AND r.is_active = 1
AND m.is_active = 1";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@role_id", roleId);
                        cmd.Parameters.AddWithValue("@page_name", pageName);

                        var result = cmd.ExecuteScalar();

                        return Convert.ToInt64(result) > 0;
                    }
                }
            }
            catch (SQLiteException dbEx)
            {
                Log("HasAccess DB Error", dbEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                Log("View User", ex.Message);
                return false;
            }
        }

        public void Log(string action, string description)
        {
            try
            {
                int userId = 0;

                if (Application.Current.Properties["UserId"] != null)
                    userId = Convert.ToInt32(Application.Current.Properties["UserId"]);

                using (var con = new SQLiteConnection(_connectionString))
                {
                    con.Open();

                    string sql = @"INSERT INTO AuditLogM
                          (action, description, is_active, inserted_by, inserted_date)
                          VALUES
                          (@action, @desc, 1, @uid, datetime('now','localtime'))";

                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@desc", description);
                        cmd.Parameters.AddWithValue("@uid", userId);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Log Error: " + ex.Message);
            }
        }
        //        private void BtnClick(object sender, RoutedEventArgs e)
        //        {
        //            try
        //            {
        //                if (!Application.Current.Properties.Contains("RoleId"))
        //                {
        //                    MessageBox.Show("Session expired. Please login again.");
        //                    return;
        //                }

        //                int roleId = Convert.ToInt32(Application.Current.Properties["RoleId"]);
        //                bool hasRights = false;

        //                // 2?? check permission directly from DB
        //                using (var conn = new SQLiteConnection(_connectionString))
        //                {
        //                    conn.Open();

        //                    string sql = @"
        //SELECT COUNT(*)
        //FROM role_wise_rights_details r
        //INNER JOIN menuM m ON r.menu_id = m.menu_id
        //WHERE r.role_id = @role_id
        //AND m.menu_name = @page_name
        //AND r.is_active = 1
        //AND m.is_active = 1";

        //                    using (var cmd = new SQLiteCommand(sql, conn))
        //                    {
        //                        cmd.Parameters.AddWithValue("@role_id", roleId);
        //                        cmd.Parameters.AddWithValue("@page_name", "UserRegistrationView");

        //                        long count = (long)cmd.ExecuteScalar();
        //                        hasRights = count > 0;
        //                    }
        //                }

        //                // 3?? if no rights
        //                if (!hasRights)
        //                {
        //                    MessageBox.Show("You are not authorized to open this user data.",
        //                        "Access Denied",
        //                        MessageBoxButton.OK,
        //                        MessageBoxImage.Warning);
        //                    return;
        //                }
        //                else
        //                {
        //                    //var uri = new Uri("UserDetails", UriKind.Relative);
        //                    var uri = new Uri("ViewAssignRole", UriKind.Relative);
        //                    RegionManager.RequestNavigate(RegionNames.OverContentRegion, uri);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show(ex.Message);
        //            }
        //        }


        //private void BtnClick_old(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        AssignRightsBL rights = new AssignRightsBL();
        //        int role_id = Convert.ToInt32(Application.Current.Properties["RoleId"]);
        //        string page_name = "UserRegistrationView";
        //        int userId = Convert.ToInt32(Application.Current.Properties["UserId"]);
        //        bool hasRights = rights.CheckAssignRights(role_id, page_name);

        //        if (!hasRights)
        //        {
        //            MessageBox.Show("You don't have rights.",
        //                "Access Denied",
        //                MessageBoxButton.OK,
        //                MessageBoxImage.Warning);
        //            return;
        //        }

        //        var uri = new Uri("UserRegistrationView", UriKind.Relative);
        //        RegionManager.RequestNavigate(RegionNames.OverContentRegion, uri);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}
    }

    internal class ChangePasswordWindow : Window
    {
        private readonly int _userId;
        private readonly AuthService _authService;
        private PasswordBox _pbCurrent, _pbNew, _pbConfirm;
        private TextBox _tbCurrent, _tbNew, _tbConfirm;
        private TextBlock _lblError;

        public ChangePasswordWindow(int userId, AuthService authService)
        {
            _userId = userId;
            _authService = authService;
            BuildUI();
        }

        private void BuildUI()
        {
            Title = "Change Password";
            Width = 460;
            Height = 430;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.SingleBorderWindow;

            var outer = new Grid { Margin = new Thickness(30) };
            for (int i = 0; i < 10; i++)
                outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock
            {
                Text = "Change Your Password",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2D, 0x3E, 0x50)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(title, 0);
            outer.Children.Add(title);

            AddLabel(outer, "Current Password *", 1);
            (_pbCurrent, _tbCurrent) = AddPasswordBox(outer, 2);

            AddLabel(outer, "New Password *", 3);
            (_pbNew, _tbNew) = AddPasswordBox(outer, 4);

            AddLabel(outer, "Confirm New Password *", 5);
            (_pbConfirm, _tbConfirm) = AddPasswordBox(outer, 6);

            _lblError = new TextBlock
            {
                Foreground = Brushes.Red,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(_lblError, 8);
            outer.Children.Add(_lblError);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            Grid.SetRow(btnPanel, 9);

            var btnSubmit = new Button
            {
                Content = "Change Password",
                Width = 140,
                Height = 34,
                Background = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnSubmit.Click += OnSubmit;

            var btnCancel = new Button { Content = "Cancel", Width = 80, Height = 34 };
            btnCancel.Click += (s, ev) => Close();

            btnPanel.Children.Add(btnSubmit);
            btnPanel.Children.Add(btnCancel);
            outer.Children.Add(btnPanel);

            Content = outer;
        }

        private void AddLabel(Grid grid, string text, int row)
        {
            var lbl = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2D, 0x3E, 0x50)),
                Margin = new Thickness(0, 8, 0, 3)
            };
            Grid.SetRow(lbl, row);
            grid.Children.Add(lbl);
        }

        private (PasswordBox, TextBox) AddPasswordBox(Grid grid, int row)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4)
            };

            var inner = new Grid();
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pb = new PasswordBox
            {
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Height = 28,
                FontSize = 14
            };
            Grid.SetColumn(pb, 0);

            var tb = new TextBox
            {
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Height = 28,
                FontSize = 14,
                Visibility = Visibility.Collapsed
            };
            Grid.SetColumn(tb, 0);

            var btnToggle = new ToggleButton
            {
                Content = "👁",
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            Grid.SetColumn(btnToggle, 1);
            btnToggle.Click += (s, ev) =>
            {
                if (tb.Visibility == Visibility.Visible)
                {
                    pb.Password = tb.Text;
                    pb.Visibility = Visibility.Visible;
                    tb.Visibility = Visibility.Collapsed;
                }
                else
                {
                    tb.Text = pb.Password;
                    tb.Visibility = Visibility.Visible;
                    pb.Visibility = Visibility.Collapsed;
                }
            };

            inner.Children.Add(pb);
            inner.Children.Add(tb);
            inner.Children.Add(btnToggle);

            border.Child = inner;
            Grid.SetRow(border, row);
            grid.Children.Add(border);
            return (pb, tb);
        }

        private static string GetPasswordText(PasswordBox pb, TextBox tb)
        {
            return tb.Visibility == Visibility.Visible ? tb.Text : pb.Password;
        }

        private void OnSubmit(object sender, RoutedEventArgs e)
        {
            string current = GetPasswordText(_pbCurrent, _tbCurrent);
            string newPass = GetPasswordText(_pbNew, _tbNew);
            string confirm = GetPasswordText(_pbConfirm, _tbConfirm);

            if (string.IsNullOrWhiteSpace(current)) { MessageBox.Show("Please enter your current password.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (string.IsNullOrWhiteSpace(newPass)) { MessageBox.Show("Please enter a new password.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (string.IsNullOrWhiteSpace(confirm)) { MessageBox.Show("Please confirm your new password.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (newPass != confirm) { MessageBox.Show("New password and confirm password do not match.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (newPass == current) { MessageBox.Show("New password cannot be the same as current password.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            string result = _authService.ChangePassword(_userId, current, newPass);
            if (result == "Success")
            {
                MessageBox.Show("Password changed successfully! Please login again with your new password.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();

                // Logout and go back to login screen
                Application.Current.Properties.Clear();
                Application.Current.MainWindow.Hide();

                var login = new LoginWindow();
                bool? loginResult = login.ShowDialog();

                if (loginResult == true)
                    Application.Current.MainWindow.Show();
                else
                    Application.Current.Shutdown();
            }
            else
            {
                MessageBox.Show(result, "Password Change Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
