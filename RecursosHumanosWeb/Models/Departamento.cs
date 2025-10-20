using System;
using System.Collections.Generic;

namespace RecursosHumanosWeb.Models;

public partial class Departamento
{
    public int Id { get; set; }

    public string Descripcion { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public bool Estatus { get; set; }

    public int IdUsuarioCrea { get; set; }

    public int IdUsuarioModifica { get; set; }

    public virtual Usuario IdUsuarioCreaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioModificaNavigation { get; set; } = null!;

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
