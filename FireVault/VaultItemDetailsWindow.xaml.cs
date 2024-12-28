using FireVaultCore;
using FireVaultCore.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace FireVault
{
    public sealed partial class VaultItemDetailsWindow : UserControl
    {
        private VaultItem _item;
        private DatabaseManager _databaseManager;
        private Action _onItemDeleted;

        public VaultItemDetailsWindow(VaultItem item, DatabaseManager databaseManager, Action onItemDeleted)
        {
            this.InitializeComponent();
            _item = item;
            _databaseManager = databaseManager;
            _onItemDeleted = onItemDeleted;

            TitleTextBlock.Text = item.Title;
            TypeTextBlock.Text = $"Type: {item.Type}";
        }

        private async void UnlockButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string decryptedData = await _databaseManager.DecryptVaultItemAsync(_item, PasswordBox.Password);
                DecryptedDataTextBox.Text = decryptedData;
                DecryptedDataTextBox.Visibility = Visibility.Visible;
                CopyButton.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;
                UnlockButton.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Failed to decrypt. Please check your password and try again.");
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(DecryptedDataTextBox.Text);
            Clipboard.SetContent(dataPackage);
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog confirmDialog = new ContentDialog
            {
                Title = "Confirm Deletion",
                Content = "Are you sure you want to delete this item? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                bool deleted = await _databaseManager.DeleteVaultItemAsync(_item.Title);
                if (deleted)
                {
                    _onItemDeleted?.Invoke();
                    CloseButton_Click(sender, e);
                }
                else
                {
                    await ShowErrorDialog("Failed to delete the item. Please try again.");
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private async Task ShowErrorDialog(string message)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await errorDialog.ShowAsync();
        }
    }
}

