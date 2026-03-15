using System.Diagnostics;

namespace PRN232.NMS.Services.Helpers.HelperClasses
{
    public class ApplySchemaHelper
    {
        private readonly string _schemaPath = @"C:\Users\Admin\Desktop\PRNGrading\SU25LeopardDB.sql";
        public async Task ApplySchemaAsync(string dbName, string schemaPath, string username, string password, string serverName)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sqlcmd",
                Arguments = $@"-S {serverName} -U {username} -P {password} -d {dbName} -i ""{schemaPath}""",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi)!;
            string err = await p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();

            if (p.ExitCode != 0)
                throw new Exception($"sqlcmd failed (exit {p.ExitCode}): {err}");
        }
    }
}
