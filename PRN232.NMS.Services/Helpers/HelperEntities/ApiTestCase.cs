using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.API.Helpers.HelperEntities
{
    public class ApiTestCase
    {
        public string Name { get; set; } = null!;
        public HttpMethod Method { get; set; } = HttpMethod.Get;
        public string Path { get; set; } = null!;
        public string? JsonBody { get; set; }
        public int ExpectedStatus { get; set; } = 200;
        public string? ExpectedContentContains { get; set; }
        public string? BearerTokenEmail { get; set; }
        public string? BearerTokenPassword { get; set; }
        public int? AlternateStatus { get; set; }
        public string? PathHint { get; set; }
        public int Points { get; set; } = 10;           // configurable weight
    }
}
