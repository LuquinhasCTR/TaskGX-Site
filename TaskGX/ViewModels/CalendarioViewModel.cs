using System.Collections.Generic;
using TaskGX.Models;

namespace TaskGX.ViewModels
{
    public class CalendarioViewModel
    {
        public IReadOnlyList<Lista> Listas { get; set; } = new List<Lista>();
        public IReadOnlyList<Prioridade> Prioridades { get; set; } = new List<Prioridade>();
        public IReadOnlyList<Tarefa> Tarefas { get; set; } = new List<Tarefa>();
    }
}
