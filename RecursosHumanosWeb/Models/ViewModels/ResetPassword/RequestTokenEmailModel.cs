namespace RecursosHumanosWeb.Models.ViewModels.ResetPassword
{
    // ViewModel used to format email content for token notification
    public class RequestTokenEmailModel
    {
        public string Email { get; set; } = string.Empty;
        public string Selector { get; set; } = string.Empty;
        public string Verifier { get; set; } = string.Empty; // the raw token part sent to user
        public string Link { get; set; } = string.Empty;
        public int ExpirationMinutes { get; set; }
    }
}
