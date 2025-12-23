
namespace RecursosHumanosWeb.Models.DTOs
{
    public class AlertResponseDTO
    {
        // Propiedades de la Alerta (con valores por defecto limpios)
        public bool Success { get; set; } = false;
        public string Message { get; set; } = "Operación no completada.";
        public string Title { get; set; } = "Error";
        public string Icon { get; set; } = "error"; // success, error, warning, info, question

        // Propiedades de Confirmación (Para el flujo de dos pasos)
        public bool ShowConfirmation { get; set; } = false;
        public string ConfirmButtonText { get; set; } = "Sí, continuar";
        public string CancelButtonText { get; set; } = "Cancelar";

        // Propiedad de Redirección (Manejada en el cliente JS si se incluye)
        public string RedirectUrl { get; set; }

        // Datos adicionales
        public object Data { get; set; }
        public Dictionary<string, string[]> Errors { get; internal set; }

        // =========================================================
        // 1. Constructor por defecto: Necesario para la deserialización JSON
        //    y se usa para respuestas de error, confirmación o info genérica.
        // =========================================================
        public AlertResponseDTO() { }

        // =========================================================
        // 2. Constructor Enfocado: Para el caso específico de ÉXITO + REDIRECCIÓN AUTOMÁTICA
        //    (Simplifica la lógica del controlador para Create, Edit, etc.).
        // =========================================================

        /// <summary>
        /// Crea una respuesta de éxito con redirección automática al Index del controlador actual.
        /// </summary>
        /// <param name="controllerName">Nombre del controlador (obtenido de RouteData).</param>
        /// <param name="message">Mensaje de éxito a mostrar.</param>
        /// <param name="title">Título de la alerta (opcional).</param>
        /// <param name="showView">Vista de destino (opcional, por defecto "Index").</param>
        public AlertResponseDTO(string controllerName, string message, string title = "¡Éxito!", string showView = "Index")
        {
            this.Success = true;
            this.Icon = "success";
            this.Title = title;
            this.Message = message;
            this.RedirectUrl = $"/{controllerName}/{showView}";
        }
    }
}