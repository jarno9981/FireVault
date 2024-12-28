using FireVaultCore.Helpers;
using FireVaultCore.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace FireVault
{
    public sealed partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            this.InitializeComponent();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await ShowErrorDialog("Username and password are required.");
                return;
            }

            if (password != confirmPassword)
            {
                await ShowErrorDialog("Passwords do not match.");
                return;
            }

            try
            {
                User newUser = await ApiService.GetInstance().RegisterUserAsync(username, password);
                await ShowSuccessDialog($"Account created successfully. Your API Key is: {newUser.ApiKey}");
                Frame.Navigate(typeof(LoginPage));
            }
            catch (Exception ex)
            {
                await ShowErrorDialog(ex.Message);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoginPage));
        }

        private async Task ShowErrorDialog(string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot,
            };
            await dialog.ShowAsync();
        }

        private async Task ShowSuccessDialog(string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Success",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot,
            };
            await dialog.ShowAsync();
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}