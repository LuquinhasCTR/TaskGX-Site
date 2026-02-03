using System.Collections.Generic;
using TaskGX.Models;

namespace TaskGX.ViewModels
{
    public class DashboardViewModel
    {
        public string? Sucesso { get; set; }
        public string? Erro { get; set; }
        public int UsuarioId { get; set; }
        public string? UsuarioNome { get; set; }
        public IReadOnlyList<Lista> Listas { get; set; } = new List<Lista>();
        public IReadOnlyList<Prioridade> Prioridades { get; set; } = new List<Prioridade>();
        public IReadOnlyList<Tarefa> Tarefas { get; set; } = new List<Tarefa>();
        public Lista? ListaSelecionada { get; set; }
        public int ListaId { get; set; }
        public DashboardStats Stats { get; set; } = new DashboardStats();
    }

    public class DashboardStats
    {
        public int TotalListas { get; set; }
        public int TotalTarefas { get; set; }
        public int TarefasConcluidas { get; set; }
        public int TarefasPendentes { get; set; }
        public int TarefasVencidas { get; set; }
        public int TarefasHoje { get; set; }
    }
}
