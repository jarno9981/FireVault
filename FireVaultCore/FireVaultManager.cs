using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using FireVaultCore.Helpers;
using FireVaultCore.Models;
using FireVaultCore;

namespace FireVaultCore
{
    public class FireVaultManager
    {
        private readonly ApiService _apiService;
        private DatabaseManager _databaseManager;
        private readonly UserInterface _userInterface;
        private readonly HttpListener _httpListener;
        private const string ListenerPrefix = "http://localhost:5000/";

        public FireVaultManager(UserInterface userInterface)
        {
            _apiService = ApiService.GetInstance();
            _userInterface = userInterface;
            UpdateDatabaseManager();
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(ListenerPrefix);
        }

        private void UpdateDatabaseManager()
        {
            var currentUser = _apiService.GetCurrentUserAsync().Result;
            _databaseManager = new DatabaseManager(currentUser?.Username ?? "default");
        }

        public void StartListening()
        {
            _httpListener.Start();
            Task.Run(async () => await HandleIncomingRequests());
        }

        private async Task HandleIncomingRequests()
        {
            while (_httpListener.IsListening)
            {
                var context = await _httpListener.GetContextAsync();
                if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/trust")
                {
                    await HandleTrustRequest(context);
                }
                else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/login")
                {
                    await HandleLoginRequest(context);
                }
                else if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/data")
                {
                    await HandleDataRequest(context);
                }
                else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/validate-api-key")
                {
                    await HandleValidateApiKeyRequest(context);
                }
            }
        }

        private async Task HandleValidateApiKeyRequest(HttpListenerContext context)
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                string requestBody = await reader.ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<ApiKeyValidationRequest>(requestBody);

                var user = await _apiService.ValidateApiKeyAsync(request.ApiKey);
                bool isValid = user != null;

                var response = new { IsValid = isValid };
                string responseJson = JsonConvert.SerializeObject(response);

                context.Response.ContentType = "application/json";
                using (var writer = new StreamWriter(context.Response.OutputStream))
                {
                    await writer.WriteAsync(responseJson);
                }
            }
        }

        private async Task HandleTrustRequest(HttpListenerContext context)
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                string requestBody = await reader.ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<ExternalAppRequest>(requestBody);

                var currentUser = await _apiService.GetCurrentUserAsync();
                bool isTrusted = currentUser != null && currentUser.TrustedApps.Contains(request.AppId);

                if (!isTrusted)
                {
                    isTrusted = await _userInterface.ShowTrustPopupAsync(request);
                    if (isTrusted)
                    {
                        await _apiService.TrustAppAsync(request.AppId, request.AppName);
                    }
                }

                var response = new { Trusted = isTrusted };
                string responseJson = JsonConvert.SerializeObject(response);

                context.Response.ContentType = "application/json";
                using (var writer = new StreamWriter(context.Response.OutputStream))
                {
                    await writer.WriteAsync(responseJson);
                }
            }
        }

        private async Task HandleLoginRequest(HttpListenerContext context)
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                string requestBody = await reader.ReadToEndAsync();
                var loginRequest = JsonConvert.DeserializeObject<ExternalLoginRequest>(requestBody);

                bool loginSuccess = await _apiService.LoginExternalAsync(loginRequest.ApiKey, loginRequest.Username, loginRequest.Password);

                var response = new { Success = loginSuccess };
                string responseJson = JsonConvert.SerializeObject(response);

                context.Response.ContentType = "application/json";
                using (var writer = new StreamWriter(context.Response.OutputStream))
                {
                    await writer.WriteAsync(responseJson);
                }
            }
        }

        private async Task HandleDataRequest(HttpListenerContext context)
        {
            string apiKey = context.Request.Headers["X-API-Key"];
            var user = await _apiService.ValidateApiKeyAsync(apiKey);

            if (user != null)
            {
                var vaultItems = await _apiService.GetVaultItemsAsync();
                string responseJson = JsonConvert.SerializeObject(vaultItems);

                context.Response.ContentType = "application/json";
                using (var writer = new StreamWriter(context.Response.OutputStream))
                {
                    await writer.WriteAsync(responseJson);
                }
            }
            else
            {
                context.Response.StatusCode = 401; // Unauthorized
            }
        }

        public async Task<bool> HandleExternalAppRequest(string appId, string appName)
        {
            var currentUser = await _apiService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                bool loginSuccess = await _userInterface.ShowLoginPromptAsync();
                if (!loginSuccess)
                {
                    return false;
                }
                currentUser = await _apiService.GetCurrentUserAsync();
            }

            if (currentUser.TrustedApps.Contains(appId))
            {
                return true;
            }

            var request = new ExternalAppRequest(appId, appName);
            bool isTrusted = await _userInterface.ShowTrustPopupAsync(request);

            if (isTrusted)
            {
                return await _apiService.TrustAppAsync(appId, appName);
            }

            return false;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            bool loginSuccess = await _apiService.LoginInternalAsync(username, password);
            if (loginSuccess)
            {
                UpdateDatabaseManager();
                await _databaseManager.InitializeDatabaseAsync();
            }
            return loginSuccess;
        }

        public async Task<bool> LoginExternalAsync(string apiKey, string username, string password)
        {
            bool loginSuccess = await _apiService.LoginExternalAsync(apiKey, username, password);
            if (loginSuccess)
            {
                UpdateDatabaseManager();
                await _databaseManager.InitializeDatabaseAsync();
            }
            return loginSuccess;
        }

        public async Task<User> RegisterUserAsync(string username, string password)
        {
            User newUser = await _apiService.RegisterUserAsync(username, password);
            if (newUser != null)
            {
                UpdateDatabaseManager();
                await _databaseManager.InitializeDatabaseAsync();
            }
            return newUser;
        }

        public async Task<List<VaultItem>> GetVaultItemsAsync()
        {
            return await _databaseManager.GetVaultItemsAsync();
        }

        public async Task SaveVaultItemAsync(string title, string data, string type, string password)
        {
            await _databaseManager.SaveVaultItemAsync(title, data, type, password);
        }

        public async Task<bool> DeleteVaultItemAsync(string id)
        {
            return await _databaseManager.DeleteVaultItemAsync(id);
        }

        public async Task<string> DecryptVaultItemAsync(VaultItem item, string password)
        {
            return await _databaseManager.DecryptVaultItemAsync(item, password);
        }

        public async Task<User> GetCurrentUserAsync()
        {
            return await _apiService.GetCurrentUserAsync();
        }

        public async Task RegenerateApiKeyAsync()
        {
            await _apiService.RegenerateApiKeyAsync();
        }

        public string GetGlyphForType(string type)
        {
            return VaultItemHelper.GetGlyphForType(type);
        }
    }

    public class ExternalLoginRequest
    {
        public string ApiKey { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ApiKeyValidationRequest
    {
        public string ApiKey { get; set; }
    }
}

