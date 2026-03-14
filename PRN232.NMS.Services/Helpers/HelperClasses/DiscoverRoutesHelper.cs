using System.Text.Json;
using PRN232.NMS.Services.Helpers.HelperEntities;

namespace PRN232.NMS.Services.Helpers.HelperClasses
{
    public class DiscoverRoutesHelper
    {
        // Tìm các endpoint từ swagger.json, nếu không có thì để null và dùng hardcoded paths
        public async Task<RouteMap> DiscoverRoutesAsync(string baseUrl, List<string> logs)
        {
            var map = new RouteMap();
            using var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(5) };

            
            foreach (var swaggerPath in new[] { "/swagger/v1/swagger.json", "/swagger/v1.0/swagger.json", "/openapi/v1.json" })
            {
                try
                {
                    var resp = await client.GetAsync(swaggerPath);
                    if (!resp.IsSuccessStatusCode) continue;

                    var json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    if (!doc.RootElement.TryGetProperty("paths", out var paths)) continue;

                    foreach (var pathProp in paths.EnumerateObject())
                    {
                        var rawPath = pathProp.Name; // e.g. "/api/LeopardProfile" or "/api/Auth"
                        map.AllPaths.Add(rawPath);
                        var lower = rawPath.ToLower();

                        foreach (var methodProp in pathProp.Value.EnumerateObject())
                        {
                            var method = methodProp.Name.ToUpper();

                            // Auth endpoint: POST with no {id} in path, path contains auth/login/account
                            if (method == "POST" && (lower.Contains("auth") || lower.Contains("login")) && !lower.Contains("{"))
                            {
                                map.AuthPath = rawPath;
                                logs.Add($"[ROUTES] Auth endpoint discovered: POST {rawPath}");
                            }

                            // Profile collection: path contains leopard or profile, no {id}
                            if (!lower.Contains("{") && (lower.Contains("leopard") || lower.Contains("profile")))
                            {
                                if (map.ProfileCollectionPath == null || rawPath.Length < map.ProfileCollectionPath.Length)
                                {
                                    map.ProfileCollectionPath = rawPath;
                                    logs.Add($"[ROUTES] Profile collection endpoint: {method} {rawPath}");
                                }
                            }

                            // Profile item: same but has {id}
                            if (lower.Contains("{") && (lower.Contains("leopard") || lower.Contains("profile")))
                            {
                                map.ProfileItemPath = rawPath;
                                logs.Add($"[ROUTES] Profile item endpoint: {method} {rawPath}");
                            }
                        }
                    }

                    logs.Add($"[ROUTES] Discovery complete. Auth={map.AuthPath}, Collection={map.ProfileCollectionPath}, Item={map.ProfileItemPath}");
                    logs.Add($"[ROUTES] All paths: {string.Join(", ", map.AllPaths)}");
                    return map;
                }
                catch (Exception ex)
                {
                    logs.Add($"[ROUTES] {swaggerPath} failed: {ex.Message}");
                }
            }

            logs.Add("[ROUTES] Swagger not available — falling back to hardcoded paths");
            return map;
        }
    }
}
