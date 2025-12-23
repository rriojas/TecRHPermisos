using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RecursosHumanosWeb.Models.ViewModels
{
    public class PermisoCreateViewModel
    {
        [Required]
        public int IdTipoPermiso { get; set; }

        [Required]
        public DateTime Fecha1 { get; set; }

        public DateTime? Fecha2 { get; set; }

        public int? Dias { get; set; }

        [Required]
        [MaxLength(500)]
        public string Motivo { get; set; }

        [Required]
        public bool Goce { get; set; }

        public IEnumerable<TipoPermisoViewModel> TiposPermiso { get; set; }
    }

    public class TipoPermisoViewModel
    {
        public int Id { get; set; }
        public string Descripcion { get; set; }
    }
}