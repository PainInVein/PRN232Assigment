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
        private readonly string _masterConnStr = "Server=DESKTOP-H9I435N\\SQLEXPRESS;User Id=sa;Password=1;TrustServerCertificate=true;";
        public async Task CreateDatabaseAsync(string dbName)
        {
            using var conn = new SqlConnection(_masterConnStr);
            await conn.OpenAsync();
            using var cmd = new SqlCommand($"CREATE DATABASE [{dbName}]", conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
