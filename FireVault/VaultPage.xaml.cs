using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using System.Threading.Tasks;
using System;
using Microsoft.UI;
using Windows.Graphics;
using Microsoft.UI.Windowing;
using FireVaultCore.Models;
using FireVaultCore;
using FireVaultCore.Helpers;

namespace FireVault
{
    public sealed partial class VaultPage : Page
    {
        public ObservableCollection<VaultItem> VaultItems { get; set; }
        private DatabaseManager _databaseManager;
        private string _username;
        public string PageTitle => $"Your Vault: {_username}";

        public VaultPage()
        {
            this.InitializeComponent();
            VaultItems = new ObservableCollection<VaultItem>();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            var currentUser = await ApiService.GetInstance().GetCurrentUserAsync();
            _username = currentUser.Username;
            _databaseManager = new DatabaseManager(_username); // Update: Removed currentUser.PasswordHash
            await _databaseManager.InitializeDatabaseAsync();
            await LoadVaultItems();

            SearchBox.TextChanged += SearchBox_TextChanged;
        }

        private async Task LoadVaultItems()
        {
            VaultItems.Clear();
            var items = await _databaseManager.GetVaultItemsAsync();
            foreach (var item in items)
            {
                VaultItems.Add(item);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filtered = VaultItems.Where(item =>
                item.Title.ToLower().Contains(SearchBox.Text.ToLower()));
            VaultItemsGridView.ItemsSource = filtered;
        }

        private async void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            var addItemDialog = new AddItemDialog();
            ContentDialog dialog = new ContentDialog
            {
                Title = "Add Vault Item",
                Content = addItemDialog,
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Add",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await _databaseManager.SaveVaultItemAsync(
                    addItemDialog.Title,
                    addItemDialog.Data,
                    addItemDialog.Type,
                    addItemDialog.Password // Update: Added addItemDialog.Password
                );
                await LoadVaultItems();
            }
        }

        private void VaultItem_Click(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as VaultItem;
            if (item != null)
            {
                var detailsWindow = new Window
                {
                    Title = "Vault Item Details",
                    Content = new VaultItemDetailsWindow(item, _databaseManager, async () => await LoadVaultItems()),
                };

           

                var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(detailsWindow);

                // Retrieve the WindowId
                var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);

                // Retrieve the AppWindow
                var appWindow = AppWindow.GetFromWindowId(windowId);
                appWindow.SetIcon("details.ico");

                // Set the window size
                int width = 700;  // Set your desired width
                int height = 1000; // Set your desired height
                appWindow.Resize(new SizeInt32(width, height));
                detailsWindow.Activate();
            }
        }

        private async void ViewApiKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = await ApiService.GetInstance().GetCurrentUserAsync();

            TextBox apiKeyTextBox = new TextBox
            {
                Text = currentUser.ApiKey,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = "Your API Key",
                Content = apiKeyTextBox,
                CloseButtonText = "Close",
                PrimaryButtonText = "Regenerate",
                SecondaryButtonText = "Copy",
                XamlRoot = this.XamlRoot,
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await ApiService.GetInstance().RegenerateApiKeyAsync();
                ViewApiKeyButton_Click(sender, e);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetText(currentUser.ApiKey);
                Clipboard.SetContent(dataPackage);
            }
        }

        private async void ManageTrustedAppsButton_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = await ApiService.GetInstance().GetCurrentUserAsync();
            var trustedAppsDialog = new TrustedAppsDialog(currentUser.TrustedApps);

            ContentDialog dialog = new ContentDialog
            {
                Title = "Manage Trusted Apps",
                Content = trustedAppsDialog,
                CloseButtonText = "Close",
                PrimaryButtonText = "Save",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                currentUser.TrustedApps = trustedAppsDialog.TrustedApps.ToList();
                await ApiService.GetInstance().UpdateUserAsync(currentUser);
            }
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

