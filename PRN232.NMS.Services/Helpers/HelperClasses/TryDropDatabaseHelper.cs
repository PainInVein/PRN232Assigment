using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.API.Helpers.HelperClasses
{
    public class TryDropDatabaseHelper
    {
        private readonly string _masterConnStr = "Server=DESKTOP-OEQ9HFB\\SQLEXPRESS;User Id=sa;Password=12345;TrustServerCertificate=true;";

        public async Task TryDropDatabaseAsync(string dbName)
        {
            try
            {
                using var conn = new SqlConnection(_masterConnStr);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    $"""
                ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{dbName}];
                """, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch {}
        }
    }
}
