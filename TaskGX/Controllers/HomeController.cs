using Microsoft.AspNetCore.Mvc;
using TaskGX.Models;
using TaskGX.Services;
using TaskGX.ViewModels;

namespace TaskGX.Controllers;

public class HomeController : Controller
{
    private readonly TaskGxApiClient _apiClient;
    public HomeController(TaskGxApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public IActionResult Index()
    {
        if (TryObterToken(out _))
        {
            return RedirectToAction("Dashboard");
        }

        return View();
    }

    public IActionResult Termos() => View();
    public IActionResult Privacidade() => View();
    public IActionResult Sobre() => View();

    [HttpGet]
    public IActionResult VerificarEmail(bool novo = false, bool senha = false, string? erro = null, string? sucesso = null, string? email = null)
    {
        var usuarioLogado = TryObterToken(out _);
        if (!novo && !senha && !usuarioLogado)
        {
            TempData["Error"] = "Você precisa fazer login.";
            return RedirectToAction("Login");
        }

        var usuarioEmail = HttpContext.Session.GetString("UsuarioEmail");
        var viewModel = new VerificarEmailViewModel
        {
            NovoRegistro = novo,
            TrocaSenha = senha,
            Erro = !string.IsNullOrWhiteSpace(erro) ? erro : TempData["Error"] as string,
            Sucesso = !string.IsNullOrWhiteSpace(sucesso) ? sucesso : TempData["Success"] as string,
            Email = !string.IsNullOrWhiteSpace(email) ? email : usuarioEmail
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessarVerificacaoEmail(string codigo, string tipo, string? email)
    {
        if (!string.Equals(tipo, "registro", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("VerificarEmail", new { novo = false, senha = false, email, erro = "Fluxo de verificação não disponível." });
        }

        var resultado = await _apiClient.VerificarEmailAsync(email ?? string.Empty, codigo ?? string.Empty);
        if (!resultado.Success)
        {
            return RedirectToAction("VerificarEmail", new { novo = true, email, erro = resultado.Message });
        }

        TempData["Success"] = string.IsNullOrWhiteSpace(resultado.Message) ? "Email verificado com sucesso." : resultado.Message;
        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReenviarCodigo(string tipo, string? email)
    {
        if (!string.Equals(tipo, "registro", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("VerificarEmail", new { novo = false, senha = false, email, erro = "Fluxo de reenvio não disponível." });
        }

        var resultado = await _apiClient.ReenviarCodigoAsync(email ?? string.Empty);
        if (!resultado.Success)
        {
            return RedirectToAction("VerificarEmail", new { novo = true, email, erro = resultado.Message });
        }

        return RedirectToAction("VerificarEmail", new
        {
            novo = true,
            email,
            sucesso = string.IsNullOrWhiteSpace(resultado.Message) ? "Código reenviado com sucesso." : resultado.Message
        });
    }

    [HttpGet]
    public async Task<IActionResult> Calendario()
    {
        if (!TryObterToken(out var token))
        {
            return RedirectToAction("Login");
        }

        var listasResult = await _apiClient.ObterListasAsync(token);
        var prioridadesResult = await _apiClient.ObterPrioridadesAsync(token);
        var tarefasResult = await _apiClient.ObterTarefasAsync(token);

        if (!listasResult.Success || !prioridadesResult.Success || !tarefasResult.Success)
        {
            TempData["Error"] = PrimeiraMensagemErro(listasResult.Message, prioridadesResult.Message, tarefasResult.Message);
            return RedirectToAction("Dashboard");
        }

        return View(new CalendarioViewModel
        {
            Listas = listasResult.Data ?? [],
            Prioridades = prioridadesResult.Data ?? [],
            Tarefas = tarefasResult.Data ?? []
        });
    }

    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        if (!TryObterToken(out var token))
        {
            return RedirectToAction("Login");
        }

        var usuarioResult = await _apiClient.ObterPerfilAsync(token);
        var listasResult = await _apiClient.ObterListasAsync(token);
        var tarefasResult = await _apiClient.ObterTarefasAsync(token);

        if (!usuarioResult.Success)
        {
            TempData["Error"] = usuarioResult.Message;
            return RedirectToAction("Dashboard");
        }

        var tarefas = tarefasResult.Data ?? [];

        return View(new PerfilViewModel
        {
            Usuario = usuarioResult.Data ?? new Usuarios(),
            TotalListas = listasResult.Data?.Count ?? 0,
            TotalTarefas = tarefas.Count,
            TarefasConcluidas = tarefas.Count(t => t.Concluida),
            Sucesso = TempData["Success"] as string,
            Erro = TempData["Error"] as string
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarPerfil(string nome, string email)
    {
        if (!TryObterToken(out var token))
        {
            return RedirectToAction("Login");
        }

        var resultado = await _apiClient.AtualizarPerfilAsync(token, nome, email);
        if (!resultado.Success)
        {
            TempData["Error"] = resultado.Message;
            return RedirectToAction("Perfil");
        }

        HttpContext.Session.SetString("UsuarioNome", nome.Trim());
        HttpContext.Session.SetString("UsuarioEmail", email.Trim());
        TempData["Success"] = string.IsNullOrWhiteSpace(resultado.Message) ? "Dados atualizados com sucesso." : resultado.Message;
        return RedirectToAction("Perfil");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(string senha_atual, string nova_senha, string confirmar_nova_senha)
    {
        if (!TryObterToken(out var token))
        {
            return RedirectToAction("Login");
        }

        var resultado = await _apiClient.AlterarSenhaAsync(token, senha_atual, nova_senha, confirmar_nova_senha);
        if (!resultado.Success)
        {
            TempData["Error"] = resultado.Message;
            return RedirectToAction("Perfil");
        }

        TempData["Success"] = string.IsNullOrWhiteSpace(resultado.Message) ? "Senha atualizada com sucesso." : resultado.Message;
        return RedirectToAction("Perfil");
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(int? listaId, string? sucesso = null, string? erro = null)
    {
        if (!TryObterToken(out var token))
        {
            return RedirectToAction("Login");
        }

        var usuarioId = HttpContext.Session.GetInt32("UsuarioID") ?? 0;
        var usuarioNome = HttpContext.Session.GetString("UsuarioNome") ?? "Usuário";

        var listasResult = await _apiClient.ObterListasAsync(token);
        var prioridadesResult = await _apiClient.ObterPrioridadesAsync(token);
        var tarefasResult = await _apiClient.ObterTarefasAsync(token);

        var viewModel = new DashboardViewModel
        {
            UsuarioId = usuarioId,
            UsuarioNome = usuarioNome,
            ListaId = listaId ?? 0,
            Sucesso = !string.IsNullOrWhiteSpace(sucesso) ? sucesso : TempData["Success"] as string,
            Erro = !string.IsNullOrWhiteSpace(erro) ? erro : TempData["Error"] as string,
            Listas = listasResult.Data ?? [],
            Prioridades = prioridadesResult.Data ?? []
        };

        if (!listasResult.Success || !prioridadesResult.Success || !tarefasResult.Success)
        {
            viewModel.Erro = PrimeiraMensagemErro(listasResult.Message, prioridadesResult.Message, tarefasResult.Message);
            viewModel.Tarefas = [];
            return View(viewModel);
        }

        var todasTarefas = tarefasResult.Data ?? [];
        viewModel.Stats = MontarStats(viewModel.Listas.Count, todasTarefas);

        if (listaId.HasValue && listaId.Value > 0)
        {
            viewModel.ListaSelecionada = viewModel.Listas.FirstOrDefault(l => l.ID == listaId.Value);
            viewModel.Tarefas = todasTarefas.Where(t => t.ListaId == listaId.Value).ToList();
        }
        else
        {
            viewModel.Tarefas = todasTarefas;
        }

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string senha)
    {
        var resultado = await _apiClient.LoginAsync(email, senha);
        if (!resultado.Success || resultado.Data is null)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(resultado.Message) ? "Email ou senha incorretos." : resultado.Message;
            return View();
        }

        if (!resultado.Data.EmailVerificado)
        {
            HttpContext.Session.SetString("UsuarioEmail", resultado.Data.Email);
            TempData["Error"] = "Você precisa verificar seu email antes de entrar.";
            return RedirectToAction("VerificarEmail", new { novo = true, email = resultado.Data.Email });
        }

        HttpContext.Session.SetString("UsuarioToken", resultado.Data.Token);
        HttpContext.Session.SetInt32("UsuarioID", resultado.Data.Id);
        HttpContext.Session.SetString("UsuarioNome", resultado.Data.Nome);
        HttpContext.Session.SetString("UsuarioEmail", resultado.Data.Email);

        return RedirectToAction("Dashboard");
    }

    [HttpGet]
    public IActionResult Registrar() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = await _apiClient.RegistrarAsync(model.Nome, model.Email, model.Senha, model.ConfirmarSenha);
        if (!resultado.Success)
        {
            ModelState.AddModelError(string.Empty, resultado.Message);
            return View(model);
        }

        HttpContext.Session.SetString("UsuarioEmail", model.Email);
        return RedirectToAction("VerificarEmail", new { novo = true, email = model.Email, sucesso = resultado.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    private bool TryObterToken(out string token)
    {
        token = HttpContext.Session.GetString("UsuarioToken") ?? string.Empty;
        return !string.IsNullOrWhiteSpace(token);
    }

    private static DashboardStats MontarStats(int totalListas, IReadOnlyCollection<Tarefa> tarefas)
    {
        var stats = new DashboardStats { TotalListas = totalListas, TotalTarefas = tarefas.Count };
        var hoje = DateTime.Today;

        foreach (var tarefa in tarefas)
        {
            if (tarefa.Concluida)
            {
                stats.TarefasConcluidas++;
                continue;
            }

            stats.TarefasPendentes++;
            if (!tarefa.DataVencimento.HasValue)
            {
                continue;
            }

            if (tarefa.DataVencimento.Value.Date < hoje)
            {
                stats.TarefasVencidas++;
            }
            else if (tarefa.DataVencimento.Value.Date == hoje)
            {
                stats.TarefasHoje++;
            }
        }

        return stats;
    }

    private static string PrimeiraMensagemErro(params string[] mensagens)
    {
        return mensagens.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Erro ao comunicar com a API.";
    }
}
