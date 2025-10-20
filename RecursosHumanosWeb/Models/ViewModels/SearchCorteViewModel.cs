using RecursosHumanosWeb.Models.DTOs;
using X.PagedList;

namespace RecursosHumanosWeb.Models.ViewModels
{
    public class SearchCorteViewModel
    {
        public IPagedList<CortesDTO> Cortes { get; set; }
        public List<UsuariosDTO> Usuarios { get; set; }
        public string FechaDesde { get; set; }
        public string FechaHasta { get; set; }
        public string UsuarioCreadorFilter { get; set; }
        public string StatusFilter { get; set; }
        public bool IsAdministrador { get; set; }
    }
}
