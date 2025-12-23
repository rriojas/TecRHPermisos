using System.ComponentModel.DataAnnotations;

namespace RecursosHumanosWeb.Models.ViewModels.ResetPassword
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [StringLength(12, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 12 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva Contraseña")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Contraseña")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Selector + verifier token parts
        [Required]
        public string Selector { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
