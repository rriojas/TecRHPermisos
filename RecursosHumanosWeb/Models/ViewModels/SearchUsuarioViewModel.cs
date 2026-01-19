using X.PagedList;

namespace RecursosHumanosWeb.Models.ViewModels
{
    public class SearchUsuarioViewModel
    {
        public IPagedList<RecursosHumanosWeb.Models.Usuario>? UsuariosPaginados { get; set; }
        public IEnumerable<RecursosHumanosWeb.Models.Usuario> Usuarios { get; set; } = new List<RecursosHumanosWeb.Models.Usuario>();
        public IEnumerable<Area> Areas { get; set; } = new List<Area>();
        public IEnumerable<Departamento> Departamentos { get; set; } = new List<Departamento>();
    }
}
