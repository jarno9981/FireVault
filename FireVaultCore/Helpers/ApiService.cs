using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using FireVaultCore.Helpers;
using FireVaultCore.Models;

namespace FireVaultCore.Helpers
{
    public class ApiService
    {
        private static ApiService _instance;
        private User _currentUser;
        private List<User> _users;
        private List<VaultItem> _vaultItems;
        private readonly string _usersFilePath;
        private readonly string _vaultItemsFilePath;
        private readonly string _appDataFolder;

        private ApiService()
        {
            _users = new List<User>();
            _vaultItems = new List<VaultItem>();

            _appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FireVault");
            _usersFilePath = Path.Combine(_appDataFolder, "users.json");
            _vaultItemsFilePath = Path.Combine(_appDataFolder, "vaultitems.json");

            EnsureDirectoryAndFilesExist();
            LoadUsers();
            LoadVaultItems();
        }

        public static ApiService GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ApiService();
            }
            return _instance;
        }

        private void EnsureDirectoryAndFilesExist()
        {
            if (!Directory.Exists(_appDataFolder))
            {
                Directory.CreateDirectory(_appDataFolder);
            }

            if (!File.Exists(_usersFilePath))
            {
                File.WriteAllText(_usersFilePath, JsonConvert.SerializeObject(new List<User>()));
            }

            if (!File.Exists(_vaultItemsFilePath))
            {
                File.WriteAllText(_vaultItemsFilePath, JsonConvert.SerializeObject(new List<VaultItem>()));
            }
        }

        public async Task<User> ValidateApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
            }

            try
            {
                await Task.Delay(100); // Simulate some asynchronous work

                return _users.FirstOrDefault(u => u.ApiKey == apiKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating API key: {ex.Message}");
                throw new Exception("Failed to validate API key. Please try again.", ex);
            }
        }

        public async Task<bool> LoginExternalAsync(string apiKey, string username, string password)
        {
            try
            {
                var user = await ValidateApiKeyAsync(apiKey);
                if (user != null && user.Username == username)
                {
                    string hashedPassword = HashPassword(password, user.Salt);
                    if (hashedPassword == user.PasswordHash)
                    {
                        _currentUser = user;
                        await CreateSessionAsync(user);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during external login: {ex.Message}");
                throw new Exception("Failed to login. Please try again.", ex);
            }
        }

        public async Task<bool> LoginInternalAsync(string username, string password)
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                {
                    string hashedPassword = HashPassword(password, user.Salt);
                    if (hashedPassword == user.PasswordHash)
                    {
                        _currentUser = user;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during internal login: {ex.Message}");
                throw new Exception("Failed to login. Please try again.", ex);
            }
        }

        public async Task<User> RegisterUserAsync(string username, string password)
        {
            try
            {
                if (_users.Count >= 5)
                {
                    throw new Exception("Maximum number of users (5) reached.");
                }

                if (_users.Any(u => u.Username == username))
                {
                    throw new Exception("Username already exists.");
                }

                string salt = GenerateSalt();
                string passwordHash = HashPassword(password, salt);
                string apiKey = GenerateApiKey(username);

                User newUser = new User(username, passwordHash, salt, apiKey);
                _users.Add(newUser);
                SaveUsers();
                return newUser;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering user: {ex.Message}");
                throw new Exception("Failed to register user. Please try again.");
            }
        }

        public async Task<List<VaultItem>> GetVaultItemsAsync()
        {
            try
            {
                if (_currentUser == null)
                {
                    throw new Exception("No user is currently logged in.");
                }
                return _vaultItems.Where(item => item.Username == _currentUser.Username).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting vault items: {ex.Message}");
                throw new Exception("Failed to retrieve vault items. Please try again.");
            }
        }

        public async Task DeleteVaultItemAsync(string title)
        {
            try
            {
                if (_currentUser == null)
                {
                    throw new Exception("No user is currently logged in.");
                }

                var itemToRemove = _vaultItems.FirstOrDefault(item =>
                    item.Username == _currentUser.Username && item.Title == title);

                if (itemToRemove != null)
                {
                    _vaultItems.Remove(itemToRemove);
                    SaveVaultItems();
                }
                else
                {
                    throw new Exception("Item not found.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting vault item: {ex.Message}");
                throw new Exception("Failed to delete vault item. Please try again.");
            }
        }

        public async Task<string> GetDecryptedVaultItemAsync(VaultItem item, string password)
        {
            try
            {
                if (_currentUser == null)
                {
                    throw new Exception("No user is currently logged in.");
                }

                if (item.Username != _currentUser.Username)
                {
                    throw new Exception("Access denied to this vault item.");
                }

                return Encryption.Decrypt(item.EncryptedData, password);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error decrypting vault item: {ex.Message}");
                throw new Exception("Failed to decrypt vault item. Please try again.");
            }
        }

        public List<User> GetAllUsers()
        {
            return _users ?? new List<User>();
        }

        public async Task<User> GetCurrentUserAsync()
        {
            return _currentUser;
        }

        public async Task RegenerateApiKeyAsync()
        {
            try
            {
                if (_currentUser == null)
                {
                    throw new Exception("No user is currently logged in.");
                }
                _currentUser.ApiKey = GenerateApiKey(_currentUser.Username);
                SaveUsers();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error regenerating API key: {ex.Message}");
                throw new Exception("Failed to regenerate API key. Please try again.");
            }
        }

        public async Task UpdateUserAsync(User user)
        {
            try
            {
                var index = _users.FindIndex(u => u.Username == user.Username);
                if (index != -1)
                {
                    _users[index] = user;
                    SaveUsers();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user: {ex.Message}");
                throw new Exception("Failed to update user. Please try again.");
            }
        }

        public async Task<bool> TrustAppAsync(string appId, string appName)
        {
            try
            {
                if (_currentUser == null)
                {
                    throw new InvalidOperationException("No user is currently logged in.");
                }

                if (string.IsNullOrWhiteSpace(appId))
                {
                    throw new ArgumentException("App ID cannot be null or empty.", nameof(appId));
                }

                _currentUser.TrustApp(appId);
                await UpdateUserAsync(_currentUser);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error trusting app: {ex.Message}");
                throw new Exception("Failed to trust the app. Please try again.", ex);
            }
        }

        private string GenerateApiKey(string username)
        {
            byte[] apiKeyBytes = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(apiKeyBytes);
            }
            return $"{username}_{Convert.ToBase64String(apiKeyBytes)}";
        }

        private string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 10000))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hash);
            }
        }

        private void LoadUsers()
        {
            try
            {
                string json = File.ReadAllText(_usersFilePath);
                _users = JsonConvert.DeserializeObject<List<User>>(json) ?? new List<User>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading users: {ex.Message}");
                _users = new List<User>();
                SaveUsers();
            }
        }

        private void SaveUsers()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_users, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(_usersFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving users: {ex.Message}");
                throw new Exception("Failed to save user data. Please try again.");
            }
        }

        private void LoadVaultItems()
        {
            try
            {
                string json = File.ReadAllText(_vaultItemsFilePath);
                _vaultItems = JsonConvert.DeserializeObject<List<VaultItem>>(json) ?? new List<VaultItem>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading vault items: {ex.Message}");
                _vaultItems = new List<VaultItem>();
                SaveVaultItems();
            }
        }

        private void SaveVaultItems()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_vaultItems, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(_vaultItemsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving vault items: {ex.Message}");
                throw new Exception("Failed to save vault items. Please try again.");
            }
        }

        public async Task<bool> ValidateSessionAsync(string apiKey)
        {
            var user = await ValidateApiKeyAsync(apiKey);
            if (user != null && user.SessionExpiration.HasValue && user.SessionExpiration.Value > DateTime.UtcNow)
            {
                _currentUser = user;
                return true;
            }
            return false;
        }

        public async Task CreateSessionAsync(User user)
        {
            user.SessionExpiration = DateTime.UtcNow.AddDays(31);
            await UpdateUserAsync(user);
        }
    }
}

