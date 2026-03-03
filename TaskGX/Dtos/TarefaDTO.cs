using System;

namespace TaskGX.ApiModels
{
    public class TarefaDTO
    {
        public int ID { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        public string? Tags { get; set; }

        public bool Concluida { get; set; }

        public bool Arquivada { get; set; }

        public DateTime? DataVencimento { get; set; }

        public DateTime DataCriacao { get; set; }

        public int ListaId { get; set; }

        public string? ListaNome { get; set; }

        public int? PrioridadeId { get; set; }

        public string? PrioridadeNome { get; set; }
    }
}
