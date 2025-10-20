using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.DTOs;
using RecursosHumanosWeb.Models.ViewModels;
using System.Data;
using System.Security;
using System.Security.Claims;
using X.PagedList;
using X.PagedList.Extensions;
using System.IO.MemoryMappedFiles;

namespace RecursosHumanosWeb.Controllers
{
    [Authorize]
    public class PermisosController : Controller
    {
        private readonly RecursosHumanosContext _context;
        private readonly IWebHostEnvironment _environment;

        public PermisosController(RecursosHumanosContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string nombreUsuario = null, int? idArea = null, DateTime? delDia = null, DateTime? alDia = null)
        {
            // Obtener información del usuario autenticado desde Claims
            bool isAutorizador = User.HasClaim("TipoUsuario", "1");
            bool isRH = User.HasClaim("TipoUsuario", "2");
            bool isAdministrador = User.HasClaim("TipoUsuario", "4");
            bool isEmpleado = User.HasClaim("TipoUsuario", "3");
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (idUsuario == 0)
            {
                ViewData["ErrorMessage"] = "No se pudo obtener el ID del usuario autenticado.";
                return View(new SearchPermissionsViewModel
                {
                    Permisos = new List<PermisosDTO>(), // Cambia de StaticPagedList a List para que coincida con la propiedad
                });
            }

            // Obtener el corte actual
            var currentCourtId = await _context.Cortes
                .Where(c => c.Estatus)
                .OrderByDescending(c => c.Id)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            // Inicializar la consulta base
            var permisosQuery = _context.Permisos
                .Include(p => p.IdUsuarioSolicitaNavigation)
                .Include(p => p.IdTipoPermisoNavigation)
                .Where(p => p.Estatus == true)
                .AsQueryable();

            // Restricciones por tipo de usuario
            if (isAutorizador)
            {
                var userArea = await _context.Usuarios
                    .Where(u => u.Id == idUsuario)
                    .Select(u => u.IdArea)
                    .FirstOrDefaultAsync();

                permisosQuery = permisosQuery
                    .Where(p => p.IdUsuarioSolicitaNavigation.IdArea == userArea && p.IdCorte == currentCourtId);
            }
            else if (isEmpleado)
            {
                permisosQuery = permisosQuery.Where(p => p.IdUsuarioSolicita == idUsuario);
            }

            // Filtros
            if (!string.IsNullOrEmpty(nombreUsuario) && (isRH || isAdministrador || isAutorizador))
                permisosQuery = permisosQuery.Where(p => p.IdUsuarioSolicitaNavigation.Nombre == nombreUsuario);

            if (idArea.HasValue && (isRH || isAdministrador))
                permisosQuery = permisosQuery.Where(p => p.IdUsuarioSolicitaNavigation.IdArea == idArea.Value);

            if (delDia.HasValue)
                permisosQuery = permisosQuery.Where(p => p.Fecha1 >= delDia.Value);

            if (alDia.HasValue)
            {
                permisosQuery = permisosQuery.Where(p =>
                    p.Fecha1 <= alDia.Value ||
                    (p.Fecha2 != null && p.Fecha2 >= delDia.Value && p.Fecha2 <= alDia.Value));
            }

            // Mapeo a DTO
            var permisos = await permisosQuery
                .Select(p => new PermisosDTO
                {
                    Id = p.Id,
                    Correo = p.IdUsuarioSolicitaNavigation.Correo,
                    Nombre = p.IdUsuarioSolicitaNavigation.Nombre,
                    IdUsuarioSolicita = p.IdUsuarioSolicita,
                    IdTipoPermiso = p.IdTipoPermisoNavigation.Id,
                    FechaCreacion = p.FechaCreacion,
                    Fecha1 = p.Fecha1,
                    Fecha2 = p.Fecha2,
                    Dias = p.Dias,
                    Motivo = p.Motivo,
                    Evidencia = p.Evidencia,
                    FechaAutorizacion = p.FechaAutorizacion,
                    Goce = p.Goce,
                    Revisado = p.Revisado,
                    Estatus = p.Estatus
                })
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync(); // ✅ Traer todo (sin paginar)

            // Usuarios para el filtro
            var usuarios = new List<UsuariosDTO>();
            if (isRH || isAdministrador)
            {
                usuarios = await _context.Usuarios
                    .Where(u => u.Estatus)
                    .Select(u => new UsuariosDTO { Id = u.Id, Nombre = u.Nombre, IdArea = u.IdArea })
                    .OrderBy(u => u.Nombre)
                    .ToListAsync();
            }
            else if (isAutorizador)
            {
                var userArea = await _context.Usuarios
                    .Where(u => u.Id == idUsuario)
                    .Select(u => u.IdArea)
                    .FirstOrDefaultAsync();

                usuarios = await _context.Usuarios
                    .Where(u => u.Estatus && u.IdArea == userArea)
                    .Select(u => new UsuariosDTO { Id = u.Id, Nombre = u.Nombre, IdArea = u.IdArea })
                    .OrderBy(u => u.Nombre)
                    .ToListAsync();
            }

            // Áreas para el filtro
            var areas = (isRH || isAdministrador)
                ? await _context.Areas
                    .Where(a => a.Estatus)
                    .Select(a => new AreasDTO { Id = a.Id, Descripcion = a.Descripcion })
                    .OrderBy(a => a.Descripcion)
                    .ToListAsync()
                : new List<AreasDTO>();

            // ViewModel final
            var model = new SearchPermissionsViewModel
            {
                Permisos = permisos,
                Usuarios = usuarios,
                Areas = areas,
                SelectedUser = nombreUsuario,
                SelectedArea = idArea
            };

            // Pasar datos extra a la vista
            ViewBag.DelDia = delDia;
            ViewBag.AlDia = alDia;
            ViewBag.IsAutorizador = isAutorizador;
            ViewBag.IsRH = isRH;
            ViewBag.IsAdministrador = isAdministrador;
            ViewBag.IsEmpleado = isEmpleado;
            ViewBag.CurrentCourtId = currentCourtId;

            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> GetPermisos(DateTime? delDia, DateTime? alDia, string nombreUsuario, int? idArea, int? idTipoPermisos, int pageNumber = 1, int pageSize = 5)
        {
            // Validar si la solicitud es AJAX
            if (!Request.Headers.ContainsKey("X-Requested-With") || Request.Headers["X-Requested-With"] != "XMLHttpRequest")
            {
                return RedirectToAction("Index");
            }

            // Validar parámetros
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 5;

            // Obtener información del usuario autenticado
            bool isAutorizador = User.HasClaim("TipoUsuario", "1");
            bool isRH = User.HasClaim("TipoUsuario", "2");
            bool isAdministrador = User.HasClaim("TipoUsuario", "4");
            bool isEmpleado = User.HasClaim("TipoUsuario", "3");
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Validar que idUsuario sea válido
            if (idUsuario == 0)
            {
                return Json(new { error = "No se pudo obtener el ID del usuario autenticado." });
            }

            // Obtener el corte actual
            var currentCourtId = await _context.Cortes
                .Where(c => c.Estatus)
                .OrderByDescending(c => c.Id)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            // Construir la consulta
            var query = _context.Permisos
                .Include(p => p.IdUsuarioSolicitaNavigation)
                .Include(p => p.IdTipoPermisoNavigation)
                .AsQueryable();

            // Aplicar restricciones según tipo de usuario
            if (isAutorizador)
            {
                var userArea = await _context.Usuarios
                    .Where(u => u.Id == idUsuario)
                    .Select(u => u.IdArea)
                    .FirstOrDefaultAsync();
                query = query.Where(p => p.IdUsuarioSolicitaNavigation.IdArea == userArea && p.IdCorte == currentCourtId);
            }
            else if (isEmpleado)
            {
                query = query.Where(p => p.IdUsuarioSolicita == idUsuario);
            }

            // Aplicar filtros
            if (delDia.HasValue)
                query = query.Where(p => p.Fecha1 >= delDia.Value);
            if (alDia.HasValue)
                query = query.Where(p => p.Fecha1 <= alDia.Value || (p.Fecha2 != null && p.Fecha2 >= delDia.Value && p.Fecha2 <= alDia.Value));
            if (!string.IsNullOrEmpty(nombreUsuario) && (isRH || isAdministrador || isAutorizador))
            {
                query = query.Where(p => p.IdUsuarioSolicitaNavigation.Nombre == nombreUsuario);
            }
            if (idArea.HasValue && (isRH || isAdministrador))
            {
                query = query.Where(p => p.IdUsuarioSolicitaNavigation.IdArea == idArea.Value);
            }
            if (idTipoPermisos.HasValue)
                query = query.Where(p => p.IdTipoPermiso == idTipoPermisos.Value);

            // Obtener resultados paginados
            var permisos = query
                .Select(p => new PermisosDTO
                {
                    Id = p.Id,
                    Correo = p.IdUsuarioSolicitaNavigation.Correo,
                    Nombre = p.IdUsuarioSolicitaNavigation.Nombre,
                    IdUsuarioSolicita = p.IdUsuarioSolicita,
                    IdTipoPermiso = p.IdTipoPermisoNavigation.Id,
                    FechaCreacion = p.FechaCreacion,
                    Fecha1 = p.Fecha1,
                    Fecha2 = p.Fecha2,
                    Dias = p.Dias,
                    Motivo = p.Motivo,
                    Evidencia = p.Evidencia,
                    FechaAutorizacion = p.FechaAutorizacion,
                    Goce = p.Goce,
                    Estatus = p.Estatus
                })
                .OrderByDescending(p => p.FechaCreacion)
                .ToPagedList(pageNumber, pageSize);

            // Obtener usuarios para el filtro
            var usuarios = new List<UsuariosDTO>();
            if (isRH || isAdministrador)
            {
                usuarios = await _context.Usuarios
                    .Where(u => u.Estatus)
                    .Select(u => new UsuariosDTO { Id = u.Id, Nombre = u.Nombre, IdArea = u.IdArea })
                    .OrderBy(u => u.Nombre)
                    .ToListAsync();
            }
            else if (isAutorizador)
            {
                var userArea = await _context.Usuarios
                    .Where(u => u.Id == idUsuario)
                    .Select(u => u.IdArea)
                    .FirstOrDefaultAsync();
                usuarios = await _context.Usuarios
                    .Where(u => u.Estatus && u.IdArea == userArea)
                    .Select(u => new UsuariosDTO { Id = u.Id, Nombre = u.Nombre, IdArea = u.IdArea })
                    .OrderBy(u => u.Nombre)
                    .ToListAsync();
            }

            // Obtener áreas para el filtro
            var areas = (isRH || isAdministrador)
                ? await _context.Areas
                    .Where(a => a.Estatus)
                    .Select(a => new AreasDTO { Id = a.Id, Descripcion = a.Descripcion })
                    .OrderBy(a => a.Descripcion)
                    .ToListAsync()
                : new List<AreasDTO>();

            // Obtener el IdArea del usuario seleccionado
            int? selectedUserAreaId = null;
            if (!string.IsNullOrEmpty(nombreUsuario) && (isRH || isAdministrador || isAutorizador))
            {
                selectedUserAreaId = await _context.Usuarios
                    .Where(u => u.Nombre == nombreUsuario && u.Estatus)
                    .Select(u => u.IdArea)
                    .FirstOrDefaultAsync();
            }

            return Json(new
            {
                items = permisos,
                totalPages = permisos.PageCount,
                currentPage = permisos.PageNumber,
                pageSize = permisos.PageSize,
                selectedUser = nombreUsuario,
                selectedArea = idArea,
                selectedUserAreaId = selectedUserAreaId,
                usuarios = usuarios,
                areas = areas
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserType = int.Parse(User.FindFirst("TipoUsuario")?.Value ?? "0");

            IQueryable<Permiso> query = _context.Permisos;

            if (currentUserType == 3)
            {
                query = query.Where(p => p.IdUsuarioSolicita == currentUserId);
            }

            var stats = await query
                .GroupBy(p => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Pendientes = g.Count(p => p.Estatus == true),
                    Aprobados = g.Count(p => p.Estatus == false && p.FechaAutorizacion != null),
                    ConGoce = g.Count(p => p.Goce == true),
                    SinGoce = g.Count(p => p.Goce == false),
                    EsteMes = g.Count(p => p.FechaCreacion.HasValue &&
                                           p.FechaCreacion.Value.Month == DateTime.Now.Month &&
                                           p.FechaCreacion.Value.Year == DateTime.Now.Year)
                })
                .FirstOrDefaultAsync();

            return Json(stats ?? new
            {
                Total = 0,
                Pendientes = 0,
                Aprobados = 0,
                ConGoce = 0,
                SinGoce = 0,
                EsteMes = 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PermissionsCreateViewModel model)
        {
            string? evidenciaPath = null;

            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Usuario no autenticado." });
                }

                int idUsuarioCrea = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (idUsuarioCrea == 0)
                {
                    return Json(new { success = false, message = "No se pudo obtener el ID del usuario autenticado." });
                }

                bool isRH = User.HasClaim("TipoUsuario", "2");
                bool isAdministrador = User.HasClaim("TipoUsuario", "4");
                bool isEmpleado = User.HasClaim("TipoUsuario", "3");

                model.IdUsuarioCrea = idUsuarioCrea;
                if (isRH || isAdministrador)
                {
                    model.IdUsuarioSolicita ??= idUsuarioCrea;
                }
                else
                {
                    model.IdUsuarioSolicita = idUsuarioCrea;
                }

                if (!await _context.Usuarios.AnyAsync(u => u.Id == model.IdUsuarioSolicita && u.Estatus))
                {
                    return Json(new { success = false, message = $"El usuario solicitante con ID {model.IdUsuarioSolicita} no es válido o no está activo." });
                }

                if (!model.Fecha1.HasValue)
                {
                    ModelState.AddModelError("Fecha1Date", "La fecha de inicio es obligatoria.");
                }

                if (model.IdTipoPermiso != 3 && !model.Fecha2.HasValue)
                {
                    ModelState.AddModelError("Fecha2Date", "La fecha de fin es obligatoria para este tipo de permiso.");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                    return Json(new { success = false, message = "Errores de validación en el formulario.", errors });
                }

                // Obtener el corte activo
                var corteActivo = await _context.Cortes
                    .Where(c => c.Estatus)
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync();

                if (corteActivo == null)
                {
                    return Json(new { success = false, message = "No se encontró un corte activo." });
                }

                // Obtener el corte futuro
                var corteFuturo = await _context.Cortes
                    .Where(c => c.Id > corteActivo.Id)
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync();

                // Procesar archivo de evidencia
                if (model.EvidenceFile != null)
                {
                    if (model.EvidenceFile.Length > 5 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = "El archivo no debe superar los 5MB." });
                    }

                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "Evidences");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = $"{model.IdUsuarioSolicita}_{Guid.NewGuid()}.pdf";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.EvidenceFile.CopyToAsync(fileStream);
                    }

                    evidenciaPath = uniqueFileName; // Guardar solo el nombre del archivo
                }

                var permisosCreados = new List<int>();
                var currentDateTime = DateTime.Now; // Obtener la fecha y hora actual

                // Lógica de división de permisos
                if (model.IdTipoPermiso == 3 || model.Fecha2 <= corteActivo.Termina)
                {
                    // Caso 1: Permiso de un día o dentro del corte activo
                    var permiso = new Permiso
                    {
                        Motivo = model.Motivo,
                        Fecha1 = model.Fecha1!.Value,
                        Fecha2 = model.IdTipoPermiso != 3 ? model.Fecha2 : null,
                        IdCorte = corteActivo.Id,
                        IdTipoPermiso = model.IdTipoPermiso,
                        IdUsuarioSolicita = model.IdUsuarioSolicita!.Value,
                        IdUsuarioCrea = idUsuarioCrea,
                        IdUsuarioModifica = idUsuarioCrea,
                        Evidencia = evidenciaPath,
                        FechaCreacion = currentDateTime, // Asignar fecha de creación
                        FechaModificacion = currentDateTime // Asignar fecha de modificación
                    };

                    _context.Permisos.Add(permiso);
                    await _context.SaveChangesAsync();
                    permisosCreados.Add(permiso.Id);
                }
                else if (model.IdTipoPermiso is 2 or 4 or 5 && model.Fecha2 > corteActivo.Termina && corteFuturo != null)
                {
                    // Caso 2: Permiso cruza el corte activo
                    // Primer permiso (hasta el final del corte activo)
                    var permiso1 = new Permiso
                    {
                        Motivo = model.Motivo,
                        Fecha1 = model.Fecha1!.Value,
                        Fecha2 = corteActivo.Termina,
                        IdCorte = corteActivo.Id,
                        IdTipoPermiso = model.IdTipoPermiso,
                        IdUsuarioSolicita = model.IdUsuarioSolicita!.Value,
                        IdUsuarioCrea = idUsuarioCrea,
                        IdUsuarioModifica = idUsuarioCrea,
                        Evidencia = evidenciaPath,
                        FechaCreacion = currentDateTime, // Asignar fecha de creación
                        FechaModificacion = currentDateTime // Asignar fecha de modificación
                    };

                    // Segundo permiso (desde el inicio del corte futuro)
                    var fechaSiguiente = corteActivo.Termina!.Value.AddDays(1);
                    var permiso2 = new Permiso
                    {
                        Motivo = model.Motivo,
                        Fecha1 = fechaSiguiente,
                        Fecha2 = model.Fecha2,
                        IdCorte = corteFuturo.Id,
                        IdTipoPermiso = model.IdTipoPermiso,
                        IdUsuarioSolicita = model.IdUsuarioSolicita!.Value,
                        IdUsuarioCrea = idUsuarioCrea,
                        IdUsuarioModifica = idUsuarioCrea,
                        Evidencia = evidenciaPath,
                        FechaCreacion = currentDateTime, // Asignar fecha de creación
                        FechaModificacion = currentDateTime // Asignar fecha de modificación
                    };

                    _context.Permisos.AddRange(permiso1, permiso2);
                    await _context.SaveChangesAsync();
                    permisosCreados.Add(permiso1.Id);
                    permisosCreados.Add(permiso2.Id);
                }
                else
                {
                    // Caso 3: No hay corte futuro para el permiso
                    return Json(new { success = false, message = "No se encontró un corte futuro para el permiso." });
                }

                if (!permisosCreados.Any())
                {
                    if (evidenciaPath != null && System.IO.File.Exists(Path.Combine(_environment.WebRootPath, "Evidences", evidenciaPath)))
                    {
                        System.IO.File.Delete(Path.Combine(_environment.WebRootPath, "Evidences", evidenciaPath));
                    }
                    return Json(new { success = false, message = "Error: No se pudo crear el permiso. No se encontraron permisos generados." });
                }

                int newId = permisosCreados.First();
                return Json(new { success = true, message = "Permiso creado exitosamente", id = newId, ids = permisosCreados });
            }
            catch (Exception ex)
            {
                if (evidenciaPath != null && System.IO.File.Exists(Path.Combine(_environment.WebRootPath, "Evidences", evidenciaPath)))
                {
                    System.IO.File.Delete(Path.Combine(_environment.WebRootPath, "Evidences", evidenciaPath));
                }
                return Json(new { success = false, message = $"Error al crear el permiso: {ex.Message}" });
            }
        }

        private async Task PrepareCreateEditViewBag()
        {
            // Get authenticated user's information
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            bool isRH = User.HasClaim("TipoUsuario", "2");
            bool isAdministrador = User.HasClaim("TipoUsuario", "4");
            bool isEmpleado = User.HasClaim("TipoUsuario", "3");

            // Set ViewBag properties
            ViewBag.UserType = User.FindFirst("TipoUsuario")?.Value;
            ViewBag.IdUsuario = idUsuario;
            ViewBag.IsRH = isRH;
            ViewBag.IsAdministrador = isAdministrador;
            ViewBag.IsEmpleado = isEmpleado;

            // Fetch the authenticated user's details
            var currentUser = await _context.Usuarios
                .Where(u => u.Id == idUsuario && u.Estatus)
                .Select(u => new { u.Id, u.Nombre })
                .FirstOrDefaultAsync();

            ViewBag.CurrentUserName = currentUser?.Nombre ?? "Desconocido";

            // Fetch users list for RH and Administrators
            if (isRH || isAdministrador)
            {
                ViewBag.Usuarios = await _context.Usuarios
                    .Where(u => u.Estatus)
                    .Select(u => new { u.Id, u.Nombre })
                    .OrderBy(u => u.Nombre)
                    .ToListAsync();
            }
            else
            {
                ViewBag.Usuarios = new List<dynamic>();
            }

            // Hardcoded areas (as per your original code)
            ViewBag.Areas = new List<dynamic>
            {
                new { Id = 1, Nombre = "SUBDIRECCION DE SERVICIOS ADMINISTRATIVOS" },
                new { Id = 2, Nombre = "DIRECCION GENERAL" },
                new { Id = 3, Nombre = "DIRECCION DE PLANEACION Y VINCULACION" },
                new { Id = 4, Nombre = "DIRECCION ACADEMICA" }
            };
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var permiso = await _context.Permisos
                .Include(p => p.IdUsuarioSolicitaNavigation)
                .Include(p => p.IdTipoPermisoNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (permiso == null)
                return NotFound();

            if (!CanAccessPermiso(permiso))
                return Forbid();

            return View(permiso);
        }

        [HttpGet]
        public IActionResult Create()
        {
            // Verificar autenticación
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            // Obtener roles del usuario
            ViewBag.IsRH = User.HasClaim("TipoUsuario", "2");
            ViewBag.IsAdministrador = User.HasClaim("TipoUsuario", "4");
            ViewBag.IsEmpleado = User.HasClaim("TipoUsuario", "3");

            // Inicializar ViewBag.Usuarios con usuarios activos
            try
            {
                var usuarios = _context.Usuarios
                    .Where(u => u.Estatus)
                    .Select(u => new { u.Id, u.Nombre }) // Use Nombre as in the query
                    .OrderBy(u => u.Nombre)
                    .ToList();

                // Crear SelectList con el campo correcto
                ViewBag.Usuarios = new SelectList(
                    usuarios,
                    "Id",
                    "Nombre" // Corrected from NombreCompleto
                );
            }
            catch (Exception ex)
            {
              
                ViewBag.Usuarios = new SelectList(new List<SelectListItem>(), "Value", "Text");
            }

            return View(new PermissionsCreateViewModel());
        }
        private PermissionsCreateViewModel MapToEditViewModel(Permiso permiso)
        {
            return new PermissionsCreateViewModel
            {
                Id = permiso.Id,
                IdUsuarioSolicita = permiso.IdUsuarioSolicita,
                IdTipoPermiso = permiso.IdTipoPermiso,
                Fecha1Date = permiso.Fecha1.Date,
                Hora1Time = permiso.Fecha1.ToString("HH:mm"),
                Fecha2Date = permiso.Fecha2?.Date,
                Hora2Time = permiso.Fecha2?.ToString("HH:mm"),
                Motivo = permiso.Motivo,
                Evidencia = permiso.Evidencia,
                Goce = permiso.Goce
            };
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var permiso = await _context.Permisos
                .Include(p => p.IdTipoPermisoNavigation)
                .Include(p => p.IdUsuarioSolicitaNavigation)
                .Include(p => p.IdCorteNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (permiso == null)
            {
                return NotFound();
            }

            if (permiso.IdCorteNavigation != null && !permiso.IdCorteNavigation.Estatus)
            {
                TempData["ErrorMessage"] = "No se puede modificar un permiso asociado a un corte no vigente.";
                return RedirectToAction("Index");
            }

            if (permiso.Revisado == true)
            {
                TempData["ErrorMessage"] = "No se puede modificar un permiso que ya fue revisado.";
                return RedirectToAction("Index");
            }

            if (!CanAccessPermiso(permiso))
            {
                return Forbid();
            }

            PrepareCreateEditViewBag();
            return View(MapToEditViewModel(permiso));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PermissionsCreateViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var originalPermiso = await _context.Permisos
                .Include(p => p.IdCorteNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (originalPermiso == null)
            {
                return NotFound();
            }

            if (originalPermiso.IdCorteNavigation != null && !originalPermiso.IdCorteNavigation.Estatus)
            {
                TempData["ErrorMessage"] = "No se puede modificar un permiso asociado a un corte no vigente.";
                return RedirectToAction("Index");
            }

            if (originalPermiso.Revisado == true)
            {
                TempData["ErrorMessage"] = "No se puede modificar un permiso que ya fue revisado.";
                return RedirectToAction("Index");
            }

            if (!CanAccessPermiso(originalPermiso))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userId = ViewData["IdUsuario"]?.ToString();
                    bool isAutorizador = Convert.ToBoolean(ViewData["IsAutorizador"]);
                    bool isAdministrador = Convert.ToBoolean(ViewData["IsAdministrador"]);

                    if (string.IsNullOrEmpty(userId))
                    {
                        ModelState.AddModelError(string.Empty, "No se pudo identificar al usuario en sesión.");
                        return View(model);
                    }

                    var permiso = new Permiso
                    {
                        Id = model.Id ?? originalPermiso.Id,
                        IdUsuarioSolicita = originalPermiso.IdUsuarioSolicita,
                        IdTipoPermiso = originalPermiso.IdTipoPermiso,
                        IdCorte = originalPermiso.IdCorte,
                        Fecha1 = model.Fecha1 ?? originalPermiso.Fecha1,
                        Fecha2 = model.Fecha2 ?? originalPermiso.Fecha2,
                        Motivo = model.Motivo,
                        Evidencia = originalPermiso.Evidencia,
                        Goce = (bool)((isAdministrador || isAutorizador) ? model.Goce : originalPermiso.Goce),
                        IdUsuarioModifica = int.Parse(userId),
                        FechaModificacion = DateTime.Now,
                        Estatus = originalPermiso.Estatus,
                        Revisado = originalPermiso.Revisado,
                        FechaCreacion = originalPermiso.FechaCreacion,
                        FechaAutorizacion = originalPermiso.FechaAutorizacion,
                        IdUsuarioAutoriza = originalPermiso.IdUsuarioAutoriza,
                        IdUsuarioCrea = originalPermiso.IdUsuarioCrea
                    };

                    if (permiso.Fecha2.HasValue && permiso.Fecha1 != null)
                    {
                        permiso.Dias = (permiso.Fecha2.Value.Date - permiso.Fecha1.Date).Days + 1;
                    }

                    if (model.EvidenceFile != null && model.EvidenceFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_environment.WebRootPath, "Evidences");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string oldFileName = originalPermiso.Evidencia?.TrimStart('/').Replace('/', '\\');
                        string oldFilePath = !string.IsNullOrEmpty(oldFileName) ? Path.Combine(_environment.WebRootPath, oldFileName) : null;

                        if (model.EvidenceFile.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("EvidenceFile", "El archivo no debe superar los 5MB.");
                            return View(model);
                        }

                        if (model.EvidenceFile.ContentType != "application/pdf")
                        {
                            ModelState.AddModelError("EvidenceFile", "Solo se permiten archivos PDF.");
                            return View(model);
                        }

                        string newFilePath = !string.IsNullOrEmpty(oldFileName) ? Path.Combine(uploadsFolder, oldFileName) : Path.Combine(uploadsFolder, $"{permiso.IdUsuarioSolicita}_{Guid.NewGuid()}.pdf");
                        using (var fileStream = new FileStream(newFilePath, FileMode.Create))
                        {
                            await model.EvidenceFile.CopyToAsync(fileStream);
                        }
                    }

                    _context.Update(permiso);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PermisoExists(model.Id ?? id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            PrepareCreateEditViewBag();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id, string comments = "")
        {
            var currentUserType = int.Parse(User.FindFirst("TipoUsuario")?.Value ?? "0");

            if (currentUserType != 1 && currentUserType != 4)
                return Forbid();

            var permiso = await _context.Permisos.FindAsync(id);
            if (permiso == null)
                return NotFound();

            permiso.Estatus = false; // Aprobado
            permiso.FechaAutorizacion = DateTime.Now;
            permiso.IdUsuarioAutoriza = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (!string.IsNullOrEmpty(comments))
            {
                permiso.Motivo = comments;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Permiso aprobado exitosamente" });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var permiso = await _context.Permisos
                .Include(p => p.IdUsuarioSolicitaNavigation)
                .Include(p => p.IdTipoPermisoNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (permiso == null)
                return NotFound();

            if (!CanAccessPermiso(permiso))
                return Forbid();

            return View(permiso);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var permiso = await _context.Permisos.FindAsync(id);
            if (permiso == null)
                return NotFound();

            if (!CanAccessPermiso(permiso))
                return Forbid();

            // Realizar borrado lógico cambiando el estatus a false
            permiso.Estatus = false;
            _context.Permisos.Update(permiso);
            await _context.SaveChangesAsync();

            // Opcional: Agregar mensaje de éxito
            TempData["SuccessMessage"] = "El permiso ha sido desactivado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        private bool PermisoExists(int id)
        {
            return _context.Permisos.Any(e => e.Id == id);
        }

        private bool CanAccessPermiso(Permiso permiso)
        {
            if (permiso == null)
                return false;

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserType = int.Parse(User.FindFirst("TipoUsuario")?.Value ?? "0");

            switch (currentUserType)
            {
                case 1: // Autorizador
                case 2: // Recursos Humanos
                case 4: // Administrador
                    return true;

                case 3: // Trabajadores
                    return permiso.IdUsuarioSolicita == currentUserId;

                default:
                    return false;
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(DateTime? Fecha1 = null, DateTime? Fecha2 = null, string? NombreUsuario = null, int? IdArea = null, int? IdTipoPermiso = null)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentUserType = int.Parse(User.FindFirst("TipoUsuario")?.Value ?? "0");
            var currentUserArea = int.Parse(User.FindFirst("Area")?.Value ?? "0");

            if (!Fecha1.HasValue)
                Fecha1 = DateTime.Today.AddDays(-30);
            if (!Fecha2.HasValue)
                Fecha2 = DateTime.Today;

            int? filtroUsuarioId = null;
            int? filtroArea = null;

            switch (currentUserType)
            {
                case 1:
                case 2:
                case 4:
                    if (!string.IsNullOrEmpty(NombreUsuario))
                    {
                        var usuario = await _context.Usuarios
                            .FirstOrDefaultAsync(u => u.Nombre == NombreUsuario);
                        if (usuario != null)
                            filtroUsuarioId = usuario.Id;
                    }
                    else if (IdArea.HasValue)
                    {
                        filtroArea = IdArea.Value;
                    }
                    break;

                case 3:
                    filtroUsuarioId = currentUserId;
                    filtroArea = currentUserArea;
                    break;
            }

            var permisos = await _context.SearchPermissionsDTO
                .FromSqlRaw("EXEC dbo.SearchPermissions @Fecha1 = {0}, @Fecha2 = {1}, @NombreUsuario = {2}, @IdUsuario = {3}, @IdArea = {4}, @IdTipoPermiso = {5}",
                    Fecha1, Fecha2, NombreUsuario, filtroUsuarioId, filtroArea, IdTipoPermiso)
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = permisos,
                message = $"Se encontraron {permisos.Count} registros para exportar"
            });
        }



    }
}