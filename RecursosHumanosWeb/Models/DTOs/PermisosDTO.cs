namespace RecursosHumanosWeb.Models.DTOs
{
    public class PermisosDTO
    {
        public int Id { get; set; }

        public DateTime Fecha1 { get; set; }

        public DateTime? Fecha2 { get; set; }

        public int? Dias { get; set; }

        public string? Motivo { get; set; }

        public string? Evidencia { get; set; }

        public DateTime? FechaAutorizacion { get; set; }

        public DateTime? FechaCreacion { get; set; }

        public DateTime? FechaModificacion { get; set; }
        public bool Revisado { get; set; }

        public bool Goce { get; set; }

        public bool Estatus { get; set; }

        public int IdCorte { get; set; }

        public int IdTipoPermiso { get; set; }

        public int IdUsuarioSolicita { get; set; }

        public int? IdUsuarioAutoriza { get; set; }

        public int IdUsuarioCrea { get; set; }

        public int IdUsuarioModifica { get; set; }

        public string Nombre { get; set; }

        public string Correo { get; set; }
    }
}
