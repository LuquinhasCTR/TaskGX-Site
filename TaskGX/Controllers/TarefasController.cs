using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskGX.Services;

namespace TaskGX.Controllers;

public class TarefasController : Controller
{
    private readonly TaskGxApiClient _apiClient;
    private readonly ILogger<TarefasController> _logger;

    public TarefasController(TaskGxApiClient apiClient, ILogger<TarefasController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessarLista(string nome, string cor, string acao)
    {
        if (!TryObterToken(out var token))
        {
            return RedirectToAction("Login", "Home");
        }

        if (!string.Equals(acao, "criar", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Ação inválida para lista.";
            return RedirectToAction("Dashboard", "Home");
        }

        var resultado = await _apiClient.CriarListaAsync(token, nome.Trim(), cor);
        TempData[resultado.Success ? "Success" : "Error"] = string.IsNullOrWhiteSpace(resultado.Message)
            ? (resultado.Success ? "Lista criada com sucesso." : "Não foi possível criar a lista.")
            : resultado.Message;

        return RedirectToAction("Dashboard", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessarTarefa(string titulo, string descricao, int? prioridade_id, DateTime? data_vencimento, string tags, int? lista_id, int? tarefa_id, string acao)
    {
        if (!TryObterToken(out var token))
        {
            return RedirectToAction("Login", "Home");
        }

        if (string.IsNullOrWhiteSpace(titulo))
        {
            TempData["Error"] = "Informe o título da tarefa.";
            return RedirectToAction("Dashboard", "Home", new { listaId = lista_id ?? 0 });
        }

        if (!lista_id.HasValue || lista_id <= 0)
        {
            TempData["Error"] = "Selecione uma lista para a tarefa.";
            return RedirectToAction("Dashboard", "Home");
        }

        if (string.Equals(acao, "editar", StringComparison.OrdinalIgnoreCase))
        {
            if (!tarefa_id.HasValue || tarefa_id <= 0)
            {
                TempData["Error"] = "Tarefa inválida para edição.";
                return RedirectToAction("Dashboard", "Home", new { listaId = lista_id.Value });
            }

            var result = await _apiClient.AtualizarTarefaAsync(token, tarefa_id.Value, titulo.Trim(), descricao, prioridade_id, data_vencimento, tags);
            TempData[result.Success ? "Success" : "Error"] = string.IsNullOrWhiteSpace(result.Message)
                ? (result.Success ? "Tarefa atualizada com sucesso." : "Não foi possível atualizar a tarefa.")
                : result.Message;

            return RedirectToAction("Dashboard", "Home", new { listaId = lista_id.Value });
        }

        var criar = await _apiClient.CriarTarefaAsync(token, lista_id.Value, titulo.Trim(), descricao, prioridade_id, data_vencimento, tags);
        TempData[criar.Success ? "Success" : "Error"] = string.IsNullOrWhiteSpace(criar.Message)
            ? (criar.Success ? "Tarefa criada com sucesso." : "Não foi possível criar a tarefa.")
            : criar.Message;

        return RedirectToAction("Dashboard", "Home", new { listaId = lista_id.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarConcluida([FromBody] AtualizarConcluidaRequest request)
    {
        if (!TryObterToken(out var token))
        {
            return Unauthorized();
        }

        try
        {
            if (!request.Concluida)
            {
                return Json(new { success = false, message = "A API atual suporta apenas concluir tarefas." });
            }

            var result = await _apiClient.ConcluirTarefaAsync(token, request.TarefaId);
            return Json(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar tarefa.");
            return BadRequest();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir([FromBody] TarefaIdRequest request)
    {
        if (!TryObterToken(out var token))
        {
            return Unauthorized();
        }

        var result = await _apiClient.ExcluirTarefaAsync(token, request.TarefaId);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicar([FromBody] TarefaIdRequest request)
    {
        if (!TryObterToken(out var token))
        {
            return Unauthorized();
        }

        var tarefas = await _apiClient.ObterTarefasAsync(token);
        var original = tarefas.Data?.FirstOrDefault(t => t.ID == request.TarefaId);
        if (!tarefas.Success || original is null || !original.ListaId.HasValue)
        {
            return Json(new { success = false, message = "Não foi possível duplicar a tarefa." });
        }

        var copiar = await _apiClient.CriarTarefaAsync(token, original.ListaId.Value, $"{original.Titulo} (cópia)", original.Descricao, original.PrioridadeId, original.DataVencimento, original.Tags);
        return Json(new { success = copiar.Success, message = copiar.Message });
    }

    [HttpGet]
    public async Task<IActionResult> Exportar(string formato, int listaId)
    {
        if (!TryObterToken(out var token))
        {
            return RedirectToAction("Login", "Home");
        }

        var tarefasResult = await _apiClient.ObterTarefasAsync(token);
        if (!tarefasResult.Success)
        {
            TempData["Error"] = tarefasResult.Message;
            return RedirectToAction("Dashboard", "Home", new { listaId });
        }

        var tarefas = tarefasResult.Data?.Where(t => t.ListaId == listaId).ToList() ?? [];

        if (string.Equals(formato, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,Titulo,Descricao,Prioridade,Tags,Concluida,DataVencimento,DataCriacao");
            foreach (var tarefa in tarefas)
            {
                csv.AppendLine(string.Join(',',
                [
                    tarefa.ID.ToString(),
                    EscapeCsv(tarefa.Titulo),
                    EscapeCsv(tarefa.Descricao),
                    EscapeCsv(tarefa.PrioridadeNome),
                    EscapeCsv(tarefa.Tags),
                    tarefa.Concluida ? "1" : "0",
                    tarefa.DataVencimento?.ToString("yyyy-MM-dd") ?? string.Empty,
                    tarefa.DataCriacao.ToString("yyyy-MM-dd")
                ]));
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"tarefas_{listaId}.csv");
        }

        var json = JsonSerializer.Serialize(tarefas);
        return File(Encoding.UTF8.GetBytes(json), "application/json", $"tarefas_{listaId}.json");
    }

    private bool TryObterToken(out string token)
    {
        token = HttpContext.Session.GetString("UsuarioToken") ?? string.Empty;
        return !string.IsNullOrWhiteSpace(token);
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

    public sealed record AtualizarConcluidaRequest(int TarefaId, bool Concluida);
    public sealed record TarefaIdRequest(int TarefaId);
}
