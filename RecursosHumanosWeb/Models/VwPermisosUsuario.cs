using System;
using System.Collections.Generic;

namespace RecursosHumanosWeb.Models;

public partial class VwPermisosUsuario
{
    public int Id { get; set; }

    public string? Descripcion { get; set; }

    public int? DiasSolicitados { get; set; }

    public bool ConGoce { get; set; }

    public TimeOnly? HoraEntrada { get; set; }

    public TimeOnly? HorarioCubrir { get; set; }

    public string? Evidencia { get; set; }

    public DateOnly? DelDia { get; set; }

    public DateOnly? AlDia { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public DateTime? FechaSolicitud { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public DateTime? FechaAutorizacion { get; set; }

    public int IdTipoPermisos { get; set; }

    public int IdCorte { get; set; }

    public bool Estatus { get; set; }

    public int IdUsuarioSolicitud { get; set; }

    public int IdUsuarioCrea { get; set; }

    public int IdUsuarioModifica { get; set; }

    public int? IdUsuarioAutorizador { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public int IdArea { get; set; }
}
