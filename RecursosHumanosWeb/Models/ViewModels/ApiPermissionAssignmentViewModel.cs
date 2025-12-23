namespace RecursosHumanosWeb.Models.ViewModels
{
    // -----------------------------------------------------------------
    // SUB-MODELO: Detalle de Asignación de Permisos por Tabla
    // -----------------------------------------------------------------
    public class ApiPermissionAssignmentViewModel
    {
        // ID de la Tabla a la que se le asignarán permisos (ej. 1=Usuario, 2=Tabla)
        public int IdTabla { get; set; }

        // Lista de IDs de los ApiPermisos seleccionados (ej. [1, 2] = Leer, Escribir)
        // El Mvc Binder recolecta los valores de todos los checkboxes marcados aquí.
        public List<int>? IdsApiPermisosSeleccionados { get; set; }
    }
}
