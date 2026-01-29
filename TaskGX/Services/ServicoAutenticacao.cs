using System;
using System.Security.Cryptography;
using System.Text;
using TaskGX.Dados;
using TaskGX.Model;
using System.Text.RegularExpressions;

using TaskGX.Ferramentas;

namespace TaskGX.Servicos
{
    public class ServicoAutenticacao
    {
        private readonly RepositorioUsuario _usuarioRepository;

        public ServicoAutenticacao()
        {
            _usuarioRepository = new RepositorioUsuario();
        }

        // =========================
        // CRIAR CONTA
        // =========================
        public bool CriarConta(string nome, string email, string senha, string confirmarSenha)
        {
            // Campos obrigatórios
            if (string.IsNullOrWhiteSpace(nome) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(senha) ||
                string.IsNullOrWhiteSpace(confirmarSenha))
                return false;

            // Email simples
            if (!email.Contains("@"))
                return false;

            // Senhas iguais
            if (senha != confirmarSenha)
                return false;

            // Senha forte
            if (!SenhaValida(senha))
                return false;

            // Verificar se já existe
            if (_usuarioRepository.ExisteEmail(email))
                return false;

            // 🔐 Gerar hash seguro
            string senhaHash = AjudaHash.GerarHashSenha(senha);

            Usuarios novoUsuario = new Usuarios
            {
                Nome = nome,
                Email = email,
                Senha = senhaHash,
                Ativo = true
            };

            _usuarioRepository.Inserir(novoUsuario);
            return true;
        }

        // =========================
        // LOGIN
        // =========================
        public bool Login(string email, string senhaDigitada)
        {
            Usuarios usuario = _usuarioRepository.ObterPorEmail(email);

            if (usuario == null)
                return false;

            if (!usuario.Ativo)
                return false;

            // 🔐 Verificar senha com BCrypt
            return AjudaHash.VerificarSenha(
                senhaDigitada,
                usuario.Senha
            );
        }

        // =========================
        // VALIDAÇÃO DE SENHA
        // =========================
        private bool SenhaValida(string senha)
        {
            if (senha.Length < 8)
                return false;

            // Pelo menos uma letra maiúscula
            if (!Regex.IsMatch(senha, "[A-Z]"))
                return false;

            // Pelo menos um caractere especial
            if (!Regex.IsMatch(senha, @"[!@#$%^&*(),.?""':{}|<>]"))
                return false;

            return true;
        }
    }
}

