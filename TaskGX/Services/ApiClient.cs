using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TaskGX.Web.Services
{
    public class ApiClient
    {
        private const string SessionTokenKey = "JWT";

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _ctx;
        private readonly JsonSerializerOptions _json;

        public ApiClient(HttpClient http, IHttpContextAccessor ctx)
        {
            _http = http;
            _ctx = ctx;

            _json = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }
    }
}
