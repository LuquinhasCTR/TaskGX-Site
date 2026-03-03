using TaskGX.ApiModels;
using TaskGX.Controllers.ViewModels;

public class DashboardViewModel
{
    public string? Sucesso { get; set; }
    public string? Erro { get; set; }

    public DashboardStats Stats { get; set; } = new();

    public List<ListaDTO> Listas { get; set; } = new();
    public List<PrioridadeDTO> Prioridades { get; set; } = new();
    public List<TarefaDTO> Tarefas { get; set; } = new();

    public ListaDTO? ListaSelecionada { get; set; }

    public int ListaId { get; set; }
    public string? UsuarioNome { get; set; }
}
