using System.Diagnostics;
using PRN232.NMS.Services.Helpers.HelperEntities;

namespace PRN232.NMS.Services.Helpers.HelperClasses
{
    public interface IClassHelperFacade
    {
        public void CopyDirectory(string source, string target);
        public Task CreateDatabaseAsync(string dbName, string masterConnStr);
        public Task TryDropDatabaseAsync(string dbName, string masterConnStr);
        public Task ApplySchemaAsync(string dbName, string schemaPath, string username, string password, string serverName);
        public Task PatchConnectionStringAsync(string projectDir, string connStr);
        public string BuildStudentConnectionString(string dbName, string username, string password, string serverName);
        public Task<bool> BuildAsync(string dir, List<string> logs);
        public Process StartApi(string dir, int port, List<string> logs);
        public Task<bool> WaitForApiReadyAsync(string baseUrl, CancellationToken ct, int timeoutSec = 30);
        public Task<string?> FetchTokenAsync(string baseUrl, string email, string password, List<string> logs, RouteMap? routes = null);
        public Task<RouteMap> DiscoverRoutesAsync(string baseUrl, List<string> logs);
        public List<ApiTestCase> GetTestSuite();
        public int GetFreePort(int start = 5100, int end = 5200);
        void ClearTokenCache();
    }
}
