using System;

namespace RecursosHumanosWeb.Models
{
    public class ResetPasswordToken
    {
        public int Id { get; set; }
        public string Selector { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public bool Utilizado { get; set; }
        public int IdUsuarioSolicita { get; set; }
        public int? IdUsuarioCrea { get; set; }
        public int? IdUsuarioModifica { get; set; }
        public bool Estatus { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaCaducidad { get; set; }
    }
}
