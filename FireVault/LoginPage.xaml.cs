using FireVaultCore.Helpers;
using FireVaultCore.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FireVault
{
    public sealed partial class LoginPage : Page
    {
        private ObservableCollection<User> Users { get; set; }

        public LoginPage()
        {
            this.InitializeComponent();
            Users = new ObservableCollection<User>();
            LoadUsers();
            UserCardsRepeater.ItemsSource = Users;
        }

        private void LoadUsers()
        {
            var apiService = ApiService.GetInstance();
            var users = apiService.GetAllUsers();
            Users.Clear();
            if (users != null && users.Count > 0)
            {
                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }
            else
            {
                // If no users are found, navigate to the RegisterPage
                Frame.Navigate(typeof(RegisterPage));
            }
        }

        private async void UserCard_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                await ShowErrorDialog("An error occurred. Please try again.");
                return;
            }

            var username = button.Tag as string;
            if (string.IsNullOrEmpty(username))
            {
                await ShowErrorDialog("User data not found. Please try again.");
                return;
            }

            ContentDialog passwordDialog = new ContentDialog
            {
                Title = $"Enter password for {username}",
                Content = new PasswordBox(),
                PrimaryButtonText = "Login",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
            };

            if (await passwordDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var passwordBox = passwordDialog.Content as PasswordBox;
                if (passwordBox == null)
                {
                    await ShowErrorDialog("An error occurred. Please try again.");
                    return;
                }

                try
                {
                    if (await ApiService.GetInstance().LoginInternalAsync(username, passwordBox.Password))
                    {
                        Frame.Navigate(typeof(VaultPage));
                    }
                    else
                    {
                        await ShowErrorDialog("Invalid password. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorDialog($"An error occurred during login: {ex.Message}");
                }
            }
        }

        private void AddNewUser_Click(object sender, RoutedEventArgs e)
        {
            if (Users.Count >= 5)
            {
                ShowErrorDialog("Maximum number of users (5) reached.");
                return;
            }
            Frame.Navigate(typeof(RegisterPage));
        }

        private async Task ShowErrorDialog(string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}

