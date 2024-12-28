using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireVaultCore.Models
{
    public class ExternalAppRequest
    {
        public string AppId { get; set; }
        public string AppName { get; set; }
        public DateTime RequestTime { get; set; }

        public ExternalAppRequest(string appId, string appName)
        {
            AppId = appId;
            AppName = appName;
            RequestTime = DateTime.Now;
        }
    }
}
