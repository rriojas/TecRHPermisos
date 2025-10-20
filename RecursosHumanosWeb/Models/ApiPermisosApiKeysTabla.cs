using System;
using System.Collections.Generic;

namespace RecursosHumanosWeb.Models;

public partial class ApiPermisosApiKeysTabla
{
    public int Id { get; set; }

    public int IdApiKey { get; set; }

    public int IdApiPermiso { get; set; }

    public int IdTabla { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public bool Estatus { get; set; }

    public int IdUsuarioCrea { get; set; }

    public int IdUsuarioModifica { get; set; }

    public virtual ApiKey IdApiKeyNavigation { get; set; } = null!;

    public virtual ApiPermiso IdApiPermisoNavigation { get; set; } = null!;

    public virtual Tabla IdTablaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioCreaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioModificaNavigation { get; set; } = null!;
}
