using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services
{
    public class DatabaseSettings
    {
        public string ServerName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SchemaPath { get; set; } = string.Empty;
        public string MasterConnectionString { get; set; } = string.Empty;
        public string BaseTempFolder { get; set; } = string.Empty;
    }
}
