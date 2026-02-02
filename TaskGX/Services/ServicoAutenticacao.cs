using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaskGX.Data;
using TaskGX.Models;
using TaskGX.Tools;

namespace TaskGX.Services
{
    public class ServicoAutenticacao
    {
        private readonly RepositorioUsuario _usuarioRepository;

        public ServicoAutenticacao(RepositorioUsuario usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        // CRIAR CONTA
        public async Task<(bool Sucesso, string Mensagem)> CriarContaAsync(string nome, string email, string senha, string confirmarSenha)
        {
            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(confirmarSenha))
                return (false, "Todos os campos são obrigatórios.");

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return (false, "Email inválido.");

            if (senha != confirmarSenha)
                return (false, "As senhas não coincidem.");

            if (!SenhaValida(senha))
                return (false, "Senha não atende aos requisitos de segurança.");

            if (await _usuarioRepository.ExisteEmailAsync(email))
                return (false, "Email já cadastrado.");

            string senhaHash = AjudaHash.GerarHashSenha(senha);

            Usuarios novoUsuario = new Usuarios
            {
                Nome = nome,
                Email = email,
                Senha = senhaHash,
                Ativo = true,
                EmailVerificado = false,
                CriadoEm = DateTime.Now
            };

            await _usuarioRepository.InserirAsync(novoUsuario);
            return (true, "Conta criada com sucesso!");
        }

        // LOGIN
        public async Task<Usuarios> LoginAsync(string email, string senhaDigitada)
        {
            var usuario = await _usuarioRepository.ObterPorEmailAsync(email);
            if (usuario == null || !usuario.Ativo)
                return null;

            if (!AjudaHash.VerificarSenha(senhaDigitada, usuario.Senha))
                return null;

            return usuario;
        }

        private bool SenhaValida(string senha)
        {
            if (senha.Length < 8)
                return false;
            if (!Regex.IsMatch(senha, "[A-Z]"))
                return false;
            if (!Regex.IsMatch(senha, @"[!@#$%^&*(),.?""':{}|<>]"))
                return false;

            return true;
        }
    }
}
