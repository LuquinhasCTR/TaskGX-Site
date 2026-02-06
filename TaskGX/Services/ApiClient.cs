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

        private void ApplyAuthHeader()
        {
            var token = _ctx.HttpContext?.Session.GetString(SessionTokenKey);

            // Limpa header antigo para evitar ficar com token velho
            _http.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrWhiteSpace(token))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public void SetToken(string token)
        {
            _ctx.HttpContext?.Session.SetString(SessionTokenKey, token);
        }

        public void ClearToken()
        {
            _ctx.HttpContext?.Session.Remove(SessionTokenKey);
        }

        public string? GetToken()
        {
            return _ctx.HttpContext?.Session.GetString(SessionTokenKey);
        }

    }
}
