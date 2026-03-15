using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Helpers.HelperClasses
{
    public class CreateDatabaseHelper
    {
        public async Task CreateDatabaseAsync(string dbName, string masterConnStr)
        {
            using var conn = new SqlConnection(masterConnStr);
            await conn.OpenAsync();
            using var cmd = new SqlCommand($"CREATE DATABASE [{dbName}]", conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
