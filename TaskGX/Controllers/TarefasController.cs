using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TaskGX.ApiModels;
using TaskGX.Services;
using TaskGX.Web.Services;

namespace TaskGX.Controllers
{
    public class TarefasController : Controller
    {
        private readonly ListasApiService _listasApi;
        private readonly TarefasApiService _tarefasApi;
        private readonly ApiClient _api; // para chamadas extras se precisar
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

        // =========================
        // LISTAS (do modal "Nova Lista")
        // =========================
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
            catch (TaskGX.Web.Services.ApiUnauthorizedException)
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

        // =========================
        // TAREFAS (modal "Nova Tarefa" e "Editar")
        // =========================
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

                    // Para editar com segurança, pegamos as tarefas da lista e achamos a tarefa atual
                    var tarefas = await _tarefasApi.ObterPorListaAsync(listaIdDestino);
                    var atual = tarefas.FirstOrDefault(t => t.ID == tarefa_id.Value);
                    if (atual == null)
                    {
                        TempData["Error"] = "Tarefa não encontrada.";
                        return RedirectToAction("Dashboard", "Home", new { listaId = listaIdDestino });
                    }

                    await _tarefasApi.AtualizarAsync(
                        id: atual.ID,
                        listaId: atual.ListaId,
                        titulo: titulo.Trim(),
                        descricao: descricao,
                        tags: tags,
                        prioridadeId: prioridade_id,
                        concluida: atual.Concluida,
                        arquivada: atual.Arquivada,
                        dataVencimento: data_vencimento,
                        ordem: 0
                    );

                    TempData["Success"] = "Tarefa atualizada com sucesso.";
                }
                else
                {
                    TempData["Error"] = "Ação inválida para tarefa.";
                }
            }
            catch (TaskGX.Web.Services.ApiUnauthorizedException)
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

        // =========================
        // Marcar concluída (JS -> fetch)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarConcluida([FromBody] AtualizarConcluidaRequest request)
        {
            if (!IsLogado()) return Unauthorized();

            try
            {
                // Para conseguir "desmarcar", fazemos PUT completo baseado no DTO atual
                // (A API hoje não tem endpoint de "desconcluir", então a gente atualiza via PUT)
                var tarefas = await _tarefasApi.ObterPorListaAsync(request.ListaId);
                var atual = tarefas.FirstOrDefault(t => t.ID == request.TarefaId);
                if (atual == null)
                    return Json(new { success = false, message = "Tarefa não encontrada." });

                await _tarefasApi.AtualizarAsync(
                    id: atual.ID,
                    listaId: atual.ListaId,
                    titulo: atual.Titulo,
                    descricao: atual.Descricao,
                    tags: atual.Tags,
                    prioridadeId: atual.PrioridadeId,
                    concluida: request.Concluida,
                    arquivada: atual.Arquivada,
                    dataVencimento: atual.DataVencimento,
                    ordem: 0
                );

                return Json(new { success = true, message = string.Empty });
            }
            catch (TaskGX.Web.Services.ApiUnauthorizedException)
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

        // =========================
        // Excluir tarefa (JS -> fetch)
        // =========================
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
            catch (TaskGX.Web.Services.ApiUnauthorizedException)
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

        // =========================
        // Duplicar tarefa (JS -> fetch)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicar([FromBody] DuplicarTarefaRequest request)
        {
            if (!IsLogado()) return Unauthorized();

            try
            {
                var tarefas = await _tarefasApi.ObterPorListaAsync(request.ListaId);
                var atual = tarefas.FirstOrDefault(t => t.ID == request.TarefaId);
                if (atual == null)
                    return Json(new { success = false, message = "Tarefa não encontrada." });

                await _tarefasApi.CriarAsync(
                    listaId: atual.ListaId,
                    titulo: atual.Titulo,
                    descricao: atual.Descricao,
                    tags: atual.Tags,
                    prioridadeId: atual.PrioridadeId,
                    dataVencimento: atual.DataVencimento
                );

                return Json(new { success = true, message = string.Empty });
            }
            catch (TaskGX.Web.Services.ApiUnauthorizedException)
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

        // =========================
        // Exportar (continua no site, mas dados vêm da API)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Exportar(string formato, int listaId)
        {
            if (!IsLogado()) return RedirectToAction("Login", "Home");

            try
            {
                var listas = await _listasApi.ObterListasAsync();
                var lista = listas.FirstOrDefault(l => l.ID == listaId);
                if (lista == null)
                    return RedirectToAction("Dashboard", "Home");

                var tarefas = await _tarefasApi.ObterPorListaAsync(listaId);

                if (string.Equals(formato, "csv", StringComparison.OrdinalIgnoreCase))
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("ID,Titulo,Descricao,Prioridade,Tags,Concluida,DataVencimento,DataCriacao");

                    foreach (var t in tarefas)
                    {
                        csv.AppendLine(string.Join(',', new[]
                        {
                            t.ID.ToString(),
                            EscapeCsv(t.Titulo),
                            EscapeCsv(t.Descricao),
                            EscapeCsv(t.PrioridadeNome),
                            EscapeCsv(t.Tags),
                            t.Concluida ? "1" : "0",
                            t.DataVencimento?.ToString("yyyy-MM-dd") ?? string.Empty,
                            t.DataCriacao.ToString("yyyy-MM-dd")
                        }));
                    }

                    return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"tarefas_{listaId}.csv");
                }

                var json = JsonSerializer.Serialize(tarefas);
                return File(Encoding.UTF8.GetBytes(json), "application/json", $"tarefas_{listaId}.json");
            }
            catch (TaskGX.Web.Services.ApiUnauthorizedException)
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

        // =========================
        // Helpers
        // =========================
        private bool IsLogado() => !string.IsNullOrWhiteSpace(_api.GetToken());

        private void LimparSessao()
        {
            HttpContext.Session.Clear();
            _api.ClearToken();
        }

        private static string EscapeCsv(string? valor)
        {
            if (string.IsNullOrEmpty(valor)) return string.Empty;

            var precisaEscape = valor.Contains(',') || valor.Contains('"') || valor.Contains('\n');
            if (!precisaEscape) return valor;

            return $"\"{valor.Replace("\"", "\"\"")}\"";
        }

        public sealed record AtualizarConcluidaRequest(int TarefaId, bool Concluida, int ListaId);
        public sealed record TarefaIdRequest(int TarefaId);
        public sealed record DuplicarTarefaRequest(int TarefaId, int ListaId);
    }
}
