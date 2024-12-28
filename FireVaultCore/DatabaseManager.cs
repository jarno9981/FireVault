using Microsoft.Data.Sqlite;
using SQLitePCL;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using FireVaultCore.Models;

namespace FireVaultCore
{
    public class DatabaseManager
    {
        private readonly string _databasePath;
        private readonly string _username;
        private static bool _sqliteInitialized = false;
        private const string FolderNameFile = "folder_name.txt";

        public DatabaseManager(string username)
        {
            _username = username;
            string randomFolderName = GetOrCreateRandomFolderName();
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), randomFolderName);
            _databasePath = Path.Combine(appDataFolder, $"{username}.fdb");

            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            InitializeSQLite();
        }

        private string GetOrCreateRandomFolderName()
        {
            string folderNameFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderNameFile);

            if (File.Exists(folderNameFilePath))
            {
                return File.ReadAllText(folderNameFilePath);
            }
            else
            {
                string randomFolderName = GenerateRandomFolderName();
                File.WriteAllText(folderNameFilePath, randomFolderName);
                return randomFolderName;
            }
        }

        private string GenerateRandomFolderName()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void InitializeSQLite()
        {
            if (!_sqliteInitialized)
            {
                SQLitePCL.Batteries_V2.Init();
                _sqliteInitialized = true;
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                var connectionStringBuilder = new SqliteConnectionStringBuilder
                {
                    DataSource = _databasePath,
                    Mode = SqliteOpenMode.ReadWriteCreate
                };

                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS vault_items (
                        id TEXT PRIMARY KEY,
                        title TEXT NOT NULL,
                        encrypted_data TEXT NOT NULL,
                        type TEXT NOT NULL,
                        created_at TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS metadata (
                        key TEXT PRIMARY KEY,
                        value TEXT NOT NULL
                    );";
                await command.ExecuteNonQueryAsync();

                command.CommandText = @"
                    INSERT OR IGNORE INTO metadata (key, value) 
                    VALUES ('schema_version', '1.0'),
                           ('created_at', $timestamp),
                           ('username', $username)";
                command.Parameters.AddWithValue("$timestamp", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("$username", _username);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                throw new Exception($"Failed to initialize database for user {_username}. Error: {ex.Message}", ex);
            }
        }

        public async Task SaveVaultItemAsync(string title, string data, string type, string password)
        {
            try
            {
                var connectionStringBuilder = new SqliteConnectionStringBuilder
                {
                    DataSource = _databasePath,
                    Mode = SqliteOpenMode.ReadWrite
                };

                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                await connection.OpenAsync();

                var encryptedData = EncryptData(data, password);
                var id = Guid.NewGuid().ToString();
                var timestamp = DateTime.UtcNow.ToString("o");

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO vault_items (id, title, encrypted_data, type, created_at)
                    VALUES ($id, $title, $data, $type, $timestamp)";

                command.Parameters.AddWithValue("$id", id);
                command.Parameters.AddWithValue("$title", title);
                command.Parameters.AddWithValue("$data", encryptedData);
                command.Parameters.AddWithValue("$type", type);
                command.Parameters.AddWithValue("$timestamp", timestamp);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save vault item error: {ex.Message}");
                throw new Exception($"Failed to save vault item. Error: {ex.Message}", ex);
            }
        }

        public async Task<List<VaultItem>> GetVaultItemsAsync()
        {
            try
            {
                var connectionStringBuilder = new SqliteConnectionStringBuilder
                {
                    DataSource = _databasePath,
                    Mode = SqliteOpenMode.ReadOnly
                };

                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM vault_items ORDER BY created_at DESC";

                var items = new List<VaultItem>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    items.Add(new VaultItem(
                        reader.GetString(0), // id
                        reader.GetString(1), // title
                        reader.GetString(2), // encrypted_data
                        reader.GetString(3), // type
                        _username
                    ));
                }

                return items;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get vault items error: {ex.Message}");
                return new List<VaultItem>(); // Return empty list on error
            }
        }

        public async Task<bool> DeleteVaultItemAsync(string id)
        {
            try
            {
                var connectionStringBuilder = new SqliteConnectionStringBuilder
                {
                    DataSource = _databasePath,
                    Mode = SqliteOpenMode.ReadWrite
                };

                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM vault_items WHERE id = $id";
                command.Parameters.AddWithValue("$id", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete vault item error: {ex.Message}");
                return false;
            }
        }

        private string EncryptData(string data, string password)
        {
            try
            {
                byte[] salt = new byte[16];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(salt);
                }

                byte[] key = DeriveKey(password, salt);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.GenerateIV();

                byte[] encryptedData;
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(data);
                    }
                    encryptedData = msEncrypt.ToArray();
                }

                byte[] combinedData = new byte[salt.Length + aes.IV.Length + encryptedData.Length];
                Buffer.BlockCopy(salt, 0, combinedData, 0, salt.Length);
                Buffer.BlockCopy(aes.IV, 0, combinedData, salt.Length, aes.IV.Length);
                Buffer.BlockCopy(encryptedData, 0, combinedData, salt.Length + aes.IV.Length, encryptedData.Length);

                return Convert.ToBase64String(combinedData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encryption error: {ex.Message}");
                throw new Exception("Failed to encrypt data", ex);
            }
        }

        private string DecryptData(string encryptedData, string password)
        {
            try
            {
                byte[] combinedData = Convert.FromBase64String(encryptedData);

                byte[] salt = new byte[16];
                byte[] iv = new byte[16];
                byte[] cipherText = new byte[combinedData.Length - 32];

                Buffer.BlockCopy(combinedData, 0, salt, 0, 16);
                Buffer.BlockCopy(combinedData, 16, iv, 0, 16);
                Buffer.BlockCopy(combinedData, 32, cipherText, 0, cipherText.Length);

                byte[] key = DeriveKey(password, salt);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var msDecrypt = new MemoryStream(cipherText);
                using var csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);

                return srDecrypt.ReadToEnd();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Decryption error: {ex.Message}");
                throw new Exception("Failed to decrypt data. Please check your password and try again.");
            }
        }

        private byte[] DeriveKey(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32); // 256 bits
        }

        public async Task<string> DecryptVaultItemAsync(VaultItem item, string password)
        {
            try
            {
                var connectionStringBuilder = new SqliteConnectionStringBuilder
                {
                    DataSource = _databasePath,
                    Mode = SqliteOpenMode.ReadOnly
                };

                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT encrypted_data FROM vault_items WHERE id = $id";
                command.Parameters.AddWithValue("$id", item.Id);

                var encryptedData = (string)await command.ExecuteScalarAsync();

                if (encryptedData == null)
                {
                    throw new Exception("Vault item not found.");
                }

                return DecryptData(encryptedData, password);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Decrypt vault item error: {ex.Message}");
                throw new Exception("Failed to decrypt vault item. Please check your password and try again.");
            }
        }
    }
}

