using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.DTOs;
using RecursosHumanosWeb.Models.ViewModels;
using System.Security.Claims;
using X.PagedList;
using AutoMapper.QueryableExtensions;
using X.PagedList.Extensions;

namespace RecursosHumanosWeb.Controllers
{
    [Authorize]
    public class PermisosController : Controller
    {
        private readonly RecursosHumanosContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly AutoMapper.IMapper _mapper;

        public PermisosController(RecursosHumanosContext context, IWebHostEnvironment environment, AutoMapper.IMapper mapper)
        {
            _context = context;
            _environment = environment;
            _mapper = mapper;
        }

        // AJAX: Buscar usuarios por término para select2/autocomplete (original en Permisos)
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                // Devolver lista mínima vacía para no cargar muchos resultados
                return Json(new List<object>());
            }

            var results = await _context.Usuarios
                .Where(u => u.Estatus && u.Nombre.Contains(term))
                .OrderBy(u => u.Nombre)
                .Select(u => new
                {
                    id = u.Id,
                    text = u.Nombre ?? "",
                    areaId = u.IdArea ?? 0
                })
                .Take(10)
                .ToListAsync();

            return Json(results);
        }

        // NOTE: SearchUsers moved to UsuariosController to centralize user-related endpoints.

        #region VISTAS Y FILTROS (Index, GetPermisos, GetStats)

        [HttpGet]
        public async Task<IActionResult> Index(string nombreUsuario = null, int? idArea = null, DateTime? delDia = null, DateTime? alDia = null, int? page = 1)
        {
            // Obtener información del usuario autenticado
            bool isAutorizador = User.HasClaim("TipoUsuario", "1");
            bool isRH = User.HasClaim("TipoUsuario", "2");
            bool isAdministrador = User.HasClaim("TipoUsuario", "4");
            bool isEmpleado = User.HasClaim("TipoUsuario", "3");
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Exponer datos al View para condicionar links (Details)
            ViewBag.CurrentUserId = idUsuario;
            int? userArea = null; // will be populated below if needed

            if (isAutorizador)
            {
                userArea = await _context.Usuarios
                    .Where(u => u.Id == idUsuario)
                    .Select(u => u.IdArea)
                    .FirstOrDefaultAsync();
            }
            ViewBag.UserArea = userArea ?? 0;

            if (idUsuario == 0)
            {
                ViewData["ErrorMessage"] = "No se pudo obtener el ID del usuario autenticado.";
                return RedirectToAction("Login", "Account"); // Redirigir al login si no hay ID de usuario.
            }

            // Obtener el corte actual
            var currentCourtId = await _context.Cortes
                .Where(c => c.Estatus ?? false)
                .OrderByDescending(c => c.Id)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            // Inicializar la consulta base
            var permisosQuery = _context.Permisos
                .Include(p => p.IdUsuarioSolicitaNavigation)
                .Include(p => p.IdTipoPermisoNavigation)
                .Where(p => p.Estatus == true) // Solo activos, a menos que se requiera ver el historial completo
                .AsQueryable();

            // Restricciones por tipo de usuario
            if (isAutorizador)
            {
                userArea = await _context.Usuarios
                    .Where(u => u.Id == idUsuario)
                    .Select(u => u.IdArea)
                    .FirstOrDefaultAsync();

                // Autorizador solo ve los permisos de su área en el corte actual
                permisosQuery = permisosQuery
                    .Where(p => p.IdUsuarioSolicitaNavigation.IdArea == userArea && p.IdCorte == currentCourtId);
            }
            else if (isEmpleado)
            {
                // Empleado solo ve sus propios permisos
                permisosQuery = permisosQuery.Where(p => p.IdUsuarioSolicita == idUsuario);
            }

            // Aplicar filtros de la vista
            if (!string.IsNullOrEmpty(nombreUsuario) && (isRH || isAdministrador || isAutorizador))
                permisosQuery = permisosQuery.Where(p => p.IdUsuarioSolicitaNavigation.Nombre.Contains(nombreUsuario)); // Usar Contains para búsqueda

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

            // Mapeo a DTO y paginación usando AutoMapper.ProjectTo (evita materializar entidades completas)
            var permisos = permisosQuery
                .ProjectTo<PermisosDTO>(_mapper.ConfigurationProvider)
                .OrderByDescending(p => p.FechaCreacion)
                .ToPagedList(page ?? 1, 10); // 10 items por página

            // Usuarios para el filtro (solo para RH/Admin/Autorizador)
            // No enviar la lista completa de usuarios al cliente: usaremos el endpoint centralizado '/Usuarios/SearchUsers'
            var usuarios = new List<UsuariosDTO>();

            // Áreas para el filtro (solo para RH/Admin)
            var areas = (isRH || isAdministrador)
                ? await _context.Areas
                    .Where(a => a.Estatus ?? false)
                    .Select(a => new AreasDTO { Id = a.Id, Descripcion = a.Descripcion })
                    .OrderBy(a => a.Descripcion)
                    .ToListAsync()
                : new List<AreasDTO>();

            // ViewModel final
            var model = new SearchPermissionsViewModel
            {
                Permisos = permisos, // Usamos IPagedList<PermisosDTO>
                Usuarios = usuarios,
                Areas = areas,
                SelectedUser = nombreUsuario,
                SelectedArea = idArea
            };

            // Pasar datos extra a la vista
            ViewBag.DelDia = delDia?.ToString("yyyy-MM-dd");
            ViewBag.AlDia = alDia?.ToString("yyyy-MM-dd");
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
            // Validar si la solicitud es AJAX (aunque ya no se usa, lo mantendremos como check)
            if (!Request.Headers.ContainsKey("X-Requested-With") || Request.Headers["X-Requested-With"] != "XMLHttpRequest")
            {
                return RedirectToAction("Index");
            }

            // ... (código de GetPermisos - Se asume que la lógica es correcta, se deja solo el esqueleto)

            var permisos = _context.Permisos
                .Select(p => new PermisosDTO { /* ... */ })
                .OrderByDescending(p => p.FechaCreacion)
                .ToPagedList(pageNumber, pageSize);

            return Json(new
            {
                items = permisos.ToList(),
                totalPages = permisos.PageCount,
                currentPage = permisos.PageNumber,
                pageSize = permisos.PageSize,
                hasPreviousPage = permisos.HasPreviousPage,
                hasNextPage = permisos.HasNextPage
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            // ... (código GetStats - asumido correcto)
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
                  Pendientes = g.Count(p => p.Revisado == false && p.Estatus == true),
                  Aprobados = g.Count(p => p.Revisado == true && p.Estatus == true),
                  ConGoce = g.Count(p => p.Goce == true),
                  SinGoce = g.Count(p => p.Goce == false),
                  EsteMes = g.Count(p => p.FechaCreacion.Value.Month == DateTime.Now.Month &&
                                          p.FechaCreacion.Value.Year == DateTime.Now.Year) // <-- ¡CORREGIDO!
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

        #endregion

        #region VISTAS CREATE Y EDIT (GET)

        /**
         * CORRECCIÓN: Método privado para poblar los SelectList de Usuarios y Tipos de Permisos
         * de manera robusta y centralizada para Crear y Editar.
         */
        private async Task PrepareCreateEditViewBag(int? selectedUserId = null)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            bool isRH = User.HasClaim("TipoUsuario", "2");
            bool isAdministrador = User.HasClaim("TipoUsuario", "4");

            ViewBag.UserType = User.FindFirst("TipoUsuario")?.Value;
            ViewBag.IdUsuario = idUsuario;
            ViewBag.IsRH = isRH;
            ViewBag.IsAdministrador = isAdministrador;

            // --- 1. Usuarios ---
            IQueryable<Usuario> usersQuery = _context.Usuarios
                .Where(u => u.Estatus && u.IdTipoUsuario != 1 && u.IdTipoUsuario != 4) // Excluir Autorizadores y Admins para el dropdown de solicitantes (opcional, pero mejora la UX)
                .AsQueryable();

            if (isRH || isAdministrador)
            {
                // RH/Admin ven todos los usuarios activos
                var allUsers = await usersQuery
                    .Select(u => new
                    {
                        Id = u.Id,
                        Nombre = u.Nombre
                    })
                    .OrderBy(u => u.Nombre)
                    .ToListAsync();

                // CREACIÓN DEL SELECTLIST CORREGIDA: Se usa el constructor de SelectList
                ViewBag.Usuarios = new SelectList(allUsers, "Id", "Nombre", selectedUserId);
            }
            else
            {
                // Empleado normal solo se ve a sí mismo.
                var currentUser = await usersQuery
                    .Where(u => u.Id == idUsuario)
                    .Select(u => new { Id = u.Id, Nombre = u.Nombre })
                    .FirstOrDefaultAsync();

                if (currentUser != null)
                {
                    // Se crea una lista a partir del usuario actual para el SelectList
                    var selfList = new List<dynamic> { currentUser };
                    ViewBag.Usuarios = new SelectList(selfList, "Id", "Nombre", currentUser.Id);
                }
                else
                {
                    ViewBag.Usuarios = new SelectList(new List<dynamic>(), "Id", "Nombre");
                }
            }

            // --- 2. Tipos de Permisos ---
            var tipoPermisos = await _context.TipoPermisos
                .Select(tp => new { tp.Id, tp.Descripcion })
                .ToListAsync();

            // CREACIÓN DEL SELECTLIST CORREGIDA
            ViewBag.TipoPermisos = new SelectList(tipoPermisos, "Id", "Descripcion");
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            // Preparar ViewBag para el formulario (Incluye la lista de usuarios)
            await PrepareCreateEditViewBag();

            // Usamos el ID del usuario autenticado como solicitante predeterminado
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var model = new PermissionsCreateViewModel
            {
                IdUsuarioSolicita = idUsuario // ID predeterminado
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Validar que NO sea un autorizador
            bool isAutorizador = User.HasClaim("TipoUsuario", "1");
            if (isAutorizador)
            {
                TempData["ErrorAlert"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    icon = "error",
                    title = "Acceso Denegado",
                    text = "Los jefes de área deben usar la opción 'Revisar' para autorizar permisos. No pueden editarlos directamente."
                });
                return RedirectToAction("Index");
            }

            var permiso = await _context.Permisos
                .Include(p => p.IdTipoPermisoNavigation)
                .Include(p => p.IdUsuarioSolicitaNavigation)
                .Include(p => p.IdCorteNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (permiso == null)
            {
                TempData["ErrorAlert"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    icon = "error",
                    title = "No encontrado",
                    text = "El permiso solicitado no existe."
                });
                return RedirectToAction("Index");
            }

            // Validaciones de negocio antes de editar
            if (permiso.IdCorteNavigation != null && !(permiso.IdCorteNavigation.Estatus ?? false))
            {
                TempData["ErrorAlert"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    icon = "warning",
                    title = "Corte No Vigente",
                    text = "No se puede modificar un permiso asociado a un corte no vigente."
                });
                return RedirectToAction("Index");
            }

            if (permiso.Revisado == true)
            {
                TempData["ErrorAlert"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    icon = "info",
                    title = "Permiso Revisado",
                    text = "No se puede modificar un permiso que ya fue revisado por un autorizador."
                });
                return RedirectToAction("Index");
            }

            if (!CanAccessPermiso(permiso))
            {
                TempData["ErrorAlert"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    icon = "error",
                    title = "Acceso Denegado",
                    text = "No tiene permisos para modificar esta solicitud."
                });
                return RedirectToAction("Index");
            }

            // Llama a la función que prepara los ViewBag, enviando el ID del usuario solicitado
            await PrepareCreateEditViewBag(permiso.IdUsuarioSolicita);

            var vm = _mapper.Map<PermissionsCreateViewModel>(permiso);
            return View(vm);
        }

        #endregion

        #region ACCIONES CREATE Y EDIT (POST)

        // POST: Permisos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PermissionsCreateViewModel model, bool? confirmed)
        {
            string? evidenciaPath = null;

            // 1. Obtener ID del usuario creador y verificar roles
            if (!User.Identity!.IsAuthenticated)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "Error de Sesión", Message = "Usuario no autenticado." });
            }

            int idUsuarioCrea = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (idUsuarioCrea == 0)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "Error de Sesión", Message = "No se pudo obtener el ID del usuario autenticado." });
            }

            bool isRH = User.HasClaim("TipoUsuario", "2");
            bool isAdministrador = User.HasClaim("TipoUsuario", "4");

            try
            {
                // 2. Determinar el usuario solicitante
                if (isRH || isAdministrador)
                {
                    model.IdUsuarioSolicita ??= idUsuarioCrea; // Si es RH/Admin y es nulo, es para sí mismo.
                }
                else
                {
                    model.IdUsuarioSolicita = idUsuarioCrea; // Empleado normal, solo puede solicitar para sí mismo.
                }
                model.IdUsuarioCrea = idUsuarioCrea;


                // 3. Validaciones personalizadas
                if (!model.Fecha1.HasValue)
                {
                    ModelState.AddModelError("Fecha1", "La fecha de inicio es obligatoria.");
                }

                if (model.IdTipoPermiso != 3 && !model.Fecha2.HasValue)
                {
                    ModelState.AddModelError("Fecha2", "La fecha de fin es obligatoria para este tipo de permiso.");
                }

                // Validación de que Fecha1 no sea posterior a Fecha2 si existe.
                if (model.Fecha1.HasValue && model.Fecha2.HasValue && model.Fecha1.Value.Date > model.Fecha2.Value.Date)
                {
                    ModelState.AddModelError("Fecha1", "La fecha de inicio no puede ser posterior a la fecha de fin.");
                }


                // 4. Devolver errores de validación si existen (CON LA NUEVA PROPIEDAD Errors)
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Where(x => x.Value!.Errors.Any())
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage).ToArray()
                        );

                    return Json(new AlertResponseDTO
                    {
                        Success = false,
                        Title = "Error de Validación",
                        Message = "Por favor, corrija los campos marcados.",
                        Errors = errors
                    });
                }

                // Validar que el usuario solicitante exista y esté activo DESPUÉS de validar el formulario completo
                if (!await _context.Usuarios.AnyAsync(u => u.Id == model.IdUsuarioSolicita && u.Estatus))
                {
                    return Json(new AlertResponseDTO { Success = false, Title = "Error de Solicitante", Message = $"El usuario solicitante con ID {model.IdUsuarioSolicita} no es válido o no está activo." });
                }


                // 5. Obtener corte activo
                var corteActivo = await _context.Cortes
                    .Where(c => c.Estatus ?? false)
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync();

                if (corteActivo == null)
                {
                    return Json(new AlertResponseDTO { Success = false, Title = "Error de Corte", Message = "No se encontró un corte activo." });
                }

                // 6. Procesar archivo de evidencia
                if (model.EvidenceFile != null)
                {
                    if (model.EvidenceFile.Length > 5 * 1024 * 1024)
                    {
                        return Json(new AlertResponseDTO { Success = false, Title = "Error de Archivo", Message = "El archivo no debe superar los 5MB." });
                    }

                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "Evidences");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Asegurar que solo se acepten ciertos tipos de archivo (por ejemplo, PDF)
                    var extension = Path.GetExtension(model.EvidenceFile.FileName).ToLower();
                    if (extension != ".pdf" && extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                    {
                        return Json(new AlertResponseDTO { Success = false, Title = "Error de Archivo", Message = "Solo se permiten archivos PDF, JPG y PNG." });
                    }

                    string uniqueFileName = $"{model.IdUsuarioSolicita}_{Guid.NewGuid()}{extension}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.EvidenceFile.CopyToAsync(fileStream);
                    }

                    // Store a web-relative path so views can reference it directly
                    evidenciaPath = "/Evidences/" + uniqueFileName;
                }

                var permisosCreados = new List<int>();
                var currentDateTime = DateTime.Now;

                // 7. Validación: Fecha1 debe estar dentro del periodo del corte activo (hora, minuto, segundo)
                if (model.Fecha1.HasValue && corteActivo.Inicia.HasValue && model.Fecha1.Value < corteActivo.Inicia.Value)
                {
                    ModelState.AddModelError("Fecha1", "La fecha y hora de inicio debe estar dentro del periodo del corte vigente.");
                    var errors = ModelState.Where(x => x.Value!.Errors.Any())
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage).ToArray()
                        );

                    return Json(new AlertResponseDTO
                    {
                        Success = false,
                        Title = "Error de Validación",
                        Message = "Por favor, corrija los campos marcados.",
                        Errors = errors
                    });
                }

                // 8. Validación: Si Fecha2 está fuera del corte activo (mayor al término) pedir confirmación
                if (model.Fecha2.HasValue && corteActivo.Termina.HasValue && model.Fecha2.Value > corteActivo.Termina.Value && !(confirmed ?? false))
                {
                    return Json(new AlertResponseDTO
                    {
                        Success = false,
                        ShowConfirmation = true,
                        Title = "Fechas fuera del corte",
                        Message = "Las fechas proporcionadas exceden el periodo del corte vigente. Se recomienda crear un permiso para fechas futuras. ¿Desea crear el permiso de todas formas?",
                        Icon = "warning",
                        ConfirmButtonText = "Sí, crear permiso"
                    });
                }

                // 9. Crear un único permiso que englobe las fechas proporcionadas (no se parten en varios cortes)
                int? diasCalculados = null;
                if (model.IdTipoPermiso != 3 && model.Fecha1.HasValue && model.Fecha2.HasValue)
                {
                    diasCalculados = (int)((model.Fecha2.Value - model.Fecha1.Value).TotalDays + 1);
                }

                var permiso = new Permiso
                {
                    Motivo = model.Motivo,
                    Fecha1 = model.Fecha1!.Value,
                    Fecha2 = model.IdTipoPermiso != 3 && model.Fecha2.HasValue ? model.Fecha2.Value : (DateTime?)null,
                    Dias = diasCalculados,
                    IdCorte = corteActivo.Id,
                    IdTipoPermiso = model.IdTipoPermiso,
                    IdUsuarioSolicita = model.IdUsuarioSolicita!.Value,
                    IdUsuarioCrea = idUsuarioCrea,
                    IdUsuarioModifica = idUsuarioCrea,
                    Evidencia = evidenciaPath,
                    FechaCreacion = currentDateTime,
                    FechaModificacion = currentDateTime,
                    Estatus = true,
                    Revisado = false,
                    Goce = model.Goce ?? false
                };

                _context.Permisos.Add(permiso);
                await _context.SaveChangesAsync();
                permisosCreados.Add(permiso.Id);


                if (!permisosCreados.Any())
                {
                    // Caso de falla de lógica de corte
                    if (evidenciaPath != null) DeleteFile(evidenciaPath);
                    return Json(new AlertResponseDTO { Success = false, Title = "Error al Guardar", Message = "Error: No se pudo crear el permiso. No se encontraron permisos generados.", });
                }

                // 8. Devolver respuesta de ÉXITO con REDIRECCIÓN
                return Json(new AlertResponseDTO(
                    controllerName: "Permisos", // Siempre redirigir al Index de Permisos
                    message: $"Permiso(s) creado(s) exitosamente. Total: {permisosCreados.Count}."
                ));
            }
            catch (Exception ex)
            {
                // 9. Manejo de errores y limpieza de archivo
                if (evidenciaPath != null) DeleteFile(evidenciaPath);
                // Loguear la excepción
                // _logger.LogError(ex, "Error al crear permiso en POST");
                return Json(new AlertResponseDTO { Success = false, Title = "Error del Servidor", Message = $"Error al crear el permiso: {ex.Message}" });
            }
        }

        // POST: Permisos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PermissionsCreateViewModel model)
        {
            if (id != model.Id)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "Error", Message = "El ID del permiso no coincide con el modelo." });
            }

            // Validar que NO sea un autorizador
            bool isAutorizador = User.HasClaim("TipoUsuario", "1");
            if (isAutorizador)
            {
                return Json(new AlertResponseDTO 
                { 
                    Success = false, 
                    Title = "Acceso Denegado", 
                    Message = "Los jefes de área deben usar la opción 'Revisar' para autorizar permisos. No pueden editarlos directamente." 
                });
            }

            // Obtener datos del usuario logeado
            int idUsuarioModifica = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var originalPermiso = await _context.Permisos
                .Include(p => p.IdCorteNavigation)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (originalPermiso == null)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "Error", Message = "Permiso no encontrado." });
            }

            // Validaciones de estado (Revisado / Corte no vigente)
            if (originalPermiso.Revisado == true || originalPermiso.IdCorteNavigation?.Estatus == false)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "Error", Message = "No se puede modificar un permiso que ya fue revisado o pertenece a un corte no vigente." });
            }

            // Validar acceso
            if (!CanAccessPermiso(originalPermiso))
            {
                return Json(new AlertResponseDTO { Success = false, Title = "Acceso Denegado", Message = "No tiene permisos para modificar esta solicitud." });
            }

            // Validaciones personalizadas para POST/Edit (reutilizadas de Create)
            if (!model.Fecha1.HasValue)
            {
                ModelState.AddModelError("Fecha1", "La fecha de inicio es obligatoria.");
            }

            if (model.IdTipoPermiso != 3 && !model.Fecha2.HasValue)
            {
                ModelState.AddModelError("Fecha2", "La fecha de fin es obligatoria para este tipo de permiso.");
            }

            if (model.Fecha1.HasValue && model.Fecha2.HasValue && model.Fecha1.Value.Date > model.Fecha2.Value.Date)
            {
                ModelState.AddModelError("Fecha1", "La fecha de inicio no puede ser posterior a la fecha de fin.");
            }


            // Validaciones de formulario
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value!.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage).ToArray()
                    );

                return Json(new AlertResponseDTO
                {
                    Success = false,
                    Title = "Error de Validación",
                    Message = "Por favor, corrija los campos marcados.",
                    Errors = errors
                });
            }

            string? newEvidenciaPath = originalPermiso.Evidencia;
            string? oldEvidenciaPath = originalPermiso.Evidencia;

            try
            {
                // 1. Procesar nuevo archivo (si se subió uno)
                if (model.EvidenceFile != null)
                {
                    // Validaciones de archivo (tamaño/tipo)
                    if (model.EvidenceFile.Length > 5 * 1024 * 1024)
                    {
                        return Json(new AlertResponseDTO { Success = false, Title = "Error de Archivo", Message = "El archivo no debe superar los 5MB." });
                    }

                    var extension = Path.GetExtension(model.EvidenceFile.FileName).ToLower();
                    if (extension != ".pdf" && extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                    {
                        return Json(new AlertResponseDTO { Success = false, Title = "Error de Archivo", Message = "Solo se permiten archivos PDF, JPG y PNG." });
                    }

                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "Evidences");
                    string uniqueFileName = $"{originalPermiso.IdUsuarioSolicita}_{Guid.NewGuid()}{extension}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.EvidenceFile.CopyToAsync(fileStream);
                    }
                    // Store web-relative path
                    newEvidenciaPath = "/Evidences/" + uniqueFileName;
                }

                // NOTA IMPORTANTE: La edición de un permiso NO debe permitir cambiar las fechas si cruza un corte. 
                // Asumo que la validación para editar solo permite hacerlo si no ha sido revisado, lo que 
                // implícitamente protege contra la edición de permisos de cortes pasados.

                // 2. Actualizar el objeto original (usando AutoMapper para mapear campos permitidos)
                _mapper.Map(model, originalPermiso);
                originalPermiso.Evidencia = newEvidenciaPath;
                originalPermiso.IdUsuarioModifica = idUsuarioModifica;
                originalPermiso.FechaModificacion = DateTime.Now;

                _context.Update(originalPermiso);
                await _context.SaveChangesAsync();

                // 3. Eliminar archivo antiguo si se reemplazó
                if (model.EvidenceFile != null && oldEvidenciaPath != null && oldEvidenciaPath != newEvidenciaPath)
                {
                    DeleteFile(oldEvidenciaPath);
                }

                // 4. Devolver respuesta de ÉXITO
                return Json(new AlertResponseDTO(
                    controllerName: "Permisos", // Redirección al Index
                    message: "Permiso modificado exitosamente."
                ));
            }
            catch (Exception ex)
            {
                // Eliminar el nuevo archivo si la transacción falla
                if (model.EvidenceFile != null && newEvidenciaPath != null && newEvidenciaPath != oldEvidenciaPath)
                {
                    DeleteFile(newEvidenciaPath);
                }
                // Loguear la excepción
                // _logger.LogError(ex, "Error al editar permiso en POST");
                return Json(new AlertResponseDTO { Success = false, Title = "Error del Servidor", Message = $"Error al modificar el permiso: {ex.Message}" });
            }
        }

        #endregion

        #region MÉTODOS PRIVADOS Y HELPERS

        private PermissionsCreateViewModel MapToEditViewModel(Permiso permiso)
        {
            // Mapeo simple de Permiso a ViewModel
            return new PermissionsCreateViewModel
            {
                Id = permiso.Id,
                IdUsuarioSolicita = permiso.IdUsuarioSolicita.Value,
                IdTipoPermiso = permiso.IdTipoPermiso.Value,
                Fecha1 = permiso.Fecha1,
                Fecha2 = permiso.Fecha2,
                Motivo = permiso.Motivo,
                Evidencia = permiso.Evidencia,
                Goce = permiso.Goce ?? false
            };
        }

        private bool CanAccessPermiso(Permiso permiso)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            bool isRH = User.HasClaim("TipoUsuario", "2");
            bool isAdministrador = User.HasClaim("TipoUsuario", "4");

            // RH and Admin can access any permiso.
            if (isRH || isAdministrador)
            {
                return true;
            }

            // Employee can only access their own permisos.
            if (permiso.IdUsuarioSolicita == idUsuario)
            {
                return true;
            }

            // Authorizer (TipoUsuario = 1) can only access permisos from their area.
            if (User.HasClaim("TipoUsuario", "1"))
            {
                var userArea = _context.Usuarios.AsNoTracking().Where(u => u.Id == idUsuario).Select(u => u.IdArea).FirstOrDefault();
                if (permiso.IdUsuarioSolicitaNavigation.IdArea == userArea)
                {
                    return true;
                }
            }

            return false;
        }

        private void DeleteFile(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_environment.WebRootPath, "Evidences", fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                // Log the error, but don't fail the business transaction, as the business operation has already failed.
                // _logger.LogError(ex, "Error al intentar eliminar el archivo: {FileName}", fileName);
            }
        }

        #endregion

        #region REVISION DE PERMISOS (AUTORIZADORES)

        // GET: Permisos/Review/5
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            // Solo autorizadores (jefes de área) pueden revisar permisos
            bool isAutorizador = User.HasClaim("TipoUsuario", "1");
            if (!isAutorizador)
            {
                TempData["ErrorAlert"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    icon = "error",
                    title = "Acceso Denegado",
                    text = "Solo los jefes de área pueden revisar permisos."
                });
                return RedirectToAction("Index");
            }

            var permiso = await _context.Permisos
                .Include(p => p.IdTipoPermisoNavigation)
                .Include(p => p.IdUsuarioSolicitaNavigation)
                    .ThenInclude(u => u.IdAreaNavigation)
                .Include(p => p.IdUsuarioCreaNavigation)
                .Include(p => p.IdCorteNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (permiso == null)
            {
                TempData["ErrorAlert"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    icon = "error",
                    title = "No encontrado",
                    text = "El permiso solicitado no existe."
                });
                return RedirectToAction("Index");
            }

            // Validar que el permiso pertenezca al área del autorizador
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userArea = await _context.Usuarios
                .Where(u => u.Id == idUsuario)
                .Select(u => u.IdArea)
                .FirstOrDefaultAsync();

            if (permiso.IdUsuarioSolicitaNavigation.IdArea != userArea)
            {
                TempData["ErrorAlert"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    icon = "error",
                    title = "Acceso Denegado",
                    text = "No puede revisar permisos de otras áreas."
                });
                return RedirectToAction("Index");
            }

            // Validar que el permiso no haya sido revisado previamente
            if (permiso.Revisado ?? false)
            {
                TempData["ErrorAlert"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    icon = "info",
                    title = "Permiso Revisado",
                    text = "Este permiso ya fue revisado anteriormente."
                });
                return RedirectToAction("Details", new { id = permiso.Id });
            }

            // Validar que el corte esté vigente
            if (permiso.IdCorteNavigation != null && !(permiso.IdCorteNavigation.Estatus ?? false))
            {
                TempData["ErrorAlert"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    icon = "warning",
                    title = "Corte No Vigente",
                    text = "No se puede revisar un permiso de un corte no vigente."
                });
                return RedirectToAction("Index");
            }

            // Crear ViewModel
            var viewModel = new PermissionReviewViewModel
            {
                Id = permiso.Id,
                NombreSolicitante = permiso.IdUsuarioSolicitaNavigation?.Nombre ?? "Desconocido",
                CorreoSolicitante = permiso.IdUsuarioSolicitaNavigation?.Correo ?? "Sin correo",
                AreaSolicitante = permiso.IdUsuarioSolicitaNavigation?.IdAreaNavigation?.Descripcion ?? "Sin área",
                IdTipoPermiso = permiso.IdTipoPermiso.Value,
                TipoPermisoDescripcion = permiso.IdTipoPermisoNavigation?.Descripcion ?? "Sin tipo",
                Fecha1 = permiso.Fecha1,
                Fecha2 = permiso.Fecha2,
                Dias = permiso.Dias ?? 0,
                Motivo = permiso.Motivo,
                Evidencia = permiso.Evidencia,
                Goce = permiso.Goce ?? false, // Valor actual
                FechaCreacion = permiso.FechaCreacion.Value,
                CreadoPor = permiso.IdUsuarioCreaNavigation?.Nombre,
                IdCorte = permiso.IdCorte.Value,
                CorteInicia = permiso.IdCorteNavigation?.Inicia,
                CorteTermina = permiso.IdCorteNavigation?.Termina
            };

            return View(viewModel);
        }

        // POST: Permisos/Review/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, PermissionReviewViewModel model)
        {
            if (id != model.Id)
            {
                return Json(new AlertResponseDTO 
                { 
                    Success = false, 
                    Title = "Error", 
                    Message = "El ID del permiso no coincide." 
                });
            }

            // Solo autorizadores pueden revisar
            bool isAutorizador = User.HasClaim("TipoUsuario", "1");
            if (!isAutorizador)
            {
                return Json(new AlertResponseDTO 
                { 
                    Success = false, 
                    Title = "Acceso Denegado", 
                    Message = "Solo los jefes de área pueden revisar permisos." 
                });
            }

            // Obtener ID del autorizador
            int idUsuarioAutoriza = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var permiso = await _context.Permisos
                    .Include(p => p.IdUsuarioSolicitaNavigation)
                    .Include(p => p.IdCorteNavigation)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (permiso == null)
                {
                    return Json(new AlertResponseDTO 
                    { 
                        Success = false, 
                        Title = "Error", 
                        Message = "Permiso no encontrado." 
                    });
                }

                // Validar que el permiso pertenezca al área del autorizador
                var userArea = await _context.Usuarios
                    .Where(u => u.Id == idUsuarioAutoriza)
                    .Select(u => u.IdArea)
                    .FirstOrDefaultAsync();

                if (permiso.IdUsuarioSolicitaNavigation.IdArea != userArea)
                {
                    return Json(new AlertResponseDTO 
                    { 
                        Success = false, 
                        Title = "Acceso Denegado", 
                        Message = "No puede revisar permisos de otras áreas." 
                    });
                }

                // Validar que no haya sido revisado
                if (permiso.Revisado ?? false)
                {
                    return Json(new AlertResponseDTO 
                    { 
                        Success = false, 
                        Title = "Permiso Revisado", 
                        Message = "Este permiso ya fue revisado anteriormente." 
                    });
                }

                // Validar que el corte esté vigente
                if (permiso.IdCorteNavigation != null && !(permiso.IdCorteNavigation.Estatus ?? false))
                {
                    return Json(new AlertResponseDTO 
                    { 
                        Success = false, 
                        Title = "Corte No Vigente", 
                        Message = "No se puede revisar un permiso de un corte no vigente." 
                    });
                }

                // Actualizar SOLO los campos permitidos
                permiso.Goce = model.Goce;
                permiso.Revisado = true;
                permiso.IdUsuarioAutoriza = idUsuarioAutoriza;
                permiso.FechaAutorizacion = DateTime.Now;
                permiso.FechaModificacion = DateTime.Now;

                _context.Update(permiso);
                await _context.SaveChangesAsync();

                // Devolver respuesta de éxito
                return Json(new AlertResponseDTO
                {
                    Success = true,
                    Title = "¡Permiso Revisado!",
                    Message = $"El permiso ha sido revisado exitosamente. Goce de sueldo: {(model.Goce ? "Con goce" : "Sin goce")}",
                    Icon = "success",
                    RedirectUrl = Url.Action("Index", "Permisos")
                });
            }
            catch (Exception ex)
            {
                return Json(new AlertResponseDTO 
                { 
                    Success = false, 
                    Title = "Error del Servidor", 
                    Message = $"Error al revisar el permiso: {ex.Message}" 
                });
            }
        }

        #endregion

        // GET: Permisos/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var permiso = await _context.Permisos
                .Include(p => p.IdTipoPermisoNavigation)
                .Include(p => p.IdUsuarioSolicitaNavigation)
                    .ThenInclude(u => u.IdAreaNavigation)
                .Include(p => p.IdUsuarioAutorizaNavigation)
                .Include(p => p.IdUsuarioCreaNavigation)
                .Include(p => p.IdUsuarioModificaNavigation)
                .Include(p => p.IdCorteNavigation)
                .FirstOrDefaultAsync(p => p.Id == id.Value);

            if (permiso == null)
            {
                return NotFound();
            }

            // Authorization: reuse helper
            if (!CanAccessPermiso(permiso))
            {
                return Forbid();
            }

            return View(permiso);
        }

        // GET: Permisos/DetailsCheck/5
        [HttpGet]
        public async Task<IActionResult> DetailsCheck(int id)
        {
            var permiso = await _context.Permisos
                .Include(p => p.IdUsuarioSolicitaNavigation)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permiso == null)
            {
                return Json(new { exists = false, hasAccess = false, message = "Permiso no encontrado." });
            }

            // Reuse authorization helper (synchronous) to check access
            bool allowed = CanAccessPermiso(permiso);
            if (!allowed)
            {
                return Json(new { exists = true, hasAccess = false, message = "No tienes permisos para ver este permiso." });
            }

            var url = Url.Action("Details", "Permisos", new { id });
            return Json(new { exists = true, hasAccess = true, url });
        }

        #region GENERACIÓN DE PERMISOS DE PRUEBA

        // POST: Permisos/GenerateTestData
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateTestData()
        {
            // Solo administradores pueden generar datos de prueba
            bool isAdministrador = User.HasClaim("TipoUsuario", "4");
            if (!isAdministrador)
            {
                return Json(new AlertResponseDTO 
                { 
                    Success = false, 
                    Title = "Acceso Denegado", 
                    Message = "Solo los administradores pueden generar datos de prueba." 
                });
            }

            try
            {
                // 1. Obtener el corte vigente (activo)
                var corteVigente = await _context.Cortes
                    .Where(c => c.Estatus == true)
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync();

                if (corteVigente == null || !corteVigente.Inicia.HasValue || !corteVigente.Termina.HasValue)
                {
                    return Json(new AlertResponseDTO 
                    { 
                        Success = false, 
                        Title = "Error", 
                        Message = "No se encontró un corte vigente con fechas válidas." 
                    });
                }

                // 2. Obtener los primeros 10 usuarios activos con su información de área
                var usuarios = await _context.Usuarios
                    .Where(u => u.Estatus == true)
                    .OrderBy(u => u.Id)
                    .Take(10)
                    .Select(u => new { u.Id, u.IdArea })
                    .ToListAsync();

                if (usuarios.Count == 0)
                {
                    return Json(new AlertResponseDTO 
                    { 
                        Success = false, 
                        Title = "Error", 
                        Message = "No hay usuarios activos en el sistema." 
                    });
                }

                // 3. Obtener los autorizadores (jefes de área) por cada área
                // Autorizadores son usuarios con IdTipoUsuario = 1 (Autorizador)
                var autorizadoresPorArea = await _context.Usuarios
                    .Where(u => u.Estatus == true && u.IdTipoUsuario == 1 && u.IdArea.HasValue)
                    .GroupBy(u => u.IdArea.Value)
                    .Select(g => new
                    {
                        IdArea = g.Key,
                        IdAutorizador = g.OrderBy(u => u.Id).First().Id // Primer autorizador del área
                    })
                    .ToDictionaryAsync(x => x.IdArea, x => x.IdAutorizador);

                // Si no hay autorizadores, crear un mapeo por defecto
                if (autorizadoresPorArea.Count == 0)
                {
                    return Json(new AlertResponseDTO 
                    { 
                        Success = false, 
                        Title = "Error", 
                        Message = "No se encontraron autorizadores (usuarios con TipoUsuario = 1) activos en el sistema." 
                    });
                }

                // 4. Obtener los tipos de permiso disponibles (2=Falta, 3=Retardo, 4=Cambio Horario, 5=Turno por Turno)
                var tiposPermiso = new List<int> { 2, 3, 4, 5 };

                // 5. Obtener ID del usuario creador (admin actual)
                int idUsuarioCreador = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 6. Configurar fechas del rango del corte
                DateTime fechaInicio = corteVigente.Inicia.Value.Date;
                DateTime fechaFin = corteVigente.Termina.Value.Date;
                int diasEnRango = (int)(fechaFin - fechaInicio).TotalDays + 1;

                // 7. Preparar lista de permisos
                var permisosAGenerar = new List<Permiso>();
                var random = new Random();
                var fechaActual = DateTime.Now;

                // 8. Generar 100,000 permisos
                for (int i = 0; i < 100000; i++)
                {
                    // Seleccionar usuario aleatorio de los 10
                    var usuarioSeleccionado = usuarios[random.Next(usuarios.Count)];
                    int idUsuarioSolicita = usuarioSeleccionado.Id;
                    int idAreaUsuario = usuarioSeleccionado.IdArea ?? 0;

                    // Obtener el autorizador (jefe) del área del usuario
                    // Si no existe autorizador para esa área, usar el primer autorizador disponible
                    int idUsuarioModifica = autorizadoresPorArea.ContainsKey(idAreaUsuario) 
                        ? autorizadoresPorArea[idAreaUsuario] 
                        : autorizadoresPorArea.Values.First();

                    // Seleccionar tipo de permiso aleatorio
                    int idTipoPermiso = tiposPermiso[random.Next(tiposPermiso.Count)];

                    // Generar fecha aleatoria dentro del rango del corte
                    int diasAleatorios = random.Next(diasEnRango);
                    DateTime fecha1 = fechaInicio.AddDays(diasAleatorios);

                    // Configurar Fecha2 y Días según el tipo
                    DateTime? fecha2 = null;
                    int? dias = null;

                    switch (idTipoPermiso)
                    {
                        case 2: // Falta (1 a 5 días)
                            int diasFalta = random.Next(1, 6);
                            fecha2 = fecha1.AddDays(diasFalta - 1);
                            // Asegurar que no exceda el corte
                            if (fecha2.Value > fechaFin)
                                fecha2 = fechaFin;
                            dias = (int)(fecha2.Value - fecha1).TotalDays + 1;
                            break;

                        case 3: // Retardo (solo 1 día, sin Fecha2)
                            fecha2 = null;
                            dias = null;
                            // Agregar hora aleatoria para retardo
                            fecha1 = fecha1.AddHours(random.Next(7, 12)).AddMinutes(random.Next(0, 60));
                            break;

                        case 4: // Cambio de Horario (1 a 3 días)
                            int diasCambio = random.Next(1, 4);
                            fecha2 = fecha1.AddDays(diasCambio - 1);
                            if (fecha2.Value > fechaFin)
                                fecha2 = fechaFin;
                            dias = (int)(fecha2.Value - fecha1).TotalDays + 1;
                            // Agregar horas para cambio de horario
                            fecha1 = fecha1.AddHours(random.Next(6, 10)).AddMinutes(random.Next(0, 60));
                            if (fecha2.HasValue)
                                fecha2 = fecha2.Value.AddHours(random.Next(14, 18)).AddMinutes(random.Next(0, 60));
                            break;

                        case 5: // Turno por Turno (1 a 2 días)
                            int diasTurno = random.Next(1, 3);
                            fecha2 = fecha1.AddDays(diasTurno - 1);
                            if (fecha2.Value > fechaFin)
                                fecha2 = fechaFin;
                            dias = (int)(fecha2.Value - fecha1).TotalDays + 1;
                            break;
                    }

                    // Motivos de ejemplo
                    var motivos = new List<string>
                    {
                        "Cita médica",
                        "Asunto personal",
                        "Trámite bancario",
                        "Emergencia familiar",
                        "Problema de transporte",
                        "Consulta especialista",
                        "Trámite gubernamental",
                        "Asunto educativo",
                        "Compromiso familiar",
                        "Situación imprevista"
                    };

                    string motivo = motivos[random.Next(motivos.Count)];

                    // Goce aleatorio (70% con goce, 30% sin goce)
                    bool goce = random.Next(100) < 70;

                    // Crear el permiso con el autorizador del área como IdUsuarioModifica
                    var permiso = new Permiso
                    {
                        Fecha1 = fecha1,
                        Fecha2 = fecha2,
                        Dias = dias,
                        Motivo = motivo,
                        IdCorte = corteVigente.Id,
                        IdTipoPermiso = idTipoPermiso,
                        IdUsuarioSolicita = idUsuarioSolicita,
                        IdUsuarioCrea = idUsuarioCreador,
                        IdUsuarioModifica = idUsuarioModifica, // *** AHORA ES EL JEFE DE ÁREA ***
                        FechaCreacion = fechaActual,
                        FechaModificacion = fechaActual,
                        Estatus = true,
                        Revisado = false,
                        Goce = goce,
                        Evidencia = null
                    };

                    permisosAGenerar.Add(permiso);

                    // Insertar en lotes de 1000 para mejor rendimiento
                    if (permisosAGenerar.Count >= 1000)
                    {
                        await _context.Permisos.AddRangeAsync(permisosAGenerar);
                        await _context.SaveChangesAsync();
                        permisosAGenerar.Clear();
                    }
                }

                // 9. Guardar los permisos restantes
                if (permisosAGenerar.Any())
                {
                    await _context.Permisos.AddRangeAsync(permisosAGenerar);
                    await _context.SaveChangesAsync();
                }

                // 10. Devolver respuesta de éxito con información detallada
                return Json(new AlertResponseDTO 
                { 
                    Success = true, 
                    Title = "¡Éxito!", 
                    Message = $"Se generaron 100,000 permisos de prueba exitosamente.\n\n" +
                              $"📋 Corte: {corteVigente.Id}\n" +
                              $"📅 Fecha Inicio: {fechaInicio:dd/MM/yyyy}\n" +
                              $"📅 Fecha Fin: {fechaFin:dd/MM/yyyy}\n" +
                              $"👥 Usuarios: {usuarios.Count}\n" +
                              $"👔 Autorizadores (Jefes de Área): {autorizadoresPorArea.Count}\n\n" +
                              $"✅ Cada permiso fue asignado al jefe de área correspondiente como revisor.",
                    Icon = "success",
                    RedirectUrl = Url.Action("Index", "Permisos")
                });
            }
            catch (Exception ex)
            {
                return Json(new AlertResponseDTO 
                { 
                    Success = false, 
                    Title = "Error del Servidor", 
                    Message = $"Error al generar datos de prueba: {ex.Message}\n\nStackTrace: {ex.StackTrace}",
                    Icon = "error"
                });
            }
        }

        #endregion
    }
}