using X.PagedList;

namespace RecursosHumanosWeb.Models.ViewModels.Usuario
{
    public class UsuarioSearchViewModel
    {
        public IPagedList<Models.Usuario>? UsuariosPaginados { get; set; }
        public IEnumerable<Models.Usuario> Usuarios { get; set; } = new List<Models.Usuario>();
        public IEnumerable<Area> Areas { get; set; } = new List<Area>();
        public IEnumerable<Departamento> Departamentos { get; set; } = new List<Departamento>();
    }
}
