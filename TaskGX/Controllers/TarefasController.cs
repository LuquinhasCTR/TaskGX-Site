using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TaskGX.Data;

namespace TaskGX.Controllers
{
    public class TarefasController : Controller
    {
        private readonly RepositorioDashboard _dashboardRepositorio;
        private readonly RepositorioTarefas _tarefasRepositorio;
        private readonly ILogger<TarefasController> _logger;

        public TarefasController(
            RepositorioDashboard dashboardRepositorio,
            RepositorioTarefas tarefasRepositorio,
            ILogger<TarefasController> logger)
        {
            _dashboardRepositorio = dashboardRepositorio;
            _tarefasRepositorio = tarefasRepositorio;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessarLista(string nome, string cor, string acao)
        {
            if (!TryObterUsuarioId(out var usuarioId))
            {
                return RedirectToAction("Login", "Home");
            }

            if (string.IsNullOrWhiteSpace(nome))
            {
                TempData["Error"] = "Informe o nome da lista.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!string.Equals(acao, "criar", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Ação inválida para lista.";
                return RedirectToAction("Dashboard", "Home");
            }

            try
            {
                var favoritaExists = await _dashboardRepositorio.ColunaExisteAsync("Listas", "Favorita");
                await _tarefasRepositorio.CriarListaAsync(usuarioId, nome.Trim(), cor, favoritaExists);
                TempData["Success"] = "Lista criada com sucesso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar lista.");
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
            string acao)
        {
            if (!TryObterUsuarioId(out var usuarioId))
            {
                return RedirectToAction("Login", "Home");
            }

            if (string.IsNullOrWhiteSpace(titulo))
            {
                TempData["Error"] = "Informe o título da tarefa.";
                return RedirectToAction("Dashboard", "Home", new { listaId = lista_id ?? 0 });
            }

            if (!string.Equals(acao, "criar", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Ação inválida para tarefa.";
                return RedirectToAction("Dashboard", "Home", new { listaId = lista_id ?? 0 });
            }

            int? listaIdDestino = null;

            try
            {
                listaIdDestino = lista_id;
                if (!listaIdDestino.HasValue || listaIdDestino <= 0)
                {
                    var favoritaExists = await _dashboardRepositorio.ColunaExisteAsync("Listas", "Favorita");
                    listaIdDestino = await _tarefasRepositorio.ObterOuCriarListaPadraoAsync(usuarioId, favoritaExists);
                }

                await _tarefasRepositorio.CriarTarefaAsync(
                    listaIdDestino,
                    titulo.Trim(),
                    descricao,
                    prioridade_id,
                    data_vencimento,
                    tags);

                TempData["Success"] = "Tarefa criada com sucesso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar tarefa.");
                TempData["Error"] = "Erro ao criar tarefa. Tente novamente.";
            }

            return RedirectToAction("Dashboard", "Home", new { listaId = listaIdDestino ?? lista_id ?? 0 });
        }

        [HttpGet]
        public async Task<IActionResult> Exportar(string formato, int listaId)
        {
            if (!TryObterUsuarioId(out var usuarioId))
            {
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var favoritaExists = await _dashboardRepositorio.ColunaExisteAsync("Listas", "Favorita");
                var lista = await _dashboardRepositorio.ObterListaAsync(listaId, usuarioId, favoritaExists);
                if (lista == null)
                {
                    return RedirectToAction("Dashboard", "Home");
                }

                var arquivadaExists = await _dashboardRepositorio.ColunaExisteAsync("Tarefas", "Arquivada");
                var tarefas = await _dashboardRepositorio.ObterTarefasPorListaAsync(listaId, arquivadaExists, true);

                if (string.Equals(formato, "csv", StringComparison.OrdinalIgnoreCase))
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("ID,Titulo,Descricao,Prioridade,Tags,Concluida,DataVencimento,DataCriacao");
                    foreach (var tarefa in tarefas)
                    {
                        csv.AppendLine(string.Join(',', new[]
                        {
                            tarefa.ID.ToString(),
                            EscapeCsv(tarefa.Titulo),
                            EscapeCsv(tarefa.Descricao),
                            EscapeCsv(tarefa.PrioridadeNome),
                            EscapeCsv(tarefa.Tags),
                            tarefa.Concluida ? "1" : "0",
                            tarefa.DataVencimento?.ToString("yyyy-MM-dd") ?? string.Empty,
                            tarefa.DataCriacao.ToString("yyyy-MM-dd")
                        }));
                    }

                    return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"tarefas_{listaId}.csv");
                }

                var json = JsonSerializer.Serialize(tarefas);
                return File(Encoding.UTF8.GetBytes(json), "application/json", $"tarefas_{listaId}.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exportar tarefas.");
                TempData["Error"] = "Erro ao exportar tarefas.";
                return RedirectToAction("Dashboard", "Home", new { listaId });
            }
        }

        private bool TryObterUsuarioId(out int usuarioId)
        {
            var usuarioIdValue = HttpContext.Session.GetString("UsuarioID");
            return int.TryParse(usuarioIdValue, out usuarioId);
        }

        private static string EscapeCsv(string? valor)
        {
            if (string.IsNullOrEmpty(valor))
            {
                return string.Empty;
            }

            var precisaEscape = valor.Contains(',') || valor.Contains('"') || valor.Contains('\n');
            if (!precisaEscape)
            {
                return valor;
            }

            return $"\"{valor.Replace("\"", "\"\"")}\"";
        }
    }
}
