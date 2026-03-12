using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Helpers.HelperEntities
{
    public class RouteMap
    {
        // key: (METHOD, path_hint_keyword)  value: actual path on this student's API
        private readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);
        public string? AuthPath { get; set; }
        public string? ProfileCollectionPath { get; set; }  // the /api/XxxProfile  path
        public string? ProfileItemPath { get; set; }        // the /api/XxxProfile/{id} path
        public List<string> AllPaths { get; } = new();

        public void Add(string method, string hint, string actualPath) =>
            _map[$"{method.ToUpper()}:{hint.ToLower()}"] = actualPath;

        /// <summary>
        /// Given an expected path like "/api/LeopardProfile" or "/api/LeopardProfile/1",
        /// return the discovered equivalent on this student's API.
        /// Falls back to the original path if nothing found.
        /// </summary>
        public string Resolve(HttpMethod method, string expectedPath, string? hint = null)
        {
            // ── OData hint: find /search sub-path or fall back to collection ──
            if (hint == "odata")
            {
                var odataQuery = expectedPath.Contains('?') ? expectedPath.Substring(expectedPath.IndexOf('?')) : "";
                var searchPath = AllPaths.FirstOrDefault(p =>
                    p.EndsWith("/search", StringComparison.OrdinalIgnoreCase) &&
                    (p.Contains("leopard", StringComparison.OrdinalIgnoreCase) || p.Contains("profile", StringComparison.OrdinalIgnoreCase)));
                if (searchPath != null) return searchPath + odataQuery;
                if (ProfileCollectionPath != null) return ProfileCollectionPath + odataQuery;
            }

            // ── DELETE hint: handle students who use query param ?id=N instead of route /{id} ──
            if (hint != null && hint.StartsWith("delete_") && int.TryParse(hint.Substring(7), out int deleteId))
            {
                if (ProfileItemPath != null)
                    return ProfileItemPath.Replace("{id}", deleteId.ToString(), StringComparison.OrdinalIgnoreCase)
                                         .Replace("{Id}", deleteId.ToString());
                if (ProfileCollectionPath != null)
                    return $"{ProfileCollectionPath}?id={deleteId}";
            }

            // ── Explicit map hint ──────────────────────────────────────────────
            if (hint != null && _map.TryGetValue($"{method.Method.ToUpper()}:{hint.ToLower()}", out var byHint))
                return byHint;

            // ── Direct match ───────────────────────────────────────────────────
            if (AllPaths.Contains(expectedPath, StringComparer.OrdinalIgnoreCase))
                return expectedPath;

            // ── Pattern matching ───────────────────────────────────────────────
            var lower = expectedPath.ToLower();

            if (lower.Contains("leopardprofile") || lower.Contains("leopard"))
            {
                var parts = expectedPath.Trim('/').Split('/');
                bool hasId = parts.Length > 0 && (int.TryParse(parts[^1], out _) || parts[^1].StartsWith("{"));
                bool hasOdata = expectedPath.Contains('?');

                if (hasOdata && ProfileCollectionPath != null)
                    return ProfileCollectionPath + expectedPath.Substring(expectedPath.IndexOf('?'));
                if (hasId && ProfileItemPath != null)
                    return ProfileItemPath.Replace("{id}", parts[^1], StringComparison.OrdinalIgnoreCase)
                                         .Replace("{Id}", parts[^1]);
                if (!hasId && ProfileCollectionPath != null)
                    return ProfileCollectionPath;
            }

            if ((lower.Contains("auth") || lower.Contains("login")) && method == HttpMethod.Post && AuthPath != null)
                return AuthPath;

            return expectedPath; // fallback — use original
        }
    }
}
