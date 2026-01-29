using System;

namespace TaskGX.Models
{
    /// <summary>
    /// Dados do usuário
    /// </summary>
    public class Usuarios
    {
        // Tornar set público para que o repositório possa popular o modelo a partir da BD
        public int ID { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public string Avatar { get; set; }
        public bool Ativo { get; set; }
        public bool EmailVerificado { get; set; }
        public string CodigoVerificacao { get; set; }
        public DateTime? CriadoEm { get; set; }
        public DateTime? DataAtualizacao { get; set; }
        public DateTime? CodigoVerificacaoExpiracao { get; set; }

        public Usuarios(string nome, string email, string senha)
        {
            Nome = nome;
            Email = email;
            Senha = senha;

            Ativo = true;
            EmailVerificado = false;
            CriadoEm = DateTime.Now;
        }

        public Usuarios() { }
    }
}