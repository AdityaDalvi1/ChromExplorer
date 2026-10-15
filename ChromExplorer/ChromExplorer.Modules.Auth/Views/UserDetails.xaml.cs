using ChromExplorer.Infrastructure;
using ChromExplorer.Modules.Auth.Services;
using ChromExplorer.Modules.Auth.ViewModels;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using static ChromExplorer.Modules.Auth.Models.User;

namespace ChromExplorer.Modules.Auth.Views

{

    // [Export("UserDetails", typeof(UserDetails))]
    [Export(typeof(UserDetails))]

    [PartCreationPolicy(CreationPolicy.NonShared)]

    public partial class UserDetails : UserControl, INavigationAware

    {

        private readonly UserDetailsViewModel _userDetails;

        [Import]

        public IRegionManager RegionManager { get; set; }

        public UserDetails()

        {

            InitializeComponent();

            _userDetails = new UserDetailsViewModel();

            LoadData();

        }

        private void LoadData()

        {

            List<UserAccount> users = new List<UserAccount>();

            users = _userDetails.GetUserDetails();

            UserGrid.ItemsSource = users;

        }

        private void CloseView()

        {

            if (RegionManager == null)

            {

                return;

            }

            var popupRegion = this.RegionManager.Regions[RegionNames.OverContentRegion];

            if (popupRegion != null)

            {

                popupRegion.Deactivate(this);

            }

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)

        {

            this.CloseView();

        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)

        {

            if (RegionManager == null)

            {

                MessageBox.Show("RegionManager is not available. Ensure MEF has injected it.", "Error");

                return;

            }

            else

            {

                RegionManager.RequestNavigate(RegionNames.OverContentRegion, "UserRegistrationView");

            }

        }

        public void OnNavigatedTo(NavigationContext navigationContext)

        {

            LoadData();

        }

        public bool IsNavigationTarget(NavigationContext navigationContext)

        {

            return true;

        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        private void Action_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string action = btn?.Tag?.ToString();
            UserAccount user = btn?.CommandParameter as UserAccount;

            if (user == null) return;

            string reasonTitle = action == "Edit" ? "Reason for Edit"
                : action == "Activate" ? "Reason for Activation"
                : action == "Unlock" ? "Reason for Unlock"
                : "Reason for Deactivation";

            string reason = ShowReasonDialog(reasonTitle);
            if (reason == null) return;

            if (action == "Unlock")
            {
                var confirm = MessageBox.Show(
                    $"Are you sure you want to unlock user '{user.UserName}'?\n\nReason: {reason}",
                    "Confirm Unlock",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                if (!ConfirmCurrentUserPassword("User - Unlock", reason)) return;

                _userDetails.UnlockUser(user.UserName, reason);
                MessageBox.Show("User unlocked successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            else if (action == "Edit")
            {
                if (!ConfirmCurrentUserPassword("User - Edit", reason)) return;

                new AuthService().Log("User Edit",
                    $"Edit initiated for user '{user.UserName}' | Reason: {reason}");

                string uri = $"UserRegistrationView?userId={user.UserId}&mode=edit&reason={Uri.EscapeDataString(reason)}";
                RegionManager.RequestNavigate(RegionNames.OverContentRegion, new Uri(uri, UriKind.Relative));
            }
            else if (action == "Delete")
            {
                var confirm = MessageBox.Show(
                    $"Are you sure you want to deactivate user '{user.UserName}'?\n\nReason: {reason}",
                    "Confirm Deactivation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;

                if (!ConfirmCurrentUserPassword("User - Deactivate", reason)) return;

                bool deactivated = _userDetails.DeleteUser(user.UserId, user.FullName, reason);
                if (deactivated)
                {
                    MessageBox.Show("User deactivated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show("Deactivation failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (action == "Activate")
            {
                var confirm = MessageBox.Show(
                    $"Are you sure you want to activate user '{user.UserName}'?\n\nReason: {reason}",
                    "Confirm Activation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                if (!ConfirmCurrentUserPassword("User - Activate", reason)) return;

                bool activated = _userDetails.ActivateUser(user.UserId, user.FullName, reason);
                if (activated)
                {
                    MessageBox.Show("User activated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show("Activation failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string ShowReasonDialog(string title)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 420,
                Height = 210,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock
            {
                Text = "Please enter a reason:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(label, 0);

            var txtReason = new TextBox
            {
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                Padding = new Thickness(6)
            };
            Grid.SetRow(txtReason, 1);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };
            Grid.SetRow(btnPanel, 2);

            string result = null;

            var btnOk = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0), Background = System.Windows.Media.Brushes.SteelBlue, Foreground = System.Windows.Media.Brushes.White };
            btnOk.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtReason.Text))
                {
                    MessageBox.Show("Reason cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = txtReason.Text.Trim();
                dialog.DialogResult = true;
            };

            var btnCancel = new Button { Content = "Cancel", Width = 80, Height = 30 };
            btnCancel.Click += (s, ev) => { dialog.DialogResult = false; };

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);

            grid.Children.Add(label);
            grid.Children.Add(txtReason);
            grid.Children.Add(btnPanel);
            dialog.Content = grid;

            bool? ok = dialog.ShowDialog();
            return ok == true ? result : null;
        }

        // Prompts for the current user's password and verifies it before a save/deactivate/edit
        // is allowed to proceed. A wrong password is logged (with the reason, for context) since
        // that's a real security-relevant event; a plain Cancel is not (the user just backed out,
        // nothing was attempted). Returns false on cancel or a wrong password — callers must not
        // proceed with the action in that case.
        private bool ConfirmCurrentUserPassword(string actionContext, string reason)
        {
            var password = ShowPasswordDialog();
            if (password == null)
                return false;

            if (!new AuthService().VerifyCurrentUserPassword(password))
            {
                new AuthService().Log($"{actionContext} - Password Verification Failed",
                    $"Incorrect password entered | Reason: {reason}");

                MessageBox.Show("Incorrect password. Action cancelled.", "Authentication Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private string ShowPasswordDialog()
        {
            var dialog = new Window
            {
                Title = "Confirm Password",
                Width = 380,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var label = new TextBlock
            {
                Text = "Enter your password to confirm this action:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(label, 0);

            var (txtPassword, txtPasswordVisible) = AddPasswordFieldWithEyeToggle(grid, 1);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };
            Grid.SetRow(btnPanel, 2);

            string result = null;

            var btnOk = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0), Background = System.Windows.Media.Brushes.SteelBlue, Foreground = System.Windows.Media.Brushes.White };
            btnOk.Click += (s, ev) =>
            {
                var entered = GetPasswordFieldText(txtPassword, txtPasswordVisible);
                if (string.IsNullOrEmpty(entered))
                {
                    MessageBox.Show("Password cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = entered;
                dialog.DialogResult = true;
            };

            var btnCancel = new Button { Content = "Cancel", Width = 80, Height = 30 };
            btnCancel.Click += (s, ev) => { dialog.DialogResult = false; };

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);

            grid.Children.Add(label);
            grid.Children.Add(btnPanel);
            dialog.Content = grid;

            bool? ok = dialog.ShowDialog();
            return ok == true ? result : null;
        }

        // Shared "eye" toggle so a password dialog's field can be revealed as plain text — a
        // bordered PasswordBox + TextBox pair, with a 👁 ToggleButton swapping which one is
        // visible (and carrying the typed value across the swap).
        private (PasswordBox, TextBox) AddPasswordFieldWithEyeToggle(Grid parentGrid, int row)
        {
            var border = new Border
            {
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 0, 4, 0)
            };
            Grid.SetRow(border, row);

            var inner = new Grid();
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pb = new PasswordBox
            {
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                Height = 28,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(pb, 0);

            var tb = new TextBox
            {
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                Height = 28,
                VerticalContentAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            Grid.SetColumn(tb, 0);

            var btnToggle = new ToggleButton
            {
                Content = "👁",
                Width = 26,
                Height = 26,
                Background = System.Windows.Media.Brushes.Transparent,
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
            parentGrid.Children.Add(border);
            return (pb, tb);
        }

        private static string GetPasswordFieldText(PasswordBox pb, TextBox tb)
        {
            return tb.Visibility == Visibility.Visible ? tb.Text : pb.Password;
        }
    }
}

