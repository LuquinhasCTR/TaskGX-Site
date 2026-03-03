using TaskGX.ApiModels;

namespace TaskGX.Web.Services
{
    public class AuthApiClient
    {
        private readonly ApiClient _api;

        public AuthApiClient(ApiClient api)
        {
            _api = api;
        }

        public Task RegisterAsync(string nome, string email, string senha)
        {
            return _api.PostAsync<object>(
                "/api/registration/register",
                new { nome, email, senha },
                auth: false
            )!;
        }

        public Task<LoginResponse?> LoginAsync(string email, string senha)
        {
            return _api.PostAsync<LoginResponse>(
                "/api/auth/login",
                new { email, senha },
                auth: false
            );
        }

        public class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public UsuarioDTO Usuario { get; set; } = new();
        }
    }
}
