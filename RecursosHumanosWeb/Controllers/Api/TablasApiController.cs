using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecursosHumanosWeb.Filters;
using RecursosHumanosWeb.Models;
using System.Linq;
using System.Threading.Tasks;

namespace RecursosHumanosWeb.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class TablasApiController : ControllerBase
    {
        private readonly RecursosHumanosContext _context;

        public TablasApiController(RecursosHumanosContext context)
        {
            _context = context;
        }

        // -----------------------------------------------------------------
        // GET: api/tablas
        // Permiso requerido: Leer sobre la tabla "Tabla"
        // -----------------------------------------------------------------
        [HttpGet]
        [ApiKeyAuthorize(TablaObjetivo = "Tabla", PermisoRequerido = "Leer")]
        public async Task<IActionResult> GetTablas()
        {
            // Solo devolvemos las tablas activas y campos relevantes para la API
            var tablas = await _context.Tablas
                .Where(t => t.Estatus == true)
                .Select(t => new
                {
                    t.Id,
                    t.Descripcion,
                    t.FechaCreacion
                })
                .ToListAsync();

            if (!tablas.Any())
            {
                return NotFound("No se encontraron tablas activas.");
            }

            return Ok(tablas);
        }

        // -----------------------------------------------------------------
        // GET: api/tablas/{id}
        // Permiso requerido: Leer sobre la tabla "Tabla"
        // -----------------------------------------------------------------
        [HttpGet("{id}")]
        [ApiKeyAuthorize(TablaObjetivo = "Tabla", PermisoRequerido = "Leer")]
        public async Task<IActionResult> GetTabla(int id)
        {
            var tabla = await _context.Tablas
                .Where(t => t.Id == id && t.Estatus == true)
                .Select(t => new
                {
                    t.Id,
                    t.Descripcion,
                    t.FechaCreacion,
                    t.FechaModificacion,
                    // Incluir usuarios de creación/modificación si es necesario
                    CreadoPor = t.IdUsuarioCreaNavigation.Nombre,
                    ModificadoPor = t.IdUsuarioModificaNavigation.Nombre
                })
                .FirstOrDefaultAsync();

            if (tabla == null)
            {
                return NotFound($"Tabla con ID {id} no encontrada o inactiva.");
            }

            return Ok(tabla);
        }

        // NOTA: No incluimos POST, PUT o DELETE a menos que sea necesario 
        // gestionar las tablas directamente desde la API, lo cual no es lo habitual.
    }
}