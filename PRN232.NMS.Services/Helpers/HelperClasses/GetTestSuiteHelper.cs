using PRN232.NMS.API.Helpers.HelperEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.NMS.API.Helpers.HelperClasses
{
    // Lớp này trả về một danh sách test case
    public class GetTestSuiteHelper
    {
        public List<ApiTestCase> GetTestSuite() => new()
{
    // ── 1. Authentication  (~1.54 pts total) ──────────────────────────

    new()
    {
        Name = "Login success (administrator) → 200 + token",
        Method = HttpMethod.Post,
        Path = "/api/auth",
        JsonBody = """{"email":"administrator@leopard.com","password":"@1"}""",
        ExpectedStatus = 200,
        ExpectedContentContains = "token",
        Points = 4   // ~0.4 pts
    },
    new()
    {
        Name = "Login success → response includes role",
        Method = HttpMethod.Post,
        Path = "/api/auth",
        JsonBody = """{"email":"administrator@leopard.com","password":"@1"}""",
        ExpectedStatus = 200,
        ExpectedContentContains = "role",
        Points = 4   // ~0.4 pts
    },
    new()
    {
        Name = "Login wrong password → 401 or 404",
        Method = HttpMethod.Post,
        Path = "/api/auth",
        JsonBody = """{"email":"administrator@leopard.com","password":"wrongpass"}""",
        ExpectedStatus = 401,
        AlternateStatus = 404,
        Points = 3   // ~0.3 pts
    },
    new()
    {
        Name = "Login non-existent email → 401 or 404",
        Method = HttpMethod.Post,
        Path = "/api/auth",
        JsonBody = """{"email":"notexist@no.com","password":"@1"}""",
        ExpectedStatus = 401,
        AlternateStatus = 404,
        Points = 3   // ~0.3 pts
    },

    // ── 2. LeopardProfile API Endpoints  (~6.92 pts total) ────────────

    // GET list
    new()
    {
        Name = "GET /api/LeopardProfile – no token → 401",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile",
        ExpectedStatus = 401,
        Points = 5   // ~0.5 pts
    },
    new()
    {
        Name = "GET /api/LeopardProfile – administrator → 200 with data",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 200,
        ExpectedContentContains = "LeopardName",
        Points = 5   // ~0.5 pts
    },
    new()
    {
        Name = "GET /api/LeopardProfile – moderator → 200",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "moderator@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 200,
        Points = 3
    },
    new()
    {
        Name = "GET /api/LeopardProfile – developer → 200",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "developer@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 200,
        Points = 3
    },
    new()
    {
        Name = "GET /api/LeopardProfile – member → 200",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "member@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 200,
        Points = 3
    },

    // GET by ID
    new()
    {
        Name = "GET /api/LeopardProfile/1 – administrator → 200",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile/1",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 200,
        Points = 3
    },
    new()
    {
        Name = "GET /api/LeopardProfile/999999 – not found → 404",
        Method = HttpMethod.Get,
        Path = "/api/LeopardProfile/999999",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 404,
        Points = 3
    },

    // POST create
    new()
    {
        Name = "POST /api/LeopardProfile – administrator → 200/201",
        Method = HttpMethod.Post,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"GraderLeoAdmin","LeopardTypeId":1,"Weight":50,"Characteristics":"Test cat","CareNeeds":"Monitored","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 201,
        AlternateStatus = 200,
        Points = 5
    },
    new()
    {
        Name = "POST /api/LeopardProfile – moderator → 200/201",
        Method = HttpMethod.Post,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "moderator@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"GraderLeoMod","LeopardTypeId":1,"Weight":60,"Characteristics":"Test","CareNeeds":"Protected","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 201,
        AlternateStatus = 200,
        Points = 5
    },
    new()
    {
        Name = "POST /api/LeopardProfile – member → 403",
        Method = HttpMethod.Post,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "member@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"Denied","LeopardTypeId":1,"Weight":50,"Characteristics":"x","CareNeeds":"x","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 403,
        Points = 5
    },
    new()
    {
        Name = "POST /api/LeopardProfile – developer → 403",
        Method = HttpMethod.Post,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "developer@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"DevDenied","LeopardTypeId":1,"Weight":50,"Characteristics":"x","CareNeeds":"x","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 403,
        Points = 4
    },

    // PUT update
    new()
    {
        Name = "PUT /api/LeopardProfile/2 – administrator → 200/201/204",
        Method = HttpMethod.Put,
        Path = "/api/LeopardProfile/2",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"UpdatedLeo","LeopardTypeId":1,"Weight":55,"Characteristics":"Updated","CareNeeds":"Updated care","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 200,
        AlternateStatus = 201,
        Points = 5
    },
    new()
    {
        Name = "PUT /api/LeopardProfile/2 – administrator → 204 (alt)",
        Method = HttpMethod.Put,
        Path = "/api/LeopardProfile/2",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"UpdatedLeo","LeopardTypeId":1,"Weight":55,"Characteristics":"Updated","CareNeeds":"Updated care","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 204,
        Points = 0   // 0 pts – prevents double-scoring; 204 is an accepted alternate only
    },
    new()
    {
        Name = "PUT /api/LeopardProfile/3 – moderator → 200/201/204",
        Method = HttpMethod.Put,
        Path = "/api/LeopardProfile/3",
        BearerTokenEmail = "moderator@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"ModUpdated","LeopardTypeId":1,"Weight":45,"Characteristics":"Mod updated","CareNeeds":"Care","ModifiedDate":"2025-06-20T00:00:00"}""",
        ExpectedStatus = 200,
        AlternateStatus = 201,
        Points = 4
    },

    // DELETE
    new()
    {
        Name = "DELETE /api/LeopardProfile/4 – administrator → 200/204",
        Method = HttpMethod.Delete,
        Path = "/api/LeopardProfile/4",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 200,
        AlternateStatus = 204,
        PathHint = "delete_4",
        Points = 4
    },
    new()
    {
        Name = "DELETE /api/LeopardProfile/5 – developer → 403",
        Method = HttpMethod.Delete,
        Path = "/api/LeopardProfile/5",
        BearerTokenEmail = "developer@leopard.com",
        BearerTokenPassword = "@1",
        ExpectedStatus = 403,
        PathHint = "delete_5",
        Points = 5
    },

    // ── 3. Error Code Format HB400001  (~1.54 pts total) ──────────────
    new()
    {
        Name = "POST weight ≤ 15 → 400 + error code HB400001",
        Method = HttpMethod.Post,
        Path = "/api/LeopardProfile",
        BearerTokenEmail = "administrator@leopard.com",
        BearerTokenPassword = "@1",
        JsonBody = """{"LeopardName":"TinyLeo","LeopardTypeId":1,"Weight":10,"Characteristics":"x","CareNeeds":"x"}""",
        ExpectedStatus = 400,
        ExpectedContentContains = "HB400001",
        Points = 15  // full 1.5 pts allocated here as sole test for this section
    },
};
    }
}
