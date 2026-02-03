using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskGX.Data;
using TaskGX.Models;
using TaskGX.Services;
using TaskGX.Tools;
using TaskGX.ViewModels;
using TaskGX.ViewModels;

namespace TaskGX.Controllers
{
    public class HomeController : Controller
    {
        private readonly ServicoAutenticacao _authService;
        private readonly RepositorioDashboard _dashboardRepositorio;
        private readonly RepositorioUsuario _usuarioRepositorio;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ServicoAutenticacao authService,
            RepositorioDashboard dashboardRepositorio,
            RepositorioUsuario usuarioRepositorio,
            ILogger<HomeController> logger)
        {
            _authService = authService;
            _dashboardRepositorio = dashboardRepositorio;
            _usuarioRepositorio = usuarioRepositorio;
            _logger = logger;
        }

        // =========================
        // Páginas comuns
        // =========================
        public IActionResult Index()
        {
            if (TryObterUsuarioId(out _))
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }
        public IActionResult Termos() => View();
        public IActionResult Privacidade() => View();
        public IActionResult Sobre() => View();
        [HttpGet]
        public async Task<IActionResult> Calendario()
        {
            if (!TryObterUsuarioId(out var usuarioId))
            {
                return RedirectToAction("Login");
            }

            try
            {
                var favoritaExists = await _dashboardRepositorio.ColunaExisteAsync("Listas", "Favorita");
                var listas = await _dashboardRepositorio.ObterListasAsync(usuarioId, favoritaExists);
                var prioridades = await _dashboardRepositorio.ObterPrioridadesAsync();
                var tarefas = await _dashboardRepositorio.ObterTarefasUsuarioAsync(usuarioId);

                var viewModel = new CalendarioViewModel
                {
                    Listas = listas,
                    Prioridades = prioridades,
                    Tarefas = tarefas
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar calendário.");
                TempData["Error"] = "Erro ao carregar calendário.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            if (!TryObterUsuarioId(out var usuarioId))
            {
                return RedirectToAction("Login");
            }

            try
            {
                var usuario = await _usuarioRepositorio.ObterPorIdAsync(usuarioId);
                if (usuario == null)
                {
                    return RedirectToAction("Logout");
                }

                var stats = await _dashboardRepositorio.ObterEstatisticasContaAsync(usuarioId);
                var viewModel = new PerfilViewModel
                {
                    Usuario = usuario,
                    TotalListas = stats.TotalListas,
                    TotalTarefas = stats.TotalTarefas,
                    TarefasConcluidas = stats.TarefasConcluidas,
                    Sucesso = TempData["Success"] as string,
                    Erro = TempData["Error"] as string
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar perfil.");
                TempData["Error"] = "Erro ao carregar dados. Por favor, tente novamente.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarPerfil(string nome, string email)
        {
            if (!TryObterUsuarioId(out var usuarioId))
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Nome e email são obrigatórios.";
                return RedirectToAction("Perfil");
            }

            try
            {
                if (await _usuarioRepositorio.ExisteEmailOutroAsync(usuarioId, email))
                {
                    TempData["Error"] = "Email já cadastrado.";
                    return RedirectToAction("Perfil");
                }

                await _usuarioRepositorio.AtualizarDadosAsync(usuarioId, nome.Trim(), email.Trim());
                HttpContext.Session.SetString("UsuarioNome", nome.Trim());
                TempData["Success"] = "Dados atualizados com sucesso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar perfil.");
                TempData["Error"] = "Erro ao atualizar dados.";
            }

            return RedirectToAction("Perfil");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarSenha(string senha_atual, string nova_senha, string confirmar_nova_senha)
        {
            if (!TryObterUsuarioId(out var usuarioId))
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(senha_atual) || string.IsNullOrWhiteSpace(nova_senha) || string.IsNullOrWhiteSpace(confirmar_nova_senha))
            {
                TempData["Error"] = "Preencha todos os campos de senha.";
                return RedirectToAction("Perfil");
            }

            if (!string.Equals(nova_senha, confirmar_nova_senha, StringComparison.Ordinal))
            {
                TempData["Error"] = "As senhas não coincidem.";
                return RedirectToAction("Perfil");
            }

            if (!SenhaValida(nova_senha))
            {
                TempData["Error"] = "Senha não atende aos requisitos de segurança.";
                return RedirectToAction("Perfil");
            }

            try
            {
                var usuario = await _usuarioRepositorio.ObterPorIdAsync(usuarioId);
                if (usuario == null || !AjudaHash.VerificarSenha(senha_atual, usuario.Senha))
                {
                    TempData["Error"] = "Senha atual inválida.";
                    return RedirectToAction("Perfil");
                }

                var novaHash = AjudaHash.GerarHashSenha(nova_senha);
                await _usuarioRepositorio.AtualizarSenhaAsync(usuarioId, novaHash);
                TempData["Success"] = "Senha atualizada com sucesso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar senha.");
                TempData["Error"] = "Erro ao alterar senha.";
            }

            return RedirectToAction("Perfil");
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(int? listaId, string sucesso = null, string erro = null)
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
                        var tarefas = await _dashboardRepositorio.ObterTarefasPorListaAsync(listaId.Value);

                        viewModel.ListaSelecionada = listaSelecionada;
                        viewModel.Tarefas = tarefas;
                    }
                }
                else
                {
                    viewModel.Tarefas = todasTarefas;
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

        private bool TryObterUsuarioId(out int usuarioId)
        {
            var usuarioIdValue = HttpContext.Session.GetString("UsuarioID");
            return int.TryParse(usuarioIdValue, out usuarioId);
        }

        private static bool SenhaValida(string senha)
        {
            if (senha.Length < 8)
            {
                return false;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(senha, "[A-Z]"))
            {
                return false;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(senha, @"[!@#$%^&*(),.?""':{}|<>]"))
            {
                return false;
            }

            return true;
        }
    }
}
