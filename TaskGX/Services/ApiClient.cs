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

        // ---------- Métodos genéricos ----------

        public async Task<T?> GetAsync<T>(string path, bool auth = true, CancellationToken ct = default)
        {
            if (auth) ApplyAuthHeader();

            var res = await _http.GetAsync(path, ct);
            return await HandleResponse<T>(res, ct);
        }

        public async Task<T?> PostAsync<T>(string path, object body, bool auth = true, CancellationToken ct = default)
        {
            if (auth) ApplyAuthHeader();

            var content = ToJsonContent(body);
            var res = await _http.PostAsync(path, content, ct);
            return await HandleResponse<T>(res, ct);
        }

        public async Task<T?> PutAsync<T>(string path, object body, bool auth = true, CancellationToken ct = default)
        {
            if (auth) ApplyAuthHeader();

            var content = ToJsonContent(body);
            var res = await _http.PutAsync(path, content, ct);
            return await HandleResponse<T>(res, ct);
        }

        public async Task DeleteAsync(string path, bool auth = true, CancellationToken ct = default)
        {
            if (auth) ApplyAuthHeader();

            var res = await _http.DeleteAsync(path, ct);

            if (res.StatusCode == HttpStatusCode.Unauthorized)
                throw new ApiUnauthorizedException();

            if (!res.IsSuccessStatusCode)
            {
                var error = await res.Content.ReadAsStringAsync(ct);
                throw new ApiException((int)res.StatusCode, error);
            }
        }
    }
}
