using TaskGX.ApiModels;
using TaskGX.Web.Services;

namespace TaskGX.Services
{
    public class ListasApiService
    {
        private readonly ApiClient _api;

        public ListasApiService(ApiClient api)
        {
            _api = api;
        }

        // GET /api/listas
        public async Task<List<ListaDTO>> ObterListasAsync()
        {
            return await _api.GetAsync<List<ListaDTO>>("/api/listas", auth: true)
                   ?? new List<ListaDTO>();
        }

        // POST /api/listas
        public async Task<ListaDTO?> CriarListaAsync(string nome, string? cor = null)
        {
            return await _api.PostAsync<ListaDTO>(
                "/api/listas",
                new { nome, cor },
                auth: true
            );
        }

        // PUT /api/listas/{id}
        public async Task AtualizarListaAsync(int id, string nome, string? cor, bool favorita)
        {
            await _api.PutAsync<object>(
                $"/api/listas/{id}",
                new { id, nome, cor, favorita },
                auth: true
            );
        }

        // DELETE /api/listas/{id}
        public async Task RemoverListaAsync(int id)
        {
            await _api.DeleteAsync($"/api/listas/{id}", auth: true);
        }
    }
}
