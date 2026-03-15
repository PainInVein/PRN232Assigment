using System.Diagnostics;

namespace PRN232.NMS.Services.Helpers.HelperClasses
{
    public class ApplySchemaHelper
    {
        private readonly string _schemaPath = @"C:\Users\Admin\Desktop\PRNGrading\SU25LeopardDB.sql";
        public async Task ApplySchemaAsync(string dbName)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sqlcmd",
                Arguments = $@"-S DESKTOP-H9I435N\SQLEXPRESS -U sa -P 1 -d {dbName} -i ""{_schemaPath}""",
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
