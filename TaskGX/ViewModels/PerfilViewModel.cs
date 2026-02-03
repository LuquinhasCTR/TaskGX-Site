using TaskGX.Models;

namespace TaskGX.ViewModels
{
    public class PerfilViewModel
    {
        public Usuarios Usuario { get; set; } = new Usuarios();
        public int TotalListas { get; set; }
        public int TotalTarefas { get; set; }
        public int TarefasConcluidas { get; set; }
        public string? Sucesso { get; set; }
        public string? Erro { get; set; }
    }
}
