using System.ComponentModel.DataAnnotations;

namespace RecursosHumanosWeb.Models.ViewModels.Usuario
{
    // Define el modelo de datos que se espera recibir en el cuerpo (Body) de la petición PUT (o PATCH).
    public class UsuarioUpdateViewModel
    {
        // --- 1. ID (Clave Primaria) ---

        [Required(ErrorMessage = "El ID del usuario es obligatorio para la actualización.")]
        public int IdUsuario { get; set; }

        // --- 2. Propiedades a modificar ---

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no debe exceder los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        [StringLength(150, ErrorMessage = "El correo no debe exceder los 150 caracteres.")]
        public string Correo { get; set; } = string.Empty;

        // La clave es opcional en la actualización. Si se envía, se actualiza; si no, se ignora.
        // Validación: entre 6 y 12 caracteres
        [StringLength(12, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 12 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva Contraseña")]
        public string? Clave { get; set; }

        // --- 3. Claves foráneas (IDs) ---

        [Required(ErrorMessage = "El Tipo de Usuario es obligatorio.")]
        [Display(Name = "Tipo de Usuario")]
        public int IdTipoUsuario { get; set; }

        [Display(Name = "Departamento")]
        public int IdDepartamento { get; set; }

        [Display(Name = "Área")]
        public int IdArea { get; set; }

        [Display(Name = "Puesto")]
        public int IdPuesto { get; set; }

        // --- 4. Estatus (Para activación/desactivación) ---

        [Required(ErrorMessage = "El estatus es obligatorio.")]
        public bool Estatus { get; set; }

        // Nota: Los campos de auditoría de modificación (FechaModificacion, IdUsuarioModifica) 
        // son manejados por el controlador y no se incluyen aquí.
        // IdApiKey NO es editable desde este formulario (se gestiona en ApiKeysController)
    }
}