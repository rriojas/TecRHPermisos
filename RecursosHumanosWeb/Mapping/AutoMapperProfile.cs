using AutoMapper;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.ViewModels.Usuario;
using RecursosHumanosWeb.Models.DTOs;

namespace RecursosHumanosWeb.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Usuario, UsuarioDetailsViewModel>()
                .ForMember(dest => dest.TipoUsuarioNombre, opt => opt.MapFrom(src => src.IdTipoUsuarioNavigation != null ? src.IdTipoUsuarioNavigation.Descripcion : "N/A"))
                .ForMember(dest => dest.AreaDescripcion, opt => opt.MapFrom(src => src.IdAreaNavigation != null ? src.IdAreaNavigation.Descripcion : "N/A"))
                .ForMember(dest => dest.DepartamentoDescripcion, opt => opt.MapFrom(src => src.IdDepartamentoNavigation != null ? src.IdDepartamentoNavigation.Descripcion : "N/A"))
                .ForMember(dest => dest.PuestoDescripcion, opt => opt.MapFrom(src => src.IdPuestoNavigation != null ? src.IdPuestoNavigation.Descripcion : "N/A"))
                // Lógica especial para auditoría (IDs 1 y 2)
                .ForMember(dest => dest.UsuarioCreaNombre, opt => opt.MapFrom(src => 
                    src.IdUsuarioCreaNavigation != null ? src.IdUsuarioCreaNavigation.Nombre : "Sistema/Auto-creado"))
                .ForMember(dest => dest.UsuarioModificaNombre, opt => opt.MapFrom(src => 
                    src.IdUsuarioModificaNavigation != null ? src.IdUsuarioModificaNavigation.Nombre : "Sin cambios"));

            // Otros mapeos de Usuario
            CreateMap<UsuarioCreateViewModel, Usuario>();
            CreateMap<UsuarioUpdateViewModel, Usuario>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.IdUsuario))
                .ForMember(dest => dest.Clave, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Clave)));

            CreateMap<Usuario, UsuarioUpdateViewModel>()
                .ForMember(dest => dest.IdUsuario, opt => opt.MapFrom(src => src.Id));

            // Mapeos de Permisos (Corrigiendo coerción de tipos)
            CreateMap<Permiso, PermisosDTO>()
                .ForMember(d => d.Nombre, o => o.MapFrom(s => s.IdUsuarioSolicitaNavigation != null ? s.IdUsuarioSolicitaNavigation.Nombre : "N/A"))
                .ForMember(d => d.TipoPermiso, o => o.MapFrom(s => s.IdTipoPermisoNavigation != null ? s.IdTipoPermisoNavigation.Descripcion : "N/A"));

            // DTOs y Listas
            CreateMap<Corte, CortesDTO>();
            CreateMap<Usuario, UsuariosDTO>();
            CreateMap<Usuario, UsuariosListDTO>()
                .ForMember(d => d.Departamento, o => o.MapFrom(s => s.IdDepartamentoNavigation != null ? s.IdDepartamentoNavigation.Descripcion : "N/A"));
        }
    }
}