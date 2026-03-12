using System.Diagnostics;

namespace PRN232.NMS.API.Helpers.HelperClasses
{
    public class StartApiHelper
    {
        public Process StartApi(string dir, int port, List<string> logs)
        {
            var apiProject = Directory
                .GetFiles(dir, "*.csproj", SearchOption.AllDirectories)
                .First(x => File.ReadAllText(x).Contains("Microsoft.NET.Sdk.Web"));

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --no-build --no-launch-profile --project \"{apiProject}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            psi.Environment["ASPNETCORE_URLS"] = $"http://localhost:{port}";
            psi.Environment["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "";

            var p = Process.Start(psi)!;

            _ = Task.Run(async () =>
            {
                string? line;
                while ((line = await p.StandardOutput.ReadLineAsync()) != null)
                    logs.Add($"[API OUT] {line}");

                while ((line = await p.StandardError.ReadLineAsync()) != null)
                    logs.Add($"[API ERR] {line}");
            });

            return p;
        }
    }
}
