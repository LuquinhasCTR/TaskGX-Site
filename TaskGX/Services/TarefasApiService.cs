using TaskGX.ApiModels;
using TaskGX.Web.Services;

namespace TaskGX.Services
{
    public class TarefasApiService
    {
        private readonly ApiClient _api;

        public TarefasApiService(ApiClient api)
        {
            _api = api;
        }

        // GET /api/tarefas?listaId=1
        public async Task<List<TarefaDTO>> ObterPorListaAsync(int listaId)
        {
            return await _api.GetAsync<List<TarefaDTO>>(
                $"/api/tarefas?listaId={listaId}",
                auth: true
            ) ?? new List<TarefaDTO>();
        }

        // POST /api/tarefas
        public async Task<TarefaDTO?> CriarAsync(
            int listaId,
            string titulo,
            string? descricao = null,
            string? tags = null,
            int? prioridadeId = null,
            DateTime? dataVencimento = null
        )
        {
            return await _api.PostAsync<TarefaDTO>(
                "/api/tarefas",
                new
                {
                    listaID = listaId,
                    titulo,
                    descricao,
                    tags,
                    prioridadeID = prioridadeId,
                    dataVencimento
                },
                auth: true
            );
        }

        // PUT /api/tarefas/{id}
        public async Task AtualizarAsync(
            int id,
            int listaId,
            string titulo,
            string? descricao,
            string? tags,
            int? prioridadeId,
            bool concluida,
            bool arquivada,
            DateTime? dataVencimento,
            int ordem
        )
        {
            await _api.PutAsync<object>(
                $"/api/tarefas/{id}",
                new
                {
                    id,
                    listaID = listaId,
                    titulo,
                    descricao,
                    tags,
                    prioridadeID = prioridadeId,
                    concluida,
                    arquivada,
                    dataVencimento,
                    ordem
                },
                auth: true
            );
        }

        // PUT /api/tarefas/{id}/detalhes
        public async Task AtualizarDetalhesAsync(
            int id,
            int listaId,
            string titulo,
            string? descricao,
            string? tags,
            int? prioridadeId,
            DateTime? dataVencimento
        )
        {
            await _api.PutAsync<object>(
                $"/api/tarefas/{id}/detalhes",
                new
                {
                    id,
                    listaID = listaId,
                    titulo,
                    descricao,
                    tags,
                    prioridadeID = prioridadeId,
                    dataVencimento
                },
                auth: true
            );
        }

        // POST /api/tarefas/{id}/concluir
        public async Task ConcluirAsync(int id)
        {
            await _api.PostAsync<object>(
                $"/api/tarefas/{id}/concluir",
                new { },
                auth: true
            );
        }

        // PUT /api/tarefas/{id}/conclusao
        public async Task AtualizarConclusaoAsync(int id, bool concluida)
        {
            await _api.PutAsync<object>(
                $"/api/tarefas/{id}/conclusao",
                new { concluida },
                auth: true
            );
        }

        // POST /api/tarefas/{id}/duplicar
        public async Task DuplicarAsync(int id)
        {
            await _api.PostAsync<object>(
                $"/api/tarefas/{id}/duplicar",
                new { },
                auth: true
            );
        }

        // GET /api/tarefas/exportar?listaId={listaId}&formato={formato}
        public async Task<byte[]?> ExportarAsync(int listaId, string formato)
        {
            return await _api.GetAsync<byte[]>(
                $"/api/tarefas/exportar?listaId={listaId}&formato={formato}",
                auth: true
            );
        }

        // DELETE /api/tarefas/{id}
        public async Task RemoverAsync(int id)
        {
            await _api.DeleteAsync(
                $"/api/tarefas/{id}",
                auth: true
            );
        }
    }
}
