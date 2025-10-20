using System.ComponentModel.DataAnnotations;

namespace RecursosHumanosWeb.Models.ViewModels
{
    public class PermissionsCreateViewModel
    {
        // Identificador único del permiso (opcional, solo para edición)
        public int? Id { get; set; }

        // El motivo es siempre obligatorio y tiene un límite de longitud
        [Required(ErrorMessage = "El motivo es obligatorio.")]
        [StringLength(255, ErrorMessage = "El motivo no puede exceder los 255 caracteres.")]
        public string Motivo { get; set; }

        // Fecha1: Almacena la fecha y hora combinadas (inicio o evento principal)
        [DataType(DataType.DateTime)]
        public DateTime? Fecha1 { get; set; }

        // Fecha2: Almacena la fecha y hora combinadas (fin o evento secundario, si aplica)
        [DataType(DataType.DateTime)]
        public DateTime? Fecha2 { get; set; }

        // El tipo de permiso es siempre obligatorio
        [Required(ErrorMessage = "El tipo de permiso es obligatorio.")]
        public int IdTipoPermiso { get; set; }

        // Ruta de la evidencia (string): Puede ser nula, longitud limitada
        [StringLength(255, ErrorMessage = "La evidencia no puede exceder los 255 caracteres.")]
        public string? Evidencia { get; set; }

        // Archivo de evidencia (IFormFile): Es opcional, por lo que ya es nullable
        public IFormFile? EvidenceFile { get; set; }

        // Goce de sueldo (opcional, editable solo por Administrador/Autorizador en edición)
        public bool? Goce { get; set; }

        // Campos temporales para la UI (no se guardan en la base de datos)
        [DataType(DataType.Date)]
        public DateTime? Fecha1Date { get; set; }

        // Cambiar TimeSpan a string para compatibilidad con input type="time"
        [DataType(DataType.Time)]
        public string? Hora1Time { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Fecha2Date { get; set; }

        [DataType(DataType.Time)]
        public string? Hora2Time { get; set; }

        // Usuario que solicita (opcional, prellenado en creación, de solo lectura en edición)
        public int? IdUsuarioSolicita { get; set; }

        // Usuario que crea (opcional, prellenado en creación)
        public int? IdUsuarioCrea { get; set; }
    }
}
