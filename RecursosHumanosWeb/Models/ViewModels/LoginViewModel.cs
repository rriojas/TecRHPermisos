using System.ComponentModel.DataAnnotations;

namespace RecursosHumanosWeb.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [RegularExpression(@"^[^@\s]+@monclova\.tecnm\.mx$",
         ErrorMessage = "El correo debe pertenecer al dominio monclova.tecnm.mx")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Clave { get; set; }

        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
