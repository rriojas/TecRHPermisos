using AutoMapper;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.ViewModels.Usuario;

namespace RecursosHumanosWeb.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Usuario mappings
            CreateMap<Usuario, UsuarioDetailsViewModel>()
                .ForMember(dest => dest.TipoUsuarioNombre, opt => opt.MapFrom(src => src.IdTipoUsuarioNavigation != null ? src.IdTipoUsuarioNavigation.Descripcion : string.Empty))
                .ForMember(dest => dest.AreaDescripcion, opt => opt.MapFrom(src => src.IdAreaNavigation != null ? src.IdAreaNavigation.Descripcion : string.Empty))
                .ForMember(dest => dest.DepartamentoDescripcion, opt => opt.MapFrom(src => src.IdDepartamentoNavigation != null ? src.IdDepartamentoNavigation.Descripcion : string.Empty))
                .ForMember(dest => dest.PuestoDescripcion, opt => opt.MapFrom(src => src.IdPuestoNavigation != null ? src.IdPuestoNavigation.Descripcion : string.Empty))
                .ForMember(dest => dest.UsuarioCreaNombre, opt => opt.MapFrom(src => src.IdUsuarioCreaNavigation != null ? src.IdUsuarioCreaNavigation.Nombre : string.Empty))
                .ForMember(dest => dest.UsuarioModificaNombre, opt => opt.MapFrom(src => src.IdUsuarioModificaNavigation != null ? src.IdUsuarioModificaNavigation.Nombre : string.Empty));
            // Ensure ProjectTo can resolve navigation mappings
            CreateMap<Usuario, UsuarioDetailsViewModel>()
                .ForMember(dest => dest.TipoUsuarioNombre, opt => opt.MapFrom(src => src.IdTipoUsuarioNavigation != null ? src.IdTipoUsuarioNavigation.Descripcion : string.Empty))
                .ForMember(dest => dest.AreaDescripcion, opt => opt.MapFrom(src => src.IdAreaNavigation != null ? src.IdAreaNavigation.Descripcion : string.Empty))
                .ForMember(dest => dest.DepartamentoDescripcion, opt => opt.MapFrom(src => src.IdDepartamentoNavigation != null ? src.IdDepartamentoNavigation.Descripcion : string.Empty))
                .ForMember(dest => dest.PuestoDescripcion, opt => opt.MapFrom(src => src.IdPuestoNavigation != null ? src.IdPuestoNavigation.Descripcion : string.Empty))
                .ForMember(dest => dest.UsuarioCreaNombre, opt => opt.MapFrom(src => src.IdUsuarioCreaNavigation != null ? src.IdUsuarioCreaNavigation.Nombre : string.Empty))
                .ForMember(dest => dest.UsuarioModificaNombre, opt => opt.MapFrom(src => src.IdUsuarioModificaNavigation != null ? src.IdUsuarioModificaNavigation.Nombre : string.Empty));

            CreateMap<UsuarioCreateViewModel, Usuario>();
            CreateMap<UsuarioUpdateViewModel, Usuario>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.IdUsuario))
                .ForMember(dest => dest.Clave, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Clave)));

            // Reverse maps for convenience
            CreateMap<Usuario, UsuarioUpdateViewModel>()
                .ForMember(dest => dest.IdUsuario, opt => opt.MapFrom(src => src.Id));

            CreateMap<Permiso, RecursosHumanosWeb.Models.ViewModels.PermissionsCreateViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.IdUsuarioSolicita, opt => opt.MapFrom(src => src.IdUsuarioSolicita))
                .ForMember(dest => dest.IdTipoPermiso, opt => opt.MapFrom(src => src.IdTipoPermiso))
                .ForMember(dest => dest.Fecha1, opt => opt.MapFrom(src => src.Fecha1))
                .ForMember(dest => dest.Fecha2, opt => opt.MapFrom(src => src.Fecha2))
                .ForMember(dest => dest.Motivo, opt => opt.MapFrom(src => src.Motivo))
                .ForMember(dest => dest.Evidencia, opt => opt.MapFrom(src => src.Evidencia))
                .ForMember(dest => dest.Goce, opt => opt.MapFrom(src => src.Goce));

            CreateMap<RecursosHumanosWeb.Models.ViewModels.PermissionsCreateViewModel, Permiso>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? 0))
                .ForMember(dest => dest.Motivo, opt => opt.MapFrom(src => src.Motivo))
                .ForMember(dest => dest.Fecha1, opt => opt.MapFrom(src => src.Fecha1))
                .ForMember(dest => dest.Fecha2, opt => opt.MapFrom(src => src.Fecha2))
                .ForMember(dest => dest.IdTipoPermiso, opt => opt.MapFrom(src => src.IdTipoPermiso))
                .ForMember(dest => dest.IdUsuarioSolicita, opt => opt.MapFrom(src => src.IdUsuarioSolicita ?? 0))
                .ForMember(dest => dest.Evidencia, opt => opt.MapFrom(src => src.Evidencia))
                .ForMember(dest => dest.Goce, opt => opt.MapFrom(src => src.Goce ?? false));

            // Mappings to DTOs used in projections/views
            CreateMap<Permiso, RecursosHumanosWeb.Models.DTOs.PermisosDTO>()
                .ForMember(d => d.Nombre, o => o.MapFrom(s => s.IdUsuarioSolicitaNavigation != null ? s.IdUsuarioSolicitaNavigation.Nombre : string.Empty))
                .ForMember(d => d.Correo, o => o.MapFrom(s => s.IdUsuarioSolicitaNavigation != null ? s.IdUsuarioSolicitaNavigation.Correo : string.Empty))
                .ForMember(d => d.TipoPermiso, o => o.MapFrom(s => s.IdTipoPermisoNavigation != null ? s.IdTipoPermisoNavigation.Descripcion : string.Empty))
                .ForMember(d => d.IdTipoPermiso, o => o.MapFrom(s => s.IdTipoPermiso));

            CreateMap<Corte, RecursosHumanosWeb.Models.DTOs.CortesDTO>();

            CreateMap<Usuario, RecursosHumanosWeb.Models.DTOs.UsuariosDTO>();
            CreateMap<Usuario, RecursosHumanosWeb.Models.DTOs.UsuariosListDTO>()
                .ForMember(d => d.Departamento, o => o.MapFrom(s => s.IdDepartamentoNavigation != null ? s.IdDepartamentoNavigation.Descripcion : string.Empty));
        }
    }
}
