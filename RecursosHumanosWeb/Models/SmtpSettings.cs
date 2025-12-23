namespace RecursosHumanosWeb.Models
{
    public class SmtpSettings
    {
        public string EmailSender { get; set; } = string.Empty;
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string PasswordApp { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}