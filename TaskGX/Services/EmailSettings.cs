namespace TaskGX.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = "taskgxsuporte@gmail.com";
        public string Password { get; set; } = "euwupqgnlgptzoxz";
        public string FromEmail { get; set; } = "taskGXsuporte@gmail.com";
        public string FromName { get; set; } = "TaskGX";
        public string AppName { get; set; } = "TaskGX";
        public bool EnableSsl { get; set; } = true;
    }
}
