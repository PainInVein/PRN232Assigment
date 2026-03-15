using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Helpers.HelperClasses
{
    public class TryDropDatabaseHelper
    {
        public async Task TryDropDatabaseAsync(string dbName, string masterConnStr)
        {
            try
            {
                using var conn = new SqlConnection(masterConnStr);
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
