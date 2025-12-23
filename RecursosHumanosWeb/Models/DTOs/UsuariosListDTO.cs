namespace RecursosHumanosWeb.Models.DTOs
{
    public class UsuariosListDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Departamento { get; set; } = string.Empty;
    }
}
