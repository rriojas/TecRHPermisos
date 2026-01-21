using System;
using System.Collections.Generic;

namespace RecursosHumanosWeb.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string Clave { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public bool Estatus { get; set; }

    public int IdTipoUsuario { get; set; }

    public int IdArea { get; set; }

    public int IdDepartamento { get; set; }

    public int IdPuesto { get; set; }

    public int IdUsuarioCrea { get; set; }

    public int? IdUsuarioModifica { get; set; }

    public int? IdApiKey { get; set; }

    // ==========================================
    // PROPIEDADES DE NAVEGACIÓN (CORREGIDAS)
    // ==========================================
    // El '?' permite que Entity Framework maneje los ciclos del ID 1 
    // y los valores NULL del ID 2 sin lanzar excepciones.

    public virtual Area? IdAreaNavigation { get; set; }

    public virtual Departamento? IdDepartamentoNavigation { get; set; }

    public virtual Puesto? IdPuestoNavigation { get; set; }

    public virtual TipoUsuario? IdTipoUsuarioNavigation { get; set; }

    public virtual Usuario? IdUsuarioCreaNavigation { get; set; }

    public virtual Usuario? IdUsuarioModificaNavigation { get; set; }

    // ==========================================
    // COLECCIONES DE RELACIONES
    // ==========================================

    public virtual ICollection<ApiFunction> ApiFunctionIdUsuarioCreaNavigations { get; set; } = new List<ApiFunction>();
    public virtual ICollection<ApiFunction>? ApiFunctionIdUsuarioModificaNavigations { get; set; }

    public virtual ICollection<ApiKey> ApiKeyIdUsuarioCreaNavigations { get; set; } = new List<ApiKey>();
    public virtual ICollection<ApiKey>? ApiKeyIdUsuarioModificaNavigations { get; set; }

    public virtual ICollection<ApiPermiso> ApiPermisoIdUsuarioCreaNavigations { get; set; } = new List<ApiPermiso>();
    public virtual ICollection<ApiPermiso>? ApiPermisoIdUsuarioModificaNavigations { get; set; }

    public virtual ICollection<ApiPermisosApiKeysTabla> ApiPermisosApiKeysTablaIdUsuarioCreaNavigations { get; set; } = new List<ApiPermisosApiKeysTabla>();
    public virtual ICollection<ApiPermisosApiKeysTabla>? ApiPermisosApiKeysTablaIdUsuarioModificaNavigations { get; set; }

    public virtual ICollection<Area> AreaIdUsuarioCreaNavigations { get; set; } = new List<Area>();
    public virtual ICollection<Area>? AreaIdUsuarioModificaNavigations { get; set; }

    public virtual ICollection<Corte> CorteIdUsuarioCreaNavigations { get; set; } = new List<Corte>();
    public virtual ICollection<Corte>? CorteIdUsuarioModificaNavigations { get; set; }

    public virtual ICollection<Departamento> DepartamentoIdUsuarioCreaNavigations { get; set; } = new List<Departamento>();
    public virtual ICollection<Departamento>? DepartamentoIdUsuarioModificaNavigations { get; set; }

    public virtual ICollection<Usuario> InverseIdUsuarioCreaNavigation { get; set; } = new List<Usuario>();
    public virtual ICollection<Usuario>? InverseIdUsuarioModificaNavigation { get; set; }

    public virtual ICollection<Permiso>? PermisoIdUsuarioAutorizaNavigations { get; set; }
    public virtual ICollection<Permiso> PermisoIdUsuarioCreaNavigations { get; set; } = new List<Permiso>();
    public virtual ICollection<Permiso>? PermisoIdUsuarioModificaNavigations { get; set; }
    public virtual ICollection<Permiso> PermisoIdUsuarioSolicitaNavigations { get; set; } = new List<Permiso>();

    public virtual ICollection<Puesto> PuestoIdUsuarioCreaNavigations { get; set; } = new List<Puesto>();
    public virtual ICollection<Puesto>? PuestoIdUsuarioModificaNavigations { get; set; }

    public virtual ICollection<Tabla> TablaIdUsuarioCreaNavigations { get; set; } = new List<Tabla>();
    public virtual ICollection<Tabla>? TablaIdUsuarioModificaNavigations { get; set; }

    public virtual ICollection<TipoPermiso> TipoPermisoIdUsuarioCreaNavigations { get; set; } = new List<TipoPermiso>();
    public virtual ICollection<TipoPermiso>? TipoPermisoIdUsuarioModificaNavigations { get; set; }

    public virtual ICollection<TipoUsuario> TipoUsuarioIdUsuarioCreaNavigations { get; set; } = new List<TipoUsuario>();
    public virtual ICollection<TipoUsuario>? TipoUsuarioIdUsuarioModificaNavigations { get; set; }
}