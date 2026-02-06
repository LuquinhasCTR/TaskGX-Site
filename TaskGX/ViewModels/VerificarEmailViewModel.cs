namespace TaskGX.ViewModels
{
    public class VerificarEmailViewModel
    {
        public bool NovoRegistro { get; set; }
        public bool TrocaSenha { get; set; }
        public string? Erro { get; set; }
        public string? Sucesso { get; set; }
        public string? Email { get; set; }
    }
}
