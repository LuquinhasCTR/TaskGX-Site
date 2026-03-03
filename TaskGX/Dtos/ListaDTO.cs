namespace TaskGX.ApiModels
{
    public class ListaDTO
    {
        public int ID { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? Cor { get; set; }

        public bool Favorita { get; set; }
    }
}
