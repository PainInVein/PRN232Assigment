using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.API.Helpers.HelperClasses
{
    public class CopyDirectoryHelper
    {
        public void CopyDirectory(string source, string target)
        {
            var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".vs", "bin", "obj", ".git", "node_modules"
    };

            foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(dir);

                if (excludedDirs.Contains(name))
                    continue;

                var relative = Path.GetRelativePath(source, dir);
                Directory.CreateDirectory(Path.Combine(target, relative));
            }

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(source, file);

                if (relative.Split(Path.DirectorySeparatorChar).Any(x => excludedDirs.Contains(x)))
                    continue;

                var dest = Path.Combine(target, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest, true);
            }
        }
    }
}
