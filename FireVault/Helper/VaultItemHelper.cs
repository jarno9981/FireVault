using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireVault.Helper
{
    public static class VaultItemHelper
    {
        public static string GetGlyphForType(string type)
        {
            return type.ToLower() switch
            {
                "password" => "\uE8D7", // Lock icon
                "credit card" => "\uE8C7", // Credit card icon
                "private data" => "\uE8C3", // Document icon
                _ => "\uE7C3", // Generic file icon
            };
        }
    }
}

