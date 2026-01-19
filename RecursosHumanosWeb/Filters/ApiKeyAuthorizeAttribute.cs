// --- EN ApiKeyAuthorizeAttribute.cs ---

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using RecursosHumanosWeb.Models;

namespace RecursosHumanosWeb.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public string? TablaObjetivo { get; set; }
        public string? PermisoRequerido { get; set; }

        private const string HeaderApiKeyName = "X-API-KEY";
        private const string QueryStringApiKeyName = "api_key"; // Nombre del parámetro en la URL

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<RecursosHumanosContext>();
            string? extractedApiKey = null;

            // 1. INTENTAR OBTENER LA CLAVE DEL ENCABEZADO (MÉTODO PREFERIDO Y SEGURO)
            if (context.HttpContext.Request.Headers.TryGetValue(HeaderApiKeyName, out var headerApiKey))
            {
                extractedApiKey = headerApiKey.ToString();
            }
            // 2. SI NO ESTÁ EN EL HEADER, INTENTAR OBTENERLA DEL QUERY STRING (MÉTODO DE PRUEBA)
            else if (context.HttpContext.Request.Query.ContainsKey(QueryStringApiKeyName))
            {
                extractedApiKey = context.HttpContext.Request.Query[QueryStringApiKeyName].ToString();
            }

            // 3. SI NO SE ENCONTRÓ EN NINGÚN LUGAR, NEGAR EL ACCESO
            if (string.IsNullOrEmpty(extractedApiKey))
            {
                // Usamos UnauthorizedObjectResult para asegurar una respuesta API limpia (401)
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "Unauthorized",
                    message = $"Se requiere el encabezado '{HeaderApiKeyName}' o el parámetro de URL '{QueryStringApiKeyName}'."
                });
                return;
            }

            // 4. BÚSQUEDA Y VALIDACIÓN EN LA BASE DE DATOS (Misma lógica que antes)
            var apiKeyEntity = await dbContext.ApiKeys
                // Incluimos las relaciones necesarias para la validación de permisos
                .Include(k => k.ApiPermisosApiKeysTablas)
                    .ThenInclude(pkt => pkt.IdTablaNavigation)
                .Include(k => k.ApiPermisosApiKeysTablas)
                    .ThenInclude(pkt => pkt.IdApiPermisoNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Clave == extractedApiKey && k.Estatus == true);

            if (apiKeyEntity == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            // 5. VALIDACIÓN DE PERMISOS GRANULARES (Sin cambios)
            if (!string.IsNullOrEmpty(TablaObjetivo) && !string.IsNullOrEmpty(PermisoRequerido))
            {
                bool tienePermiso = apiKeyEntity.ApiPermisosApiKeysTablas.Any(pkt =>
                    pkt.Estatus == true &&
                    pkt.IdTablaNavigation.Descripcion.Equals(TablaObjetivo, StringComparison.OrdinalIgnoreCase) &&
                    pkt.IdApiPermisoNavigation.Descripcion.Equals(PermisoRequerido, StringComparison.OrdinalIgnoreCase)
                );

                if (!tienePermiso)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }

            // 6. ÉXITO: Continuar
            context.HttpContext.Items["ApiUserId"] = apiKeyEntity.IdUsuarioCrea;
        }
    }
}