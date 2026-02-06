namespace TaskGX.ViewModels
{
    public class EmailTemplateViewModel
    {
        public string Nome { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string AppName { get; set; } = "TaskGX";
        public bool TrocaSenha { get; set; }
        public int ExpiracaoHoras { get; set; }
    }
}
