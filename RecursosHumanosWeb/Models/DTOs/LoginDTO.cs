using Microsoft.EntityFrameworkCore;

namespace RecursosHumanosWeb.Models.DTOs
{
    [Keyless]
    public class LoginDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public int IdArea { get; set; }
        public string NombreArea { get; set; }
        public int IdTipoUsuario { get; set; }
    }
}
