using System.Diagnostics;

namespace PRN232.NMS.Services.Helpers.HelperClasses
{
    public class ProjectBuildHelper
    {
        public async Task<bool> BuildAsync(string dir, List<string> logs)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build",
                WorkingDirectory = dir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi)!;
            string output = await p.StandardOutput.ReadToEndAsync();
            string err = await p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();

            logs.Add("Build Succeeded");

            return p.ExitCode == 0;
        }
    }
}
