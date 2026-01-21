using System;

namespace RecursosHumanosWeb.Models.ViewModels.Usuario
{
    public class UsuarioDetailsViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public bool Estatus { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        public int IdTipoUsuario { get; set; }
        public string TipoUsuarioNombre { get; set; } = string.Empty;

        public int IdArea { get; set; }
        public string AreaDescripcion { get; set; } = string.Empty;

        public int IdDepartamento { get; set; }
        public string DepartamentoDescripcion { get; set; } = string.Empty;

        public int IdPuesto { get; set; }
        public string PuestoDescripcion { get; set; } = string.Empty;

        public int? IdUsuarioCrea { get; set; }
        public string? UsuarioCreaNombre { get; set; } // Permitir nulo

        public int? IdUsuarioModifica { get; set; }
        public string? UsuarioModificaNombre { get; set; } // Permitir nulo

        public int? IdApiKey { get; set; }
    }
}