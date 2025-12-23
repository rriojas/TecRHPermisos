using RecursosHumanosWeb.Models.DTOs;
using System.ComponentModel.DataAnnotations;
using X.PagedList;
using System.Collections.Generic;

namespace RecursosHumanosWeb.Models.ViewModels
{
    public class SearchPermissionsViewModel
    {
        public IPagedList<PermisosDTO> Permisos { get; set; }
        public List<UsuariosDTO> Usuarios { get; set; } = new List<UsuariosDTO>();
        public List<AreasDTO> Areas { get; set; } = new List<AreasDTO>();
        public string SelectedUser { get; set; }
        public int? SelectedArea { get; set; }
        public bool Revisado { get; set; } = false;
        public DateTime? Fecha1 { get; set; }
        public DateTime? Fecha2 { get; set; }
        public string? NombreUsuario { get; set; }
        public int? IdUsuario { get; set; }
        public int? IdArea { get; set; }
        public int? IdTipoPermiso { get; set; }

        // Resultados
        public string? Motivo { get; internal set; }
        public int Id { get; internal set; }
    }
}
