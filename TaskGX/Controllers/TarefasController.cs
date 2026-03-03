using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TaskGX.Services;
using TaskGX.Web.Services;

namespace TaskGX.Controllers
{
    public class TarefasController : Controller
    {
        private readonly ListasApiService _listasApi;
        private readonly TarefasApiService _tarefasApi;
        private readonly ApiClient _api;
        private readonly ILogger<TarefasController> _logger;

        public TarefasController(
            ListasApiService listasApi,
            TarefasApiService tarefasApi,
            TaskGX.Web.Services.ApiClient api,
            ILogger<TarefasController> logger)
        {
            _listasApi = listasApi;
            _tarefasApi = tarefasApi;
            _api = api;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessarLista(string nome, string cor, string acao)
        {
            if (!IsLogado()) return RedirectToAction("Login", "Home");

            if (!string.Equals(acao, "criar", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Ação inválida para lista.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (string.IsNullOrWhiteSpace(nome))
            {
                TempData["Error"] = "Informe o nome da lista.";
                return RedirectToAction("Dashboard", "Home");
            }

            try
            {
                await _listasApi.CriarListaAsync(nome.Trim(), cor);
                TempData["Success"] = "Lista criada com sucesso.";
            }
            catch (ApiUnauthorizedException)
            {
                LimparSessao();
                return RedirectToAction("Login", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar lista via API.");
                TempData["Error"] = "Erro ao criar lista. Tente novamente.";
            }

            return RedirectToAction("Dashboard", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessarTarefa(
            string titulo,
            string descricao,
            int? prioridade_id,
            DateTime? data_vencimento,
            string tags,
            int? lista_id,
            int? tarefa_id,
            string acao)
        {
            if (!IsLogado()) return RedirectToAction("Login", "Home");

            if (string.IsNullOrWhiteSpace(titulo))
            {
                TempData["Error"] = "Informe o título da tarefa.";
                return RedirectToAction("Dashboard", "Home", new { listaId = lista_id ?? 0 });
            }

            var listaIdDestino = lista_id ?? 0;
            if (listaIdDestino <= 0)
            {
                TempData["Error"] = "Selecione uma lista para criar a tarefa.";
                return RedirectToAction("Dashboard", "Home");
            }

            try
            {
                if (string.Equals(acao, "criar", StringComparison.OrdinalIgnoreCase))
                {
                    await _tarefasApi.CriarAsync(
                        listaIdDestino,
                        titulo.Trim(),
                        descricao,
                        tags,
                        prioridade_id,
                        data_vencimento
                    );

                    TempData["Success"] = "Tarefa criada com sucesso.";
                }
                else if (string.Equals(acao, "editar", StringComparison.OrdinalIgnoreCase))
                {
                    if (!tarefa_id.HasValue || tarefa_id.Value <= 0)
                    {
                        TempData["Error"] = "Tarefa inválida para edição.";
                        return RedirectToAction("Dashboard", "Home", new { listaId = listaIdDestino });
                    }

                    await _tarefasApi.AtualizarDetalhesAsync(
                        id: tarefa_id.Value,
                        listaId: listaIdDestino,
                        titulo: titulo.Trim(),
                        descricao: descricao,
                        tags: tags,
                        prioridadeId: prioridade_id,
                        dataVencimento: data_vencimento
                    );

                    TempData["Success"] = "Tarefa atualizada com sucesso.";
                }
                else
                {
                    TempData["Error"] = "Ação inválida para tarefa.";
                }
            }
            catch (ApiUnauthorizedException)
            {
                LimparSessao();
                return RedirectToAction("Login", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar tarefa via API.");
                TempData["Error"] = "Erro ao salvar tarefa. Tente novamente.";
            }

            return RedirectToAction("Dashboard", "Home", new { listaId = listaIdDestino });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarConcluida([FromBody] AtualizarConcluidaRequest request)
        {
            if (!IsLogado()) return Unauthorized();

            try
            {
                await _tarefasApi.AtualizarConclusaoAsync(request.TarefaId, request.Concluida);
                return Json(new { success = true, message = string.Empty });
            }
            catch (ApiUnauthorizedException)
            {
                LimparSessao();
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao marcar tarefa via API.");
                return Json(new { success = false, message = "Erro ao atualizar a tarefa." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir([FromBody] TarefaIdRequest request)
        {
            if (!IsLogado()) return Unauthorized();

            try
            {
                await _tarefasApi.RemoverAsync(request.TarefaId);
                return Json(new { success = true, message = string.Empty });
            }
            catch (ApiUnauthorizedException)
            {
                LimparSessao();
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir tarefa via API.");
                return Json(new { success = false, message = "Não foi possível excluir a tarefa." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicar([FromBody] DuplicarTarefaRequest request)
        {
            if (!IsLogado()) return Unauthorized();

            try
            {
                await _tarefasApi.DuplicarAsync(request.TarefaId);
                return Json(new { success = true, message = string.Empty });
            }
            catch (ApiUnauthorizedException)
            {
                LimparSessao();
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao duplicar tarefa via API.");
                return Json(new { success = false, message = "Não foi possível duplicar a tarefa." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Exportar(string formato, int listaId)
        {
            if (!IsLogado()) return RedirectToAction("Login", "Home");

            try
            {
                var extension = string.Equals(formato, "csv", StringComparison.OrdinalIgnoreCase) ? "csv" : "json";
                var contentType = extension == "csv" ? "text/csv" : "application/json";
                var arquivo = await _tarefasApi.ExportarAsync(listaId, extension);

                if (arquivo == null || arquivo.Length == 0)
                {
                    TempData["Error"] = "Nenhum dado para exportar.";
                    return RedirectToAction("Dashboard", "Home", new { listaId });
                }

                return File(arquivo, contentType, $"tarefas_{listaId}.{extension}");
            }
            catch (ApiUnauthorizedException)
            {
                LimparSessao();
                return RedirectToAction("Login", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exportar tarefas via API.");
                TempData["Error"] = "Erro ao exportar tarefas.";
                return RedirectToAction("Dashboard", "Home", new { listaId });
            }
        }

        private bool IsLogado() => !string.IsNullOrWhiteSpace(_api.GetToken());

        private void LimparSessao()
        {
            HttpContext.Session.Clear();
            _api.ClearToken();
        }

        public sealed record AtualizarConcluidaRequest(int TarefaId, bool Concluida, int ListaId);
        public sealed record TarefaIdRequest(int TarefaId);
        public sealed record DuplicarTarefaRequest(int TarefaId, int ListaId);
    }
}
