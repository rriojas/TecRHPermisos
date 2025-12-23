using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecursosHumanosWeb.Filters;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.ViewModels.Usuario;
using RecursosHumanosWeb.Helpers;
using AutoMapper.QueryableExtensions;

namespace RecursosHumanosWeb.Controllers.Api
{
    // Heredamos de ControllerBase para API
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosApiController : ControllerBase
    {
        private readonly RecursosHumanosContext _context;
        private readonly AutoMapper.IMapper _mapper;

        public UsuariosApiController(RecursosHumanosContext context, AutoMapper.IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // --- ACCIONES DE LECTURA (GET) ---

        // Simula la obtención de la lista (Similar a Index de tu MVC Controller)
        [HttpGet]
        [ApiKeyAuthorize(TablaObjetivo = "Usuario", PermisoRequerido = "Leer")]
        public async Task<IActionResult> GetUsuarios()
        {
            // Usar AutoMapper ProjectTo para proyectar eficientemente desde la BD
            var usuarios = await _context.Usuarios
                .Where(u => u.Estatus == true)
                .ProjectTo<RecursosHumanosWeb.Models.DTOs.UsuariosListDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            if (!usuarios.Any()) return NotFound("No se encontraron usuarios activos.");

            return Ok(usuarios);
        }

        // Obtiene detalles de un usuario (Similar a Details de tu MVC Controller)
        [HttpGet("{id}")]
        [ApiKeyAuthorize(TablaObjetivo = "Usuario", PermisoRequerido = "Leer")]
        public async Task<IActionResult> GetUsuario(int id)
        {
            // Proyectar a un DTO de detalle si necesitas más campos o usar el ViewModel
            var usuario = await _context.Usuarios
                .Where(u => u.Id == id)
                .ProjectTo<RecursosHumanosWeb.Models.ViewModels.Usuario.UsuarioDetailsViewModel>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (usuario == null) return NotFound($"Usuario con ID {id} no encontrado.");

            return Ok(usuario);
        }

        // --- ACCIONES DE ESCRITURA/CREACIÓN (POST) ---

        // Crea un nuevo usuario (Similar a POST Create de tu MVC Controller)
        [HttpPost]
        [ApiKeyAuthorize(TablaObjetivo = "Usuario", PermisoRequerido = "Escribir")]
        public async Task<IActionResult> PostUsuario([FromBody] UsuarioCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Aquí debes asegurarte de que la lógica de creación refleje la API Key.
            var userId = HttpContext.Items["ApiUserId"] as int? ?? 1; // ID del usuario de la API Key

            var newUsuario = new Usuario
            {
                Nombre = model.Nombre,
                Correo = model.Correo,
                IdDepartamento = model.IdDepartamento, // Asumimos que el ViewModel trae estos IDs
                IdArea = model.IdArea,
                IdPuesto = model.IdPuesto,
                IdTipoUsuario = model.IdTipoUsuario,
                Clave = string.IsNullOrWhiteSpace(model.Clave) ? string.Empty : PasswordHasher.HashSha256(model.Clave),
                Estatus = true,
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now,
                IdUsuarioCrea = userId, // Usamos el ID de la API Key
                IdUsuarioModifica = userId
            };

            _context.Usuarios.Add(newUsuario);
            await _context.SaveChangesAsync();
            // Devolvemos el 201 CreatedAtAction (estándar REST)
            // Proyectar el nuevo usuario a UsuarioDetailsViewModel para la respuesta
            var created = await _context.Usuarios
                .Where(u => u.Id == newUsuario.Id)
                .ProjectTo<RecursosHumanosWeb.Models.ViewModels.Usuario.UsuarioDetailsViewModel>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (created != null)
            {
                return CreatedAtAction(nameof(GetUsuario), new { id = newUsuario.Id }, created);
            }

            // Fallback: map the entity we just saved to the details VM
            var fallback = _mapper.Map<RecursosHumanosWeb.Models.ViewModels.Usuario.UsuarioDetailsViewModel>(newUsuario);
            return CreatedAtAction(nameof(GetUsuario), new { id = newUsuario.Id }, fallback);
        }

        // --- ACCIONES DE ACTUALIZACIÓN (PUT) ---

        // Actualiza un usuario existente (Similar a POST Edit de tu MVC Controller)
        [HttpPut("{id}")]
        [ApiKeyAuthorize(TablaObjetivo = "Usuario", PermisoRequerido = "Actualizar")]
        public async Task<IActionResult> PutUsuario(int id, [FromBody] UsuarioUpdateViewModel model)
        {
            // CORRECCIÓN: Usar IdUsuario en lugar de Id
            if (id != model.IdUsuario || !ModelState.IsValid)
            {
                return BadRequest("El ID de la URL no coincide con el ID del modelo o el modelo no es válido.");
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound($"Usuario con ID {id} no encontrado.");
            }

            var userId = HttpContext.Items["ApiUserId"] as int? ?? usuario.IdUsuarioModifica;
            
            // Mapear campos desde el ViewModel al entity usando AutoMapper
            _mapper.Map(model, usuario);
            
            // CORRECCIÓN: Hash de contraseña si se proporciona
            if (string.IsNullOrWhiteSpace(model.Clave))
            {
                // Evitar sobrescribir la clave si no se envió
                _context.Entry(usuario).Property(u => u.Clave).IsModified = false;
            }
            else
            {
                // Hash SHA-256 antes de guardar
                usuario.Clave = PasswordHasher.HashSha256(model.Clave);
            }

            usuario.FechaModificacion = DateTime.Now;
            usuario.IdUsuarioModifica = userId;

            try
            {
                _context.Update(usuario);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) when (!_context.Usuarios.Any(e => e.Id == id))
            {
                return NotFound($"Usuario con ID {id} no encontrado durante la actualización.");
            }

            return NoContent(); // 204 No Content (estándar REST)
        }

        // --- ACCIONES DE ELIMINACIÓN (DELETE) ---

        // Elimina un usuario (Simula el POST Delete de tu MVC Controller, pero con Baja Lógica)
        [HttpDelete("{id}")]
        [ApiKeyAuthorize(TablaObjetivo = "Usuario", PermisoRequerido = "Eliminar")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound($"Usuario con ID {id} no encontrado.");
            }

            // Validar que el usuario no esté ya inactivo
            if (!usuario.Estatus)
            {
                return BadRequest("El usuario ya está inactivo.");
            }

            // Realizamos Baja Lógica (cambiar Estatus a false), que es la práctica recomendada.
            var userId = HttpContext.Items["ApiUserId"] as int? ?? usuario.IdUsuarioModifica;

            usuario.Estatus = false;
            usuario.FechaModificacion = DateTime.Now;
            usuario.IdUsuarioModifica = userId;

            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }
    }
}
