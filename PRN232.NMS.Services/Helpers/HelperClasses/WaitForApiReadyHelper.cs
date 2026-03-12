using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.API.Helpers.HelperClasses
{
    public class WaitForApiReadyHelper
    {
        // Lấy danh sách endpoint
        public readonly string[] _readinessProbePaths =
            { "/api/LeopardProfile", "/api/Leopard", "/swagger/v1/swagger.json", "/" };

        // Kiểm tra API đã sẵn sàng hay chưa bằng cách gửi request đến các endpoint
        public async Task<bool> WaitForApiReadyAsync(string baseUrl, CancellationToken ct, int timeoutSec = 30)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.TotalSeconds < timeoutSec)
            {
                if (ct.IsCancellationRequested) return false;

                foreach (var probe in _readinessProbePaths)
                {
                    try
                    {
                        var resp = await http.GetAsync(baseUrl + probe, ct);
                        // Any non-5xx response means the server is up and listening.
                        if ((int)resp.StatusCode < 500) return true;
                    }
                    catch { }
                }

                await Task.Delay(800, ct);
            }
            return false;
        }
    }
}
