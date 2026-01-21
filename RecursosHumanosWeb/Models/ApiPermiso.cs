using System;
using System.Collections.Generic;

namespace RecursosHumanosWeb.Models;

public partial class ApiPermiso
{
    public int Id { get; set; }

    public string Descripcion { get; set; }

    public DateTime FechaCreacion { get; set; }

    // Permitir null en FechaModificacion
    public DateTime? FechaModificacion { get; set; }

    public bool Estatus { get; set; }

    public int IdUsuarioCrea { get; set; }

    public int? IdUsuarioModifica { get; set; }

    public virtual ICollection<ApiPermisosApiKeysTabla> ApiPermisosApiKeysTablas { get; set; } = new List<ApiPermisosApiKeysTabla>();

    public virtual Usuario IdUsuarioCreaNavigation { get; set; } = null!;

    public virtual Usuario? IdUsuarioModificaNavigation { get; set; } = null!;
}
