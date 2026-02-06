using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskGX.Data;
using TaskGX.Models;
using TaskGX.Tools;

namespace TaskGX.Services
{
    public class ServicoAutenticacao
    {
        private readonly RepositorioUsuario _usuarioRepository;
        private readonly EmailSender _emailSender;
        private readonly ILogger<ServicoAutenticacao> _logger;

        public ServicoAutenticacao(
            RepositorioUsuario usuarioRepository,
            EmailSender emailSender,
            ILogger<ServicoAutenticacao> logger)
        {
            _usuarioRepository = usuarioRepository;
            _emailSender = emailSender;
            _logger = logger;
        }

        // =========================
        // CRIAR CONTA
        // =========================
        public async Task<(bool Sucesso, string Mensagem)> CriarContaAsync(
            string nome,
            string email,
            string senha,
            string confirmarSenha)
        {
            if (string.IsNullOrWhiteSpace(nome) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(senha) ||
                string.IsNullOrWhiteSpace(confirmarSenha))
            {
                return (false, "Todos os campos são obrigatórios.");
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return (false, "Email inválido.");
            }

            if (senha != confirmarSenha)
            {
                return (false, "As senhas não coincidem.");
            }

            if (!SenhaValida(senha))
            {
                return (false, "A senha não atende aos requisitos de segurança.");
            }

            if (await _usuarioRepository.ExisteEmailAsync(email))
            {
                return (false, "Email já cadastrado.");
            }

            var codigoVerificacao = GerarCodigoVerificacao();

            var novoUsuario = new Usuarios
            {
                Nome = nome,
                Email = email,
                Senha = AjudaHash.GerarHashSenha(senha),
                Ativo = false,
                EmailVerificado = false,
                CodigoVerificacao = codigoVerificacao,
                CodigoVerificacaoExpiracao = DateTime.UtcNow.AddHours(24),
                CriadoEm = DateTime.UtcNow
            };

            await _usuarioRepository.InserirAsync(novoUsuario);

            try
            {
                await _emailSender.EnviarEmailVerificacaoAsync(
                    email,
                    nome,
                    codigoVerificacao,
                    trocaSenha: false
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao enviar email de verificação para {Email}",
                    email
                );

                return (false, "Conta criada, mas não foi possível enviar o email de verificação.");
            }

            return (true, "Conta criada com sucesso! Enviamos um código de verificação para seu email.");
        }

        // =========================
        // VERIFICAR EMAIL
        // =========================
        public async Task<(bool Sucesso, string Mensagem)> VerificarEmailAsync(
            string email,
            string codigo)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(codigo))
            {
                return (false, "Email e código são obrigatórios.");
            }

            var usuario = await _usuarioRepository.ObterPorEmailAsync(email);
            if (usuario == null)
            {
                return (false, "Email não encontrado.");
            }

            if (usuario.EmailVerificado)
            {
                return (true, "Email já verificado.");
            }

            if (!string.Equals(
                    usuario.CodigoVerificacao,
                    codigo,
                    StringComparison.Ordinal))
            {
                return (false, "Código inválido.");
            }

            if (usuario.CodigoVerificacaoExpiracao.HasValue &&
                usuario.CodigoVerificacaoExpiracao.Value < DateTime.UtcNow)
            {
                return (false, "Código expirado. Solicite um novo.");
            }

            await _usuarioRepository.AtualizarVerificacaoEmailAsync(
                usuario.ID,
                ativo: true,
                emailVerificado: true,
                codigoVerificacao: null,
                expiracao: null
            );

            return (true, "Email verificado com sucesso.");
        }

        // =========================
        // REENVIAR CÓDIGO
        // =========================
        public async Task<(bool Sucesso, string Mensagem)> ReenviarCodigoAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Email é obrigatório.");
            }

            var usuario = await _usuarioRepository.ObterPorEmailAsync(email);
            if (usuario == null)
            {
                return (false, "Email não encontrado.");
            }

            var codigoVerificacao = GerarCodigoVerificacao();

            await _usuarioRepository.AtualizarVerificacaoEmailAsync(
                usuario.ID,
                ativo: false,
                emailVerificado: false,
                codigoVerificacao: codigoVerificacao,
                expiracao: DateTime.UtcNow.AddHours(24)
            );

            try
            {
                await _emailSender.EnviarEmailVerificacaoAsync(
                    usuario.Email,
                    usuario.Nome,
                    codigoVerificacao,
                    trocaSenha: false
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao reenviar código de verificação para {Email}",
                    email
                );

                return (false, "Não foi possível reenviar o código no momento.");
            }

            return (true, "Enviamos um novo código para seu email.");
        }

        // =========================
        // LOGIN
        // =========================
        public async Task<Usuarios> LoginAsync(string email, string senhaDigitada)
        {
            var usuario = await _usuarioRepository.ObterPorEmailAsync(email);
            if (usuario == null)
            {
                return null;
            }

            if (!AjudaHash.VerificarSenha(senhaDigitada, usuario.Senha))
            {
                return null;
            }

            // 🔴 Correção de lógica
            if (!usuario.EmailVerificado || !usuario.Ativo)
            {
                return null;
            }

            return usuario;
        }

        // =========================
        // AUXILIARES
        // =========================
        private static string GerarCodigoVerificacao()
        {
            var codigo = System.Security.Cryptography.RandomNumberGenerator
                .GetInt32(0, 1_000_000);

            return codigo.ToString("D6");
        }

        private static bool SenhaValida(string senha)
        {
            if (senha.Length < 8)
                return false;

            if (!Regex.IsMatch(senha, "[A-Z]"))
                return false;

            if (!Regex.IsMatch(senha, "[a-z]"))
                return false;

            if (!Regex.IsMatch(senha, "[0-9]"))
                return false;

            if (!Regex.IsMatch(senha, @"[!@#$%^&*(),.?""':{}|<>_]"))
                return false;

            return true;
        }
    }
}
