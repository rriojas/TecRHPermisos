using System;
using System.Collections.Generic;

namespace RecursosHumanosWeb.Models;

public partial class Corte
{
    public int Id { get; set; }

    public DateTime? Inicia { get; set; }

    public DateTime? Termina { get; set; }

    // FechaCreacion no puede ser nula en la BD
    public DateTime? FechaCreacion { get; set; }

    // FechaModificacion puede ser nula
    public DateTime? FechaModificacion { get; set; }

    public bool? Estatus { get; set; }

    public int? IdUsuarioCrea { get; set; }

    public int? IdUsuarioModifica { get; set; }

    public virtual Usuario IdUsuarioCreaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioModificaNavigation { get; set; } = null!;

    public virtual ICollection<Permiso> Permisos { get; set; } = new List<Permiso>();
}
