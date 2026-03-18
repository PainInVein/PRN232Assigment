using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Helpers.HelperClasses
{
    public class WindowsPathToContainerHelper
    {
        public string ResolveWindowsPathToContainer(string windowsPath)
        {
            const string prefix = @"C:\Users\Admin\Desktop\submissions\";
            if (windowsPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var relative = windowsPath.Substring(prefix.Length).Replace('\\', '/').TrimStart('/');
                return Path.Combine("/students", relative);
            }
            return windowsPath;
        }
    }
}
