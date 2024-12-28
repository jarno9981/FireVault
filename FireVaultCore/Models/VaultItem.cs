namespace FireVaultCore.Models
{
    public class VaultItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string EncryptedData { get; set; }
        public string Type { get; set; }
        public string Username { get; set; }

        public VaultItem(string id, string title, string encryptedData, string type, string username)
        {
            Id = id;
            Title = title;
            EncryptedData = encryptedData;
            Type = type;
            Username = username;
        }
    }
}

