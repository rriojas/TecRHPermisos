namespace RecursosHumanosWeb.Models.DTOs
{
    public class UsuariosDTO
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = null!;

        public string Correo { get; set; } = null!;

        public string Clave { get; set; } = null!;

        public DateTime FechaCreacion { get; set; }

        public DateTime FechaModificacion { get; set; }

        public bool Estatus { get; set; }

        public int IdTipoUsuario { get; set; }

        public int IdArea { get; set; }

        public int IdDepartamento { get; set; }

        public int IdPuesto { get; set; }

        public int IdUsuarioCrea { get; set; }

        public int IdUsuarioModifica { get; set; }

        public int? IdApiKey { get; set; }
    }
}
