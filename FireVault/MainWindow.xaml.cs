using FireVaultCore.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using FireVaultCore.Models;
using System.Threading.Tasks;
using FireVaultCore;
using Microsoft.UI.Xaml.Controls;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace FireVault
{
    public sealed partial class MainWindow : Window
    {
        private AppWindow appWindow;
        private FireVaultManager _fireVaultManager;
        private UserInterface _userInterface;
        private HttpListener _httpListener;

        public MainWindow()
        {
            this.InitializeComponent();
            InitializeFireVaultManager();
            NavigateBasedOnLaunchArgs();
            TitleTop();
            StartHttpListener();
        }

        private void InitializeFireVaultManager()
        {
            _userInterface = new UserInterface();
            _userInterface.SetShowTrustPopupMethod(ShowTrustPopupAsync);
            _userInterface.SetShowLoginPromptMethod(ShowLoginPromptAsync);
            _fireVaultManager = new FireVaultManager(_userInterface);
        }

        private void StartHttpListener()
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:5000/");
            _httpListener.Start();
            ListenForRequests();
        }

        private async void ListenForRequests()
        {
            while (_httpListener.IsListening)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/trust")
                    {
                        await HandleTrustRequest(context);
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    System.Diagnostics.Debug.WriteLine($"Error in ListenForRequests: {ex.Message}");
                }
            }
        }

        private async Task HandleTrustRequest(HttpListenerContext context)
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                string requestBody = await reader.ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<ExternalAppRequest>(requestBody);

                bool isTrusted = await ShowTrustPopupAsync(request);

                var response = new { Trusted = isTrusted };
                string responseJson = JsonConvert.SerializeObject(response);

                context.Response.ContentType = "application/json";
                using (var writer = new StreamWriter(context.Response.OutputStream))
                {
                    await writer.WriteAsync(responseJson);
                }
            }
        }

        private async Task<bool> ShowTrustPopupAsync(ExternalAppRequest request)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "External App Access Request",
                Content = $"The app '{request.AppName}' (ID: {request.AppId}) is requesting access to your FireVault data. Do you want to trust this app?",
                PrimaryButtonText = "Trust",
                SecondaryButtonText = "Do Not Trust",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.Content.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private async Task<bool> ShowLoginPromptAsync()
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "Login Required",
                Content = "Please log in to FireVault to continue.",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private void NavigateBasedOnLaunchArgs()
        {
            var apiService = ApiService.GetInstance();
            var users = apiService.GetAllUsers();

            if (users.Any())
            {
                // If there are existing users, navigate to the login page
                ContentFrame.Navigate(typeof(LoginPage));
            }
            else
            {
                // If there are no users, navigate to the register page
                ContentFrame.Navigate(typeof(RegisterPage));
            }
        }

        public void TitleTop()
        {
            nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon("Logo.ico");

            if (!AppWindowTitleBar.IsCustomizationSupported())
            {
                throw new Exception("Unsupported OS version.");
            }

            AppWindowTitleBar titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            Windows.UI.Color btnColor = Colors.Transparent;
            titleBar.BackgroundColor = titleBar.ButtonBackgroundColor =
                titleBar.InactiveBackgroundColor = titleBar.ButtonInactiveBackgroundColor =
                titleBar.ButtonHoverBackgroundColor = btnColor;
        }

        // Example method to handle external app requests
        public async Task<bool> HandleExternalAppRequest(string appId, string appName)
        {
            return await _fireVaultManager.HandleExternalAppRequest(appId, appName);
        }
    }
}

