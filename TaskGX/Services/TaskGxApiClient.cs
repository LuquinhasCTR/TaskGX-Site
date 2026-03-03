using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskGX.Models;

namespace TaskGX.Services;

public sealed class TaskGxApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public TaskGxApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResult<LoginResponse>> LoginAsync(string email, string senha)
    {
        return await SendAsync<LoginResponse>(HttpMethod.Post, "/api/auth/login", new
        {
            Email = email,
            Senha = senha
        });
    }

    public async Task<ApiResult> RegistrarAsync(string nome, string email, string senha, string confirmarSenha)
    {
        return await SendWithoutResultAsync(HttpMethod.Post, "/api/register", new
        {
            Nome = nome,
            Email = email,
            Senha = senha,
            ConfirmarSenha = confirmarSenha
        });
    }

    public async Task<ApiResult> VerificarEmailAsync(string email, string codigo)
    {
        return await SendWithoutResultAsync(HttpMethod.Post, "/api/verification/verify-email", new
        {
            Email = email,
            Codigo = codigo
        });
    }

    public async Task<ApiResult> ReenviarCodigoAsync(string email)
    {
        return await SendWithoutResultAsync(HttpMethod.Post, "/api/verification/resend-code", new { Email = email });
    }

    public async Task<ApiResult<List<Lista>>> ObterListasAsync(string token)
    {
        return await SendAsync<List<Lista>>(HttpMethod.Get, "/api/Listas", token: token);
    }

    public async Task<ApiResult<List<Prioridade>>> ObterPrioridadesAsync(string token)
    {
        return await SendAsync<List<Prioridade>>(HttpMethod.Get, "/api/Prioridades", token: token);
    }

    public async Task<ApiResult<List<Tarefa>>> ObterTarefasAsync(string token)
    {
        return await SendAsync<List<Tarefa>>(HttpMethod.Get, "/api/Tarefas", token: token);
    }

    public async Task<ApiResult<Usuarios>> ObterPerfilAsync(string token)
    {
        return await SendAsync<Usuarios>(HttpMethod.Get, "/api/Usuarios/me", token: token);
    }

    public async Task<ApiResult> AtualizarPerfilAsync(string token, string nome, string email)
    {
        return await SendWithoutResultAsync(HttpMethod.Put, "/api/Usuarios/me", new
        {
            Nome = nome,
            Email = email
        }, token);
    }

    public async Task<ApiResult> AlterarSenhaAsync(string token, string senhaAtual, string novaSenha, string confirmarNovaSenha)
    {
        return await SendWithoutResultAsync(HttpMethod.Patch, "/api/Usuarios/me/password", new
        {
            SenhaAtual = senhaAtual,
            NovaSenha = novaSenha,
            ConfirmarNovaSenha = confirmarNovaSenha
        }, token);
    }

    public async Task<ApiResult<Lista>> CriarListaAsync(string token, string nome, string? cor)
    {
        return await SendAsync<Lista>(HttpMethod.Post, "/api/Listas", new
        {
            Nome = nome,
            Cor = cor
        }, token);
    }

    public async Task<ApiResult> AtualizarListaAsync(string token, int listaId, string nome, string? cor)
    {
        return await SendWithoutResultAsync(HttpMethod.Put, $"/api/Listas/{listaId}", new
        {
            Nome = nome,
            Cor = cor
        }, token);
    }

    public async Task<ApiResult> ExcluirListaAsync(string token, int listaId)
    {
        return await SendWithoutResultAsync(HttpMethod.Delete, $"/api/Listas/{listaId}", token: token);
    }

    public async Task<ApiResult<Tarefa>> CriarTarefaAsync(string token, int listaId, string titulo, string? descricao, int? prioridadeId, DateTime? dataVencimento, string? tags)
    {
        return await SendAsync<Tarefa>(HttpMethod.Post, "/api/Tarefas", new
        {
            ListaId = listaId,
            Titulo = titulo,
            Descricao = descricao,
            PrioridadeId = prioridadeId,
            DataVencimento = dataVencimento,
            Tags = tags
        }, token);
    }

    public async Task<ApiResult> AtualizarTarefaAsync(string token, int tarefaId, string titulo, string? descricao, int? prioridadeId, DateTime? dataVencimento, string? tags)
    {
        return await SendWithoutResultAsync(HttpMethod.Put, $"/api/Tarefas/{tarefaId}", new
        {
            Titulo = titulo,
            Descricao = descricao,
            PrioridadeId = prioridadeId,
            DataVencimento = dataVencimento,
            Tags = tags
        }, token);
    }

    public async Task<ApiResult> ExcluirTarefaAsync(string token, int tarefaId)
    {
        return await SendWithoutResultAsync(HttpMethod.Delete, $"/api/Tarefas/{tarefaId}", token: token);
    }

    public async Task<ApiResult> ConcluirTarefaAsync(string token, int tarefaId)
    {
        return await SendWithoutResultAsync(HttpMethod.Post, $"/api/Tarefas/{tarefaId}/concluir", token: token);
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? payload = null, string? token = null)
    {
        using var request = BuildRequest(method, url, payload, token);
        using var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<T>.Fail(ResolveErrorMessage(body, response.StatusCode));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return ApiResult<T>.Fail("A API respondeu sem conteúdo.");
        }

        var parsed = TryParse<ApiEnvelope<T>>(body);
        if (parsed is not null)
        {
            if (!parsed.Success)
            {
                return ApiResult<T>.Fail(parsed.Message ?? "Falha ao processar resposta da API.");
            }

            if (parsed.Data is null)
            {
                return ApiResult<T>.Fail("A API respondeu sem os dados esperados.");
            }

            return ApiResult<T>.Ok(parsed.Data, parsed.Message);
        }

        var raw = TryParse<T>(body);
        return raw is null
            ? ApiResult<T>.Fail("Não foi possível interpretar a resposta da API.")
            : ApiResult<T>.Ok(raw);
    }

    private async Task<ApiResult> SendWithoutResultAsync(HttpMethod method, string url, object? payload = null, string? token = null)
    {
        using var request = BuildRequest(method, url, payload, token);
        using var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return ApiResult.Fail(ResolveErrorMessage(body, response.StatusCode));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return ApiResult.Ok();
        }

        var envelope = TryParse<ApiEnvelope<object>>(body);
        if (envelope is null)
        {
            return ApiResult.Ok();
        }

        return envelope.Success
            ? ApiResult.Ok(envelope.Message)
            : ApiResult.Fail(envelope.Message ?? "Falha ao processar resposta da API.");
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url, object? payload, string? token)
    {
        var request = new HttpRequestMessage(method, url);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (payload is not null)
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static T? TryParse<T>(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private static string ResolveErrorMessage(string body, HttpStatusCode statusCode)
    {
        var envelope = TryParse<ApiEnvelope<object>>(body);
        if (!string.IsNullOrWhiteSpace(envelope?.Message))
        {
            return envelope.Message;
        }

        var validation = TryParse<ValidationProblem>(body);
        if (validation?.Errors?.Count > 0)
        {
            return string.Join(" ", validation.Errors.SelectMany(kvp => kvp.Value));
        }

        return statusCode switch
        {
            HttpStatusCode.Unauthorized => "Sessão inválida. Faça login novamente.",
            HttpStatusCode.Forbidden => "Acesso negado.",
            _ => "Falha na comunicação com a API."
        };
    }

    private sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    private sealed class ValidationProblem
    {
        public Dictionary<string, string[]> Errors { get; set; } = [];
    }

    public sealed record LoginResponse(
        int Id,
        string Nome,
        string Email,
        string Token,
        [property: JsonPropertyName("emailVerificado")] bool EmailVerificado = true,
        [property: JsonPropertyName("ativo")] bool Ativo = true);
}

public sealed record ApiResult(bool Success, string Message)
{
    public static ApiResult Ok(string? message = null) => new(true, message ?? string.Empty);

    public static ApiResult Fail(string message) => new(false, message);
}

public sealed record ApiResult<T>(bool Success, string Message, T? Data)
{
    public static ApiResult<T> Ok(T data, string? message = null) => new(true, message ?? string.Empty, data);

    public static ApiResult<T> Fail(string message) => new(false, message, default);
}
