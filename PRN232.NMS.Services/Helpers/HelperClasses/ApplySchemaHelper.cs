using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PRN232.NMS.Services.Helpers.HelperClasses
{
    public class ApplySchemaHelper
    {
        public async Task ApplySchemaAsync(string dbName, string schemaPath, string studentConnStr)
        {
            var script = await File.ReadAllTextAsync(schemaPath);
            var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            await using var conn = new SqlConnection(studentConnStr);

            await conn.OpenAsync();

            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                await using var cmd = new SqlCommand(trimmed, conn) { CommandTimeout = 120 };
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
