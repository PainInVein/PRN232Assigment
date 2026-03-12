using PRN232.NMS.API.Helpers.HelperEntities;
using System.Text;
using System.Text.Json;
using static Grader.Services.GradingService;

namespace PRN232.NMS.API.Helpers.HelperClasses
{
    public class TokenHelper
    {
        // Lưu cache token theo email để tránh gọi nhiều lần với cùng một tài khoản
        public readonly Dictionary<string, string?> _tokenCache = new();

        // Lấy danh sách các endpoint phổ biến để thử đăng nhập
        public readonly string[] _authPaths = { "/api/auth", "/api/login", "/api/account/login", "/api/accounts/login", "/api/authenticate" };

        public string? FindTokenInElement(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;
            foreach (var prop in element.EnumerateObject())
            {
                // Chấp nhận mấy field phổ biến chứa token
                if (prop.Value.ValueKind == JsonValueKind.String &&
                    (prop.Name.Equals("token", StringComparison.OrdinalIgnoreCase) ||
                     prop.Name.Equals("accessToken", StringComparison.OrdinalIgnoreCase) ||
                     prop.Name.Equals("access_token", StringComparison.OrdinalIgnoreCase) ||
                     prop.Name.Equals("jwt", StringComparison.OrdinalIgnoreCase)))
                    return prop.Value.GetString();
                // Nếu là object, đệ quy tìm sâu hơn
                if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    var nested = FindTokenInElement(prop.Value);
                    if (nested != null) return nested;
                }
            }
            return null;
        }

        // Cố gắng đăng nhập và lấy token, thử qua nhiều endpoint khác nhau
        public async Task<string?> FetchTokenAsync(string baseUrl, string email, string password, List<string> logs, RouteMap? routes = null)
        {
            if (_tokenCache.TryGetValue(email, out var cached))
            {
                logs.Add($"[AUTH] Cache hit for {email}: token {(cached != null ? "present" : "NULL — login previously failed")}");
                return cached;
            }

            using var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(6) };
            var bodyJson = $"{{\"email\":\"{email}\",\"password\":\"{password}\"}}";

            // Lấy danh sách các auth path để thử, ưu tiên cái được route map cung cấp nếu có
            var authPathsToTry = new List<string>();
            if (routes?.AuthPath != null) authPathsToTry.Add(routes.AuthPath);
            foreach (var p in _authPaths) if (!authPathsToTry.Contains(p)) authPathsToTry.Add(p);

            foreach (var path in authPathsToTry)
            {
                try
                {
                    var msg = new HttpRequestMessage(HttpMethod.Post, path);
                    msg.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                    var resp = await client.SendAsync(msg);
                    var respBody = await resp.Content.ReadAsStringAsync();
                    logs.Add($"[AUTH] POST {path} for {email} → {(int)resp.StatusCode}  body: {respBody[..Math.Min(200, respBody.Length)]}");

                    if (!resp.IsSuccessStatusCode) continue;

                    var token = FindTokenInElement(JsonDocument.Parse(respBody).RootElement);
                    if (token != null)
                    {
                        logs.Add($"[AUTH] Token obtained for {email} via {path}");
                        _tokenCache[email] = token;
                        return token;
                    }

                    logs.Add($"[AUTH] Login at {path} returned 200 but no token field found. Raw: {respBody[..Math.Min(300, respBody.Length)]}");
                    _tokenCache[email] = null;
                    return null; // Nếu endpoint này có vẻ đúng nhưng không trả token, không thử tiếp các endpoint khác nữa
                }
                catch (Exception ex)
                {
                    logs.Add($"[AUTH] {path} exception: {ex.Message}");
                }
            }

            logs.Add($"[AUTH] All auth paths failed for {email}");
            _tokenCache[email] = null;
            return null;
        }
    }
}
