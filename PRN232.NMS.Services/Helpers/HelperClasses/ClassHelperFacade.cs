using System.Diagnostics;
using PRN232.NMS.Services.Helpers.HelperEntities;

namespace PRN232.NMS.Services.Helpers.HelperClasses
{
    public class ClassHelperFacade : IClassHelperFacade
    {
        private readonly CopyDirectoryHelper _copyHelper;
        private readonly CreateDatabaseHelper _createDbHelper;
        private readonly TryDropDatabaseHelper _tryDropDbHelper;
        private readonly ApplySchemaHelper _applySchemaHelper;
        private readonly StudentConnectionStringHelper _connStrHelper;
        private readonly ProjectBuildHelper _buildHelper;
        private readonly StartApiHelper _startApiHelper;
        private readonly WaitForApiReadyHelper _waitForApiReadyHelper;
        private readonly TokenHelper _tokenHelper;
        private readonly DiscoverRoutesHelper _discoverRoutesHelper;
        private readonly GetTestSuiteHelper _getTestSuiteHelper;
        private readonly GetFreePortHelper _getFreePortHelper;

        public ClassHelperFacade(
            CopyDirectoryHelper copyDirectoryHelper,
            CreateDatabaseHelper createDatabaseHelper,
            TryDropDatabaseHelper tryDropDatabaseHelper,
            ApplySchemaHelper applySchemaHelper,
            StudentConnectionStringHelper connStrHelper,
            ProjectBuildHelper buildHelper,
            StartApiHelper startApiHelper,
            WaitForApiReadyHelper waitForApiReadyHelper,
            TokenHelper tokenHelper,
            DiscoverRoutesHelper discoverRoutesHelper,
            GetTestSuiteHelper getTestSuiteHelper,
            GetFreePortHelper getFreePortHelper
            )
        {
            _copyHelper = copyDirectoryHelper;
            _createDbHelper = createDatabaseHelper;
            _tryDropDbHelper = tryDropDatabaseHelper;
            _applySchemaHelper = applySchemaHelper;
            _connStrHelper = connStrHelper;
            _buildHelper = buildHelper;
            _startApiHelper = startApiHelper;
            _waitForApiReadyHelper = waitForApiReadyHelper;
            _tokenHelper = tokenHelper;
            _discoverRoutesHelper = discoverRoutesHelper;
            _getTestSuiteHelper = getTestSuiteHelper;
            _getFreePortHelper = getFreePortHelper;
        }

        public void CopyDirectory(string source, string target)
        {
            _copyHelper.CopyDirectory(source, target);
        }

        public async Task CreateDatabaseAsync(string dbName, string masterConnStr)
        {
            await _createDbHelper.CreateDatabaseAsync(dbName, masterConnStr);
        }

        public async Task TryDropDatabaseAsync(string dbName, string masterConnStr)
        {
            await _tryDropDbHelper.TryDropDatabaseAsync(dbName, masterConnStr);
        }

        public async Task ApplySchemaAsync(string dbName, string schemaPath, string studentConnStr)
        {
            await _applySchemaHelper.ApplySchemaAsync(dbName, schemaPath, studentConnStr);
        }
        public async Task PatchConnectionStringAsync(string projectDir, string connStr)
        {
            await _connStrHelper.PatchConnectionStringAsync(projectDir, connStr);
        }
        public string BuildStudentConnectionString(string dbName, string username, string password, string serverName)
        {
            return _connStrHelper.BuildStudentConnectionString(dbName, username, password, serverName);
        }
        public async Task<bool> BuildAsync(string dir, List<string> logs)
        {
            return await _buildHelper.BuildAsync(dir, logs);
        }
        public Process StartApi(string dir, int port, List<string> logs)
        {
            return _startApiHelper.StartApi(dir, port, logs);
        }
        public async Task<bool> WaitForApiReadyAsync(string baseUrl, CancellationToken ct, int timeoutSec = 30)
        {
            return await _waitForApiReadyHelper.WaitForApiReadyAsync(baseUrl, ct, timeoutSec);
        }
        public async Task<string?> FetchTokenAsync(string baseUrl, string email, string password, List<string> logs, RouteMap? routes = null)
        {
            return await _tokenHelper.FetchTokenAsync(baseUrl, email, password, logs, routes);
        }
        public async Task<RouteMap> DiscoverRoutesAsync(string baseUrl, List<string> logs)
        {
            return await _discoverRoutesHelper.DiscoverRoutesAsync(baseUrl, logs);
        }
        public List<ApiTestCase> GetTestSuite()
        {
            return _getTestSuiteHelper.GetTestSuite();
        }
        public int GetFreePort(int start = 5100, int end = 5200)
        {
            return _getFreePortHelper.GetFreePort(start, end);
        }
        public void ClearTokenCache()
        {
            _tokenHelper._tokenCache.Clear();
        }
    }
}
