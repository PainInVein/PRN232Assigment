using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Helpers.HelperClasses
{
    public class WindowsPathToContainerHelper
    {
        public string ResolveWindowsPathToContainer(string windowsPath, string prefixPath, string dockerPath)
        {
            //const string prefix = @"C:\Users\Admin\Desktop\submissions\";
            if (windowsPath.StartsWith(prefixPath, StringComparison.OrdinalIgnoreCase))
            {
                var relative = windowsPath.Substring(prefixPath.Length).Replace('\\', '/').TrimStart('/');
                return Path.Combine(dockerPath, relative);
            }
            return windowsPath;
        }
    }
}
