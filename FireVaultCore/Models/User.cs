using System;
using System.Collections.Generic;

namespace FireVaultCore.Models
{
    public class User
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public string ApiKey { get; set; }
        public string AvatarUrl { get; set; }
        public List<string> TrustedApps { get; set; }
        public DateTime? SessionExpiration { get; set; }

        public User(string username, string passwordHash, string salt, string apiKey)
        {
            Username = username;
            PasswordHash = passwordHash;
            Salt = salt;
            ApiKey = apiKey;
            AvatarUrl = "/Assets/DefaultAvatar.png";
            TrustedApps = new List<string>();
        }

        public void TrustApp(string appId)
        {
            if (!TrustedApps.Contains(appId))
            {
                TrustedApps.Add(appId);
            }
        }

        public void RevokeTrustFromApp(string appId)
        {
            TrustedApps.Remove(appId);
        }
    }
}

