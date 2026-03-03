using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskGX.ApiModels;
using TaskGX.Controllers.ViewModels;
using TaskGX.Web.Services;

namespace TaskGX.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiClient _api;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApiClient api, ILogger<HomeController> logger)
        {
            _api = api;
            _logger = logger;
        }

        // =========================
        // Páginas comuns
        // =========================
        public IActionResult Index()
        {
            if (IsLogado()) return RedirectToAction(nameof(Dashboard));
            return View();
        }

        public IActionResult Termos() => View();
        public IActionResult Privacidade() => View();
        public IActionResult Sobre() => View();

        // =========================
        // LOGIN
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            if (IsLogado()) return RedirectToAction(nameof(Dashboard));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string senha)
        {
            try
            {
                var resp = await _api.PostAsync<LoginResponse>(
                    "/api/auth/login",
                    new { email, senha },
                    auth: false
                );

                if (resp == null || string.IsNullOrWhiteSpace(resp.Token))
                {
                    TempData["Error"] = "Login falhou.";
                    return View();
                }

                _api.SetToken(resp.Token);
                HttpContext.Session.SetString("UsuarioID", resp.Usuario.ID.ToString());
                HttpContext.Session.SetString("UsuarioNome", resp.Usuario.Nome ?? "Usuário");
                HttpContext.Session.SetString("UsuarioEmail", resp.Usuario.Email ?? "");

                return RedirectToAction(nameof(Dashboard));
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "Falha no login");
                TempData["Error"] = "Email ou senha incorretos (ou usuário não autorizado).";
                return View();
            }
        }

        // =========================
        // REGISTRAR
        // =========================
        [HttpGet]
        public IActionResult Registrar()
        {
            if (IsLogado()) return RedirectToAction(nameof(Dashboard));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dados inválidos. Verifique os campos.";
                return View(model);
            }

            try
            {
                await _api.PostAsync<object>(
                    "/api/registration/register",
                    new { nome = model.Nome, email = model.Email, senha = model.Senha },
                    auth: false
                );

                TempData["Success"] = "Conta criada. Agora faça login.";
                return RedirectToAction(nameof(Login));
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "Falha ao registrar");
                TempData["Error"] = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Não foi possível criar a conta. Verifique os dados (ou email já existe)."
                    : ex.Message;

                return View(model);
            }
        }

        // =========================
        // LOGOUT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            _api.ClearToken();
            return RedirectToAction(nameof(Login));
        }

        // =========================
        // DASHBOARD (via API)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Dashboard(int? listaId, string? sucesso = null, string? erro = null)
        {
            if (!IsLogado()) return RedirectToAction(nameof(Login));

            var usuarioNome = HttpContext.Session.GetString("UsuarioNome") ?? "Usuário";

            var viewModel = new DashboardViewModel
            {
                UsuarioNome = usuarioNome,
                ListaId = listaId ?? 0,
                Sucesso = !string.IsNullOrWhiteSpace(sucesso) ? sucesso : TempData["Success"] as string,
                Erro = !string.IsNullOrWhiteSpace(erro) ? erro : TempData["Error"] as string,

                // ✅ agora é DTO
                Listas = new List<ListaDTO>(),
                Prioridades = new List<PrioridadeDTO>(),
                Tarefas = new List<TarefaDTO>(),
                ListaSelecionada = null,

                Stats = new DashboardStats()
            };

            try
            {
                var prioridades = await _api.GetAsync<List<PrioridadeDTO>>("/api/prioridades", auth: true) ?? new();
                var listas = await _api.GetAsync<List<ListaDTO>>("/api/listas", auth: true) ?? new();

                // Se não vier listaId, tenta usar a primeira lista
                var effectiveListaId = (listaId.HasValue && listaId.Value > 0)
                    ? listaId.Value
                    : (listas.FirstOrDefault()?.ID ?? 0);

                var listaSelecionada = listas.FirstOrDefault(l => l.ID == effectiveListaId);

                var tarefas = (effectiveListaId > 0)
                    ? await _api.GetAsync<List<TarefaDTO>>($"/api/tarefas?listaId={effectiveListaId}", auth: true) ?? new()
                    : new List<TarefaDTO>();

                viewModel.Listas = listas;
                viewModel.Prioridades = prioridades;
                viewModel.Tarefas = tarefas;
                viewModel.ListaSelecionada = listaSelecionada;
                viewModel.ListaId = effectiveListaId;

                // Stats
                viewModel.Stats.TotalListas = listas.Count;
                viewModel.Stats.TotalTarefas = tarefas.Count;
                viewModel.Stats.TarefasConcluidas = tarefas.Count(t => t.Concluida);
                viewModel.Stats.TarefasPendentes = tarefas.Count(t => !t.Concluida);

                var hoje = DateTime.Today;
                viewModel.Stats.TarefasVencidas = tarefas.Count(t => !t.Concluida && t.DataVencimento.HasValue && t.DataVencimento.Value.Date < hoje);
                viewModel.Stats.TarefasHoje = tarefas.Count(t => !t.Concluida && t.DataVencimento.HasValue && t.DataVencimento.Value.Date == hoje);

                return View(viewModel);
            }
            catch (ApiUnauthorizedException)
            {
                HttpContext.Session.Clear();
                _api.ClearToken();
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dashboard via API.");
                viewModel.Erro = "Erro ao carregar dados. Por favor, tente novamente.";
                return View(viewModel);
            }
        }

        // =========================
        // Helpers
        // =========================
        private bool IsLogado()
        {
            var token = _api.GetToken();
            return !string.IsNullOrWhiteSpace(token);
        }

        // =========================
        // DTO de resposta do login
        // =========================
        private class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public UsuarioDTO Usuario { get; set; } = new();
        }
    }
}
