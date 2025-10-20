using X.PagedList;

namespace RecursosHumanosWeb.Models.ViewModels
{
    public class SearchUsuarioViewModel
    {
        public IPagedList<Usuario>? UsuariosPaginados { get; set; }
        public IEnumerable<Usuario> Usuarios { get; set; } = new List<Usuario>();
        public IEnumerable<Area> Areas { get; set; } = new List<Area>();
        public IEnumerable<Departamento> Departamentos { get; set; } = new List<Departamento>();
    }
}
