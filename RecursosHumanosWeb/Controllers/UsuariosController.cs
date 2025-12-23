using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.DTOs;
using RecursosHumanosWeb.Models.ViewModels;
using RecursosHumanosWeb.Models.ViewModels.Usuario;
using X.PagedList.Extensions;
using RecursosHumanosWeb.Helpers;

namespace RecursosHumanosWeb.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly RecursosHumanosContext _context;
        private readonly AutoMapper.IMapper _mapper;

        public UsuariosController(RecursosHumanosContext context, AutoMapper.IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }


        // GET: Usuarios
        public async Task<IActionResult> Index(
            string nombreFilter,
            string correoFilter,
            int? departamentoFilter,
            bool? estadoFilter, // NO se modifica el valor por defecto en la firma
            int? page,
            string sortOrder,
            string currentFilter)
        {
            // Verificar si el usuario es administrador
            ViewData["IsAdministrador"] = User.HasClaim(c => c.Type == "TipoUsuario" && c.Value == "4");

            // Parámetros de ordenamiento
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NombreSortParm = string.IsNullOrEmpty(sortOrder) ? "nombre_desc" : "";
            ViewBag.CorreoSortParm = sortOrder == "correo" ? "correo_desc" : "correo";
            ViewBag.DepartamentoSortParm = sortOrder == "departamento" ? "departamento_desc" : "departamento";
            ViewBag.FechaSortParm = sortOrder == "fecha" ? "fecha_desc" : "fecha";

            // Si hay un nuevo filtro, resetear a la página 1
            if (nombreFilter != null || correoFilter != null || departamentoFilter != null || estadoFilter != null)
            {
                page = 1;
            }
            else
            {
                // Mantener los filtros actuales si solo se está paginando
                nombreFilter = currentFilter;
            }

            ViewBag.CurrentFilter = nombreFilter;

            // Construir la consulta base con las relaciones necesarias
            var query = _context.Usuarios
                .Include(u => u.IdAreaNavigation)
                .Include(u => u.IdDepartamentoNavigation)
                .Include(u => u.IdPuestoNavigation)
                .Include(u => u.IdTipoUsuarioNavigation)
                .Include(u => u.IdUsuarioCreaNavigation)
                .Include(u => u.IdUsuarioModificaNavigation)
                .AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrEmpty(nombreFilter))
            {
                query = query.Where(u => u.Nombre.Contains(nombreFilter));
            }

            if (!string.IsNullOrEmpty(correoFilter))
            {
                query = query.Where(u => u.Correo.Contains(correoFilter));
            }

            if (departamentoFilter.HasValue)
            {
                query = query.Where(u => u.IdDepartamento == departamentoFilter.Value);
            }

            if (estadoFilter.HasValue)
            {
                query = query.Where(u => u.Estatus == estadoFilter.Value);
            }
            // NOTA: Si estadoFilter no tiene valor, se muestran todos (activos e inactivos), 
            // a menos que apliques aquí una restricción por defecto fuera de los parámetros.

            // Aplicar ordenamiento
            query = sortOrder switch
            {
                "nombre_desc" => query.OrderByDescending(u => u.Nombre),
                "correo" => query.OrderBy(u => u.Correo),
                "correo_desc" => query.OrderByDescending(u => u.Correo),
                "departamento" => query.OrderBy(u => u.IdDepartamentoNavigation.Descripcion),
                "departamento_desc" => query.OrderByDescending(u => u.IdDepartamentoNavigation.Descripcion),
                "fecha" => query.OrderBy(u => u.FechaCreacion),
                "fecha_desc" => query.OrderByDescending(u => u.FechaCreacion),
                _ => query.OrderBy(u => u.Nombre) // Orden por defecto
            };

            // Configuración de paginación
            int pageSize = 10;
            int pageNumber = page ?? 1;

            // Obtener usuarios paginados
            var usuariosPaginados = query.ToPagedList(pageNumber, pageSize);

            // Obtener las listas para los filtros
            var areas = await _context.Areas.ToListAsync();
            var departamentos = await _context.Departamentos.ToListAsync();

            // Crear el modelo de la vista
            var model = new UsuarioSearchViewModel
            {
                UsuariosPaginados = usuariosPaginados,
                Areas = areas,
                Departamentos = departamentos
            };

            // Preservar los valores de los filtros
            ViewBag.NombreFilter = nombreFilter;
            ViewBag.CorreoFilter = correoFilter;
            ViewBag.DepartamentoFilter = departamentoFilter;
            ViewBag.EstadoFilter = estadoFilter;

            return View(model);
        }

        // GET: Usuarios/Details/5 (Sin cambios)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .Include(u => u.IdAreaNavigation)
                .Include(u => u.IdDepartamentoNavigation)
                .Include(u => u.IdPuestoNavigation)
                .Include(u => u.IdTipoUsuarioNavigation)
                .Include(u => u.IdUsuarioCreaNavigation)
                .Include(u => u.IdUsuarioModificaNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            var vm = _mapper.Map<RecursosHumanosWeb.Models.ViewModels.Usuario.UsuarioDetailsViewModel>(usuario);
            return View(vm);
        }

        // GET: Usuarios/Create
        public IActionResult Create()
        {
            PrepareUserSelectLists();
            var vm = new RecursosHumanosWeb.Models.ViewModels.Usuario.UsuarioCreateViewModel();
            return View(vm);
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecursosHumanosWeb.Models.ViewModels.Usuario.UsuarioCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var usuario = _mapper.Map<Usuario>(model);
                // Prevent assigning ApiKey directly here; ApiKeysController manages keys
                usuario.IdApiKey = null;
                // Hash password before saving (SHA-256, matches DB/SP expectation if needed)
                usuario.Clave = string.IsNullOrWhiteSpace(model.Clave) ? string.Empty : PasswordHasher.HashSha256(model.Clave);
                _context.Add(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PrepareUserSelectLists();
            return View(model);
        }

        // GET: Usuarios/Edit/5 (Sin cambios)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }
            // Map entity to UpdateViewModel
            var vm = _mapper.Map<RecursosHumanosWeb.Models.ViewModels.Usuario.UsuarioUpdateViewModel>(usuario);
            vm.Clave = null; // do not prefill password

            PrepareUserSelectLists(usuario.IdUsuarioCrea, usuario.IdUsuarioModifica);
            return View(vm);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RecursosHumanosWeb.Models.ViewModels.Usuario.UsuarioUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PrepareUserSelectLists();
                return View(model);
            }

            var usuario = await _context.Usuarios.FindAsync(model.IdUsuario);
            if (usuario == null) return NotFound();

            // Preserve existing ApiKey: do not allow editing it from this action
            var existingApiKey = usuario.IdApiKey;

            // Map the fields from viewmodel onto the entity (preserve other fields)
            _mapper.Map(model, usuario);
            usuario.IdApiKey = existingApiKey;
            if (string.IsNullOrWhiteSpace(model.Clave))
            {
                // Prevent overwriting Clave with null/empty when mapping
                _context.Entry(usuario).Property(u => u.Clave).IsModified = false;
            }
            else
            {
                // Hash provided password before saving
                usuario.Clave = PasswordHasher.HashSha256(model.Clave);
            }

            try
            {
                _context.Update(usuario);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(usuario.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Usuarios/Delete/5 (Sin cambios)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .Include(u => u.IdAreaNavigation)
                .Include(u => u.IdDepartamentoNavigation)
                .Include(u => u.IdPuestoNavigation)
                .Include(u => u.IdTipoUsuarioNavigation)
                .Include(u => u.IdUsuarioCreaNavigation)
                .Include(u => u.IdUsuarioModificaNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // POST: Usuarios/Delete/5 (Flujo MVC tradicional - borrado lógico)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario != null)
            {
                usuario.Estatus = false;
                usuario.FechaModificacion = DateTime.Now;
                _context.Update(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        // =========================================================================
        // === NUEVA ACCIÓN DE CAMBIO DE ESTATUS (AJAX/Fetch) ===
        // =========================================================================

        /// <summary>
        /// Realiza el borrado lógico (Estatus = false) del usuario a través de una llamada Fetch/AJAX.
        /// Este endpoint soporta el patrón de confirmación en dos pasos.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus([FromBody] ActionRequestDTO request)
        {
            // 1. Validar la solicitud (Input)
            if (request == null || request.Id <= 0)
            {
                return Json(new AlertResponseDTO
                {
                    Success = false,
                    Title = "Error",
                    Message = "ID no válido.",
                    Icon = "error"
                });
            }

            var usuario = await _context.Usuarios.FindAsync(request.Id);

            if (usuario == null || !usuario.Estatus)
            {
                return Json(new AlertResponseDTO
                {
                    Success = false,
                    Title = "Error",
                    Message = "El usuario no fue encontrado o ya está inactivo.",
                    Icon = "error"
                });
            }

            // 2. Lógica de Confirmación (Primer Paso)
            if (!request.Confirmed)
            {
                // Devolver respuesta (Output) para mostrar la confirmación.
                return Json(new AlertResponseDTO
                {
                    ShowConfirmation = true,
                    Title = "¿Desactivar Usuario?",
                    Message = $"¿Estás seguro de que deseas desactivar al usuario **{usuario.Nombre}**? Esto es un borrado lógico.",
                    Icon = "question",
                    ConfirmButtonText = "Sí, Desactivar"
                });
            }

            // 3. Ejecutar la acción (Segundo Paso: Confirmado)
            try
            {
                usuario.Estatus = false;
                usuario.FechaModificacion = DateTime.Now;
                _context.Update(usuario);
                await _context.SaveChangesAsync();

                // 4. Devolver resultado final de éxito
                return Json(new AlertResponseDTO
                {
                    Success = true,
                    Title = "¡Desactivado!",
                    Message = $"El usuario **{usuario.Nombre}** ha sido desactivado lógicamente.",
                    Icon = "success",
                    RedirectUrl = "/Usuarios/Index"
                });
            }
            catch (Exception ex)
            {
                // 4. Devolver resultado final de error
                return Json(new AlertResponseDTO
                {
                    Success = false,
                    Title = "Error del Servidor",
                    Message = $"Ocurrió un error al intentar desactivar: {ex.Message}",
                    Icon = "error"
                });
            }
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }

        // Helper to prepare select lists safely (prevents null reference when DB empty)
        private void PrepareUserSelectLists(int? selectedUsuarioCrea = null, int? selectedUsuarioModifica = null)
        {
            var areas = _context.Areas.Select(a => new { a.Id, a.Descripcion }).ToList();
            var departamentos = _context.Departamentos.Select(d => new { d.Id, d.Descripcion }).ToList();
            var puestos = _context.Puestos.Select(p => new { p.Id, p.Descripcion }).ToList();
            var tipos = _context.TipoUsuarios.Select(t => new { t.Id, t.Nombre }).ToList();
            var usuarios = _context.Usuarios.Select(u => new { u.Id, u.Nombre }).ToList();

            ViewData["IdArea"] = new SelectList(areas, "Id", "Descripcion");
            ViewData["IdDepartamento"] = new SelectList(departamentos, "Id", "Descripcion");
            ViewData["IdPuesto"] = new SelectList(puestos, "Id", "Descripcion");
            ViewData["IdTipoUsuario"] = new SelectList(tipos, "Id", "Nombre");
            ViewData["IdUsuarioCrea"] = new SelectList(usuarios, "Id", "Nombre", selectedUsuarioCrea);
            ViewData["IdUsuarioModifica"] = new SelectList(usuarios, "Id", "Nombre", selectedUsuarioModifica);
        }

        // AJAX: Buscar usuarios por término para select2/autocomplete (centralizado)
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new List<object>());
            }

            var results = await _context.Usuarios
                .Where(u => u.Estatus && u.Nombre.Contains(term))
                .OrderBy(u => u.Nombre)
                .Select(u => new
                {
                    id = u.Id,
                    text = u.Nombre,
                    areaId = u.IdArea
                })
                .Take(10)
                .ToListAsync();

            return Json(results);
        }

        
    }
}