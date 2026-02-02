using System;

namespace TaskGX.Models
{
    public class Lista
    {
        public int ID { get; set; }
        public int UsuarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public bool? Favorita { get; set; }
        public DateTime? DataCriacao { get; set; }
    }
}
