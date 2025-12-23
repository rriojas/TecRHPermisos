using System.ComponentModel.DataAnnotations;

namespace RecursosHumanosWeb.Models.ViewModels.Usuario
{
    // Define el modelo de datos que se espera recibir en el cuerpo (Body) de la petición POST.
    public class UsuarioCreateViewModel
    {
        // --- Propiedades requeridas para la creación ---

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no debe exceder los 100 caracteres.")]
        [Display(Name = "Nombre Completo")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        [StringLength(150, ErrorMessage = "El correo no debe exceder los 150 caracteres.")]
        [Display(Name = "Correo Electrónico")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 50 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Clave { get; set; } = string.Empty;

        // --- Claves foráneas necesarias (IDs) ---

        [Display(Name = "Tipo de Usuario")]
        [Required(ErrorMessage = "El Tipo de Usuario es obligatorio.")]
        public int IdTipoUsuario { get; set; }

        [Display(Name = "Departamento")]
        [Required(ErrorMessage = "El Departamento es obligatorio.")]
        public int IdDepartamento { get; set; }

        [Display(Name = "Área")]
        [Required(ErrorMessage = "El Área es obligatoria.")]
        public int IdArea { get; set; }

        [Display(Name = "Puesto")]
        [Required(ErrorMessage = "El Puesto es obligatorio.")]
        public int IdPuesto { get; set; }

        // Nota: IdApiKey se elimina completamente de aquí.
        // Las API Keys se administran a través del controlador ApiKeysController
        // después de que el usuario ha sido creado.

        // Nota: Los campos de auditoría (FechaCreacion, IdUsuarioCrea, Estatus) 
        // son manejados por el controlador y no se incluyen aquí.
    }
}