using ChromExplorer.Modules.Auth.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChromExplorer.Modules.Quan.Services
{
    /// <summary>"Enter a reason, then confirm your password" gate used before destructive RSD
    /// actions — same flow as the user/role/rights screens in the Auth module (reason dialog,
    /// then AuthService.VerifyCurrentUserPassword). Cancel backs out silently; a wrong password
    /// is written to the audit log.</summary>
    public static class ReasonPasswordPrompt
    {
        /// <summary>Returns true only when a reason was entered AND the current user's password
        /// was verified. <paramref name="reason"/> is the entered reason in that case.</summary>
        public static bool TryAuthorize(string action, string reasonTitle, out string reason)
        {
            reason = ShowDialog(reasonTitle, "Please enter a reason:", false, 210, 60);
            if (reason == null)
                return false;

            string password = ShowDialog("Confirm Password", "Enter your password to confirm this action:", true, 180, 0);
            if (password == null)
                return false;

            var auth = new AuthService();
            if (!auth.VerifyCurrentUserPassword(password))
            {
                auth.Log(action + " - Password Verification Failed", "Incorrect password entered | Reason: " + reason);
                MessageBox.Show("Incorrect password. Action cancelled.", "Authentication Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        // One small modal used for both prompts; returns the trimmed text, or null on Cancel.
        private static string ShowDialog(string title, string prompt, bool isPassword, double height, double inputHeight)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = height,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Owner = Application.Current?.MainWindow
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock { Text = prompt, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8), TextWrapping = TextWrapping.Wrap };
            Grid.SetRow(label, 0);

            Control input;
            if (isPassword)
                input = new PasswordBox { Height = 28, VerticalContentAlignment = VerticalAlignment.Center };
            else
                input = new TextBox { Height = inputHeight, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(6) };
            input.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetRow(input, 1);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            Grid.SetRow(buttons, 2);

            string result = null;

            var ok = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0), Background = Brushes.SteelBlue, Foreground = Brushes.White, IsDefault = isPassword };
            ok.Click += (s, e) =>
            {
                string text = isPassword ? ((PasswordBox)input).Password : ((TextBox)input).Text.Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show((isPassword ? "Password" : "Reason") + " cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = text;
                dialog.DialogResult = true;
            };

            var cancel = new Button { Content = "Cancel", Width = 80, Height = 30, IsCancel = true };

            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            grid.Children.Add(label);
            grid.Children.Add(input);
            grid.Children.Add(buttons);
            dialog.Content = grid;

            return dialog.ShowDialog() == true ? result : null;
        }
    }
}
