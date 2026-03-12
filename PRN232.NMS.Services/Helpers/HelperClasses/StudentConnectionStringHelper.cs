using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PRN232.NMS.API.Helpers.HelperClasses
{
    public class StudentConnectionStringHelper
    {
        public string BuildStudentConnectionString(string dbName) =>
        $"Data Source=DESKTOP-OEQ9HFB\\SQLEXPRESS;Initial Catalog={dbName};User ID=sa;Password=12345;TrustServerCertificate=true;";

        public async Task PatchConnectionStringAsync(string projectDir, string connStr)
        {
            // Chỉnh connectiong string trong appsettings của student
            var appSettingsFiles = Directory.GetFiles(projectDir, "appsettings*.json", SearchOption.AllDirectories);
            foreach (var path in appSettingsFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(path);
                    if (!json.Contains("ConnectionStrings", StringComparison.OrdinalIgnoreCase)) continue;

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement.Clone();
                    var writerOptions = new JsonWriterOptions { Indented = true };
                    using var ms = new MemoryStream();
                    using var writer = new Utf8JsonWriter(ms, writerOptions);

                    writer.WriteStartObject();
                    bool wroteConnStr = false;
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.NameEquals("ConnectionStrings"))
                        {
                            writer.WritePropertyName("ConnectionStrings");
                            writer.WriteStartObject();
                            // Preserve whatever key names the student used (DBConnection, DefaultConnection, etc.)
                            bool wroteSomething = false;
                            foreach (var cs in prop.Value.EnumerateObject())
                            {
                                writer.WriteString(cs.Name, connStr);
                                wroteSomething = true;
                            }
                            if (!wroteSomething)
                                writer.WriteString("DefaultConnection", connStr);
                            writer.WriteEndObject();
                            wroteConnStr = true;
                        }
                        else
                        {
                            prop.WriteTo(writer);
                        }
                    }
                    if (!wroteConnStr)
                    {
                        // No ConnectionStrings section existed — add one
                        writer.WritePropertyName("ConnectionStrings");
                        writer.WriteStartObject();
                        writer.WriteString("DefaultConnection", connStr);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();
                    writer.Flush();
                    await File.WriteAllBytesAsync(path, ms.ToArray());
                }
                catch {}
            }

            // Dùng hàm ở dưới
            await PatchHardcodedDbContextAsync(projectDir, connStr);
        }

        // Chỉnh connectiong nếu tụi nó hardcor trong DbContext
        private async Task PatchHardcodedDbContextAsync(string projectDir, string connStr)
        {
            var csFiles = Directory.GetFiles(projectDir, "*Context.cs", SearchOption.AllDirectories)
                                   .Concat(Directory.GetFiles(projectDir, "*DBContext.cs", SearchOption.AllDirectories))
                                   .Distinct();

            foreach (var file in csFiles)
            {
                try
                {
                    var src = await File.ReadAllTextAsync(file);

                    if (!src.Contains("OnConfiguring", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!src.Contains("UseSqlServer", StringComparison.OrdinalIgnoreCase)) continue;

                    // Escape characters for C# string literal
                    var escaped = connStr.Replace("\\", "\\\\").Replace("\"", "\\\"");

                    // Replace double quoted argument
                    var patched = System.Text.RegularExpressions.Regex.Replace(
                        src,
                        @"optionsBuilder\.UseSqlServer\(""[^""]*""\)",
                        $"optionsBuilder.UseSqlServer(\"{escaped}\")"
                    );

                    // Replace verbatim string argument @"..."
                    patched = System.Text.RegularExpressions.Regex.Replace(
                        patched,
                        @"optionsBuilder\.UseSqlServer\(@""[^""]*""\)",
                        $"optionsBuilder.UseSqlServer(\"{escaped}\")"
                    );

                    if (patched != src)
                        await File.WriteAllTextAsync(file, patched);
                }
                catch
                {
                    /* best effort */
                }
            }
        }
    }
}
