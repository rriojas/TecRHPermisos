using System.ComponentModel.DataAnnotations;

namespace RecursosHumanosWeb.Models.ViewModels.ResetPassword
{
    public class RequestTokenViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
