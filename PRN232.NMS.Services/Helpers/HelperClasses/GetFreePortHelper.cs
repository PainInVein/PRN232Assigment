using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.Services.Helpers.HelperClasses
{
    public class GetFreePortHelper
    {
        // Lay port free tu 5100 den 5200, neu het thi throw exception
        public int GetFreePort(int start = 5100, int end = 5200)
        {
            for (int port = start; port <= end; port++)
            {
                try
                {
                    using var listener = new TcpListener(System.Net.IPAddress.Loopback, port);
                    listener.Start();
                    listener.Stop();
                    return port;
                }
                catch { }
            }
            throw new Exception($"No free port found in range {start}-{end}");
        }
    }
}
