using System.ComponentModel.DataAnnotations;

namespace RecursosHumanosWeb.Models.ViewModels
{
    public class CorteCreateViewModel
    {
        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        [DataType(DataType.DateTime)]
        public DateTime? Inicia { get; set; }

        [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
        [DataType(DataType.DateTime)]
        public DateTime? Termina { get; set; }
    }
}
