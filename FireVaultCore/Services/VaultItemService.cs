using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FireVaultCore;
using FireVaultCore.Models;

namespace FireVaultCore.Services
{
    internal class VaultItemService
    {
        private List<VaultItem> _vaultItems = new List<VaultItem>(); // In a real scenario, this would be persisted to a database

        public async Task<List<VaultItem>> GetVaultItemsForUserAsync(string username)
        {
            return await Task.FromResult(_vaultItems.Where(item => item.Username == username).ToList());
        }

        public async Task<bool> AddVaultItemAsync(VaultItem item)
        {
            _vaultItems.Add(item);
            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteVaultItemAsync(string itemId, string username)
        {
            var item = _vaultItems.FirstOrDefault(i => i.Id == itemId && i.Username == username);
            if (item != null)
            {
                _vaultItems.Remove(item);
                return await Task.FromResult(true);
            }
            return await Task.FromResult(false);
        }

        public async Task<VaultItem> GetVaultItemAsync(string itemId, string username)
        {
            return await Task.FromResult(_vaultItems.FirstOrDefault(i => i.Id == itemId && i.Username == username));
        }
    }
}

