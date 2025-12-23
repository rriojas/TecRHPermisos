using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace RecursosHumanosWeb.Models.ViewModels
{
    public class ApiKeyCreateViewModel
    {
        // -----------------------------------------------------------------
        // PROPIEDADES DE EDICIÓN (Cargadas desde la DB y pasadas como hidden)
        // -----------------------------------------------------------------
        public int Id { get; set; } 

        [Display(Name = "Clave")]
        public string Clave { get; set; } = null!; 

        [Display(Name = "Fecha Creación")]
        public DateTime FechaCreacion { get; set; } 
                                                 

        // Propiedad para el Id del usuario que crea/modifica (se llena en el controlador)
        public int IdUsuarioCrea { get; set; }

        // Campo para la selección del usuario titular (se puede cambiar en EDIT)
        [Required(ErrorMessage = "El titular de la API Key es obligatorio.")]
        [Display(Name = "Usuario Titular")]
        public int? IdUsuarioTitular { get; set; }

        // Colección para capturar los permisos/tablas seleccionadas (tanto en CREATE como en EDIT)
        public List<ApiPermissionAssignmentViewModel>? Assignments { get; set; } = new List<ApiPermissionAssignmentViewModel>();
    }

  
}