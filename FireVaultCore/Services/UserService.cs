using FireVaultCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FireVaultCore.Services
{
    internal class UserService
    {
        private List<User> _users = new List<User>(); // In a real scenario, this would be persisted to a database

        public async Task<User> GetUserByApiKeyAsync(string apiKey)
        {
            return await Task.FromResult(_users.FirstOrDefault(u => u.ApiKey == apiKey));
        }

        public async Task<User> AuthenticateUserAsync(string username, string password)
        {
            var user = _users.FirstOrDefault(u => u.Username == username);
            if (user != null && VerifyPassword(password, user.PasswordHash, user.Salt))
            {
                return await Task.FromResult(user);
            }
            return null;
        }

        public async Task UpdateUserAsync(User user)
        {
            var index = _users.FindIndex(u => u.Username == user.Username);
            if (index != -1)
            {
                _users[index] = user;
            }
            await Task.CompletedTask;
        }

        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var computedHash = EncryptionService.HashPassword(password, storedSalt);
            return computedHash == storedHash;
        }
    }
}

