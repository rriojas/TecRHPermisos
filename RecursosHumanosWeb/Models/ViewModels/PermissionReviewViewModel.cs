using System.ComponentModel.DataAnnotations;

namespace RecursosHumanosWeb.Models.ViewModels
{
    public class PermissionReviewViewModel
    {
        // Identificador del permiso
        public int Id { get; set; }

        // Información del solicitante (solo lectura)
        public string NombreSolicitante { get; set; } = string.Empty;
        public string CorreoSolicitante { get; set; } = string.Empty;
        public string AreaSolicitante { get; set; } = string.Empty;

        // Información del permiso (solo lectura)
        public int IdTipoPermiso { get; set; }
        public string TipoPermisoDescripcion { get; set; } = string.Empty;
        public DateTime? Fecha1 { get; set; }
        public DateTime? Fecha2 { get; set; }
        public int? Dias { get; set; }
        public string? Motivo { get; set; }
        public string? Evidencia { get; set; }

        // Campo EDITABLE: Goce de sueldo
        [Required(ErrorMessage = "Debe indicar si el permiso es con o sin goce de sueldo.")]
        public bool Goce { get; set; }

        // Información adicional (solo lectura)
        public DateTime FechaCreacion { get; set; }
        public string? CreadoPor { get; set; }
        public int IdCorte { get; set; }

        // Información del corte (solo lectura)
        public DateTime? CorteInicia { get; set; }
        public DateTime? CorteTermina { get; set; }
    }
}
