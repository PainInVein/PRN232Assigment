using PRN232.NMS.API.Helpers.HelperClasses;
using PRN232.NMS.API.Helpers.HelperEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Grader.Services.GradingService;

namespace PRN232.NMS.API.Services
{
    public class ExecuteTestService
    {
        private readonly IClassHelperFacade _helperFacade;
        public ExecuteTestService(IClassHelperFacade classHelperFacade)
        {
            _helperFacade = classHelperFacade;
        }


        public async Task<List<TestOutcome>> ExecuteApiTestsAsync(string baseUrl, List<string> gradingLogs, RouteMap routes)
        {
            var tests = _helperFacade.GetTestSuite();
            var outcomes = new List<TestOutcome>();
            using var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(6) };

            foreach (var t in tests)
            {
                try
                {
                    // Resolve path through route map (handles students who used different route names)
                    string resolvedPath = routes.Resolve(t.Method, t.Path, t.PathHint);
                    var msg = new HttpRequestMessage(t.Method, resolvedPath);
                    if (t.JsonBody != null)
                        msg.Content = new StringContent(t.JsonBody, Encoding.UTF8, "application/json");

                    // Attach bearer token when credentials are provided
                    if (!string.IsNullOrEmpty(t.BearerTokenEmail))
                    {
                        var token = await _helperFacade.FetchTokenAsync(baseUrl, t.BearerTokenEmail, t.BearerTokenPassword ?? "@1", gradingLogs, routes);
                        if (token != null)
                            msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                        else
                            gradingLogs.Add($"[WARN] No token for {t.BearerTokenEmail} — \"{t.Name}\" will run without auth (path: {resolvedPath})");
                    }



                    var resp = await client.SendAsync(msg);

                    string? body = null;
                    if (t.ExpectedContentContains != null || !resp.IsSuccessStatusCode)
                        body = await resp.Content.ReadAsStringAsync();

                    bool contentOk = t.ExpectedContentContains == null ||
                                     (body?.Contains(t.ExpectedContentContains, StringComparison.OrdinalIgnoreCase) ?? false);

                    // Accept alternate status codes (200 OR 201, 200 OR 204, etc.)
                    int actual = (int)resp.StatusCode;
                    bool statusOk = actual == t.ExpectedStatus ||
                                    (t.AlternateStatus.HasValue && actual == t.AlternateStatus.Value);

                    bool pass = statusOk && contentOk;
                    int bodyLen = body?.Length ?? 0;

                    outcomes.Add(new TestOutcome
                    {
                        Name = t.Name,
                        Passed = pass,
                        Points = pass ? t.Points : 0,
                        Message = pass ? null : $"Status={actual}, expected {t.ExpectedStatus}{(t.AlternateStatus.HasValue ? $" or {t.AlternateStatus}" : "")}. Body: {body?[..Math.Min(150, bodyLen)]}..."
                    });
                }
                catch (Exception ex)
                {
                    outcomes.Add(new TestOutcome
                    {
                        Name = t.Name,
                        Passed = false,
                        Points = 0,
                        Message = ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ? "Timeout" : $"Exception: {ex.Message}"
                    });
                }
            }
            return outcomes;
        }
    }
}
