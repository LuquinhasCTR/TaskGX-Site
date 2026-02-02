using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskGX.Data;
using TaskGX.Models;
using TaskGX.Services;
using TaskGX.ViewModels;

namespace TaskGX.Controllers
{
    public class HomeController : Controller
    {
        private readonly ServicoAutenticacao _authService;
        private readonly RepositorioDashboard _dashboardRepositorio;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ServicoAutenticacao authService, RepositorioDashboard dashboardRepositorio, ILogger<HomeController> logger)
        {
            _authService = authService;
            _dashboardRepositorio = dashboardRepositorio;
            _logger = logger;
        }

        // =========================
        // Páginas comuns
        // =========================
        public IActionResult Index() => View();
        public IActionResult Termos() => View();
        public IActionResult Privacidade() => View();

        [HttpGet]
        public async Task<IActionResult> Dashboard(int? listaId, bool mostrarArquivadas = false, string sucesso = null, string erro = null)
        {
            var usuarioIdValue = HttpContext.Session.GetString("UsuarioID");
            if (!int.TryParse(usuarioIdValue, out var usuarioId))
            {
                return RedirectToAction("Login");
            }

            var usuarioNome = HttpContext.Session.GetString("UsuarioNome") ?? "Usuário";

            var viewModel = new DashboardViewModel
            {
                UsuarioId = usuarioId,
                UsuarioNome = usuarioNome,
                ListaId = listaId ?? 0,
                Sucesso = !string.IsNullOrWhiteSpace(sucesso) ? sucesso : TempData["Success"] as string,
                Erro = !string.IsNullOrWhiteSpace(erro) ? erro : TempData["Error"] as string
            };

            try
            {
                var favoritaExists = await _dashboardRepositorio.ColunaExisteAsync("Listas", "Favorita");
                var listas = await _dashboardRepositorio.ObterListasAsync(usuarioId, favoritaExists);
                var prioridades = await _dashboardRepositorio.ObterPrioridadesAsync();
                var todasTarefas = await _dashboardRepositorio.ObterTarefasUsuarioAsync(usuarioId);

                var stats = new DashboardStats
                {
                    TotalListas = listas.Count
                };

                var hoje = DateTime.Today;
                foreach (var tarefa in todasTarefas)
                {
                    stats.TotalTarefas++;
                    if (tarefa.Concluida)
                    {
                        stats.TarefasConcluidas++;
                    }
                    else
                    {
                        stats.TarefasPendentes++;
                        if (tarefa.DataVencimento.HasValue)
                        {
                            if (tarefa.DataVencimento.Value.Date < hoje)
                            {
                                stats.TarefasVencidas++;
                            }
                            else if (tarefa.DataVencimento.Value.Date == hoje)
                            {
                                stats.TarefasHoje++;
                            }
                        }
                    }
                }

                viewModel.Listas = listas;
                viewModel.Prioridades = prioridades;
                viewModel.Stats = stats;

                if (listaId.HasValue && listaId.Value > 0)
                {
                    var listaSelecionada = await _dashboardRepositorio.ObterListaAsync(listaId.Value, usuarioId, favoritaExists);
                    if (listaSelecionada != null)
                    {
                        var arquivadaExists = await _dashboardRepositorio.ColunaExisteAsync("Tarefas", "Arquivada");
                        var mostrarArquivadasEfetivo = mostrarArquivadas && arquivadaExists;
                        var tarefas = await _dashboardRepositorio.ObterTarefasPorListaAsync(listaId.Value, arquivadaExists, mostrarArquivadasEfetivo);

                        viewModel.ListaSelecionada = listaSelecionada;
                        viewModel.Tarefas = tarefas;
                        viewModel.MostrarArquivadas = mostrarArquivadasEfetivo;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dashboard.");
                viewModel.Erro = "Erro ao carregar dados. Por favor, tente novamente.";
                viewModel.Listas = new List<Lista>();
                viewModel.Prioridades = new List<Prioridade>();
                viewModel.Tarefas = new List<Tarefa>();
                viewModel.Stats = new DashboardStats();
            }

            return View(viewModel);
        }

        // =========================
        // LOGIN
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string senha)
        {
            var usuario = await _authService.LoginAsync(email, senha);

            if (usuario == null)
            {
                TempData["Error"] = "Email ou senha incorretos.";
                return View();
            }

            // Criar sessão
            HttpContext.Session.SetString("UsuarioID", usuario.ID.ToString());
            HttpContext.Session.SetString("UsuarioNome", usuario.Nome);

            return RedirectToAction("Dashboard");
        }

        // =========================
        // REGISTRAR
        // =========================
        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Chama o serviço de autenticação
            var resultado = await _authService.CriarContaAsync(
                model.Nome,
                model.Email,
                model.Senha,
                model.ConfirmarSenha
            );

            if (!resultado.Sucesso)
            {
                ModelState.AddModelError(string.Empty, resultado.Mensagem);
                return View(model);
            }

            TempData["Success"] = resultado.Mensagem;
            return RedirectToAction("Login");
        }

        // =========================
        // LOGOUT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
