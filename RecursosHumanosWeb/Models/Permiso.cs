using System;
using System.Collections.Generic;

namespace RecursosHumanosWeb.Models;

public partial class Permiso
{
    public int Id { get; set; }

    // Fecha1 y Fecha2 pueden ser nulas según requisitos
    public DateTime? Fecha1 { get; set; }

    public DateTime? Fecha2 { get; set; }

    public int? Dias { get; set; }

    public string? Motivo { get; set; }

    public string? Evidencia { get; set; }

    public DateTime? FechaAutorizacion { get; set; }

    // FechaCreacion no puede ser nula en la BD
    public DateTime? FechaCreacion { get; set; }

    // FechaModificacion puede ser nula si no ha habido cambios
    public DateTime? FechaModificacion { get; set; }
    public bool? Revisado { get; set; } = false;

    public bool? Goce { get; set; } = false;

    public bool? Estatus { get; set; } = true;

    public int? IdCorte { get; set; }

    public int? IdTipoPermiso { get; set; }

    public int? IdUsuarioSolicita { get; set; }

    public int? IdUsuarioAutoriza { get; set; }

    public int? IdUsuarioCrea { get; set; }

    public int? IdUsuarioModifica { get; set; }

    public virtual Corte IdCorteNavigation { get; set; } = null!;

    public virtual TipoPermiso IdTipoPermisoNavigation { get; set; } = null!;

    public virtual Usuario? IdUsuarioAutorizaNavigation { get; set; }

    public virtual Usuario IdUsuarioCreaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioModificaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioSolicitaNavigation { get; set; } = null!;

}
