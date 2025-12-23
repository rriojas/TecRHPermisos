namespace RecursosHumanosWeb.Models.DTOs
{
    public class CorteDetailsDTO
    {
        // Información del corte
        public int Id { get; set; }
        public DateTime? Inicia { get; set; }
        public DateTime? Termina { get; set; }
        public bool Estatus { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        // Información de usuarios
        public string? UsuarioCrea { get; set; }
        public string? UsuarioModifica { get; set; }

        // Lista completa de permisos con todos sus datos
        public List<PermisosDTO> Permisos { get; set; } = new List<PermisosDTO>();

        // KPIs calculados
        public int TotalPermisos { get; set; }
        public int PermisosAprobados { get; set; }
        public int PermisosPendientes { get; set; }
        public double DiasPromedio { get; set; }

        // Datos para gráficos (serializados como JSON en el controlador)
        public string PermisosPorTipoJson { get; set; } = "[]";
        public string PermisosPorAreaJson { get; set; } = "[]";
        public string TendenciaJson { get; set; } = "[]";
        public string HeatmapDataJson { get; set; } = "[]";
        public string KpiDataJson { get; set; } = "{}";
        public int ConGoce { get; set; }
        public int SinGoce { get; set; }
    }
}
