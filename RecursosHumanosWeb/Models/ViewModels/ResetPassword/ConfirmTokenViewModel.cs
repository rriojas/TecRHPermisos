using System.ComponentModel.DataAnnotations;

namespace RecursosHumanosWeb.Models.ViewModels.ResetPassword
{
    public class ConfirmTokenViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20, MinimumLength = 6)]
        public string Token { get; set; } = string.Empty;
    }
}
