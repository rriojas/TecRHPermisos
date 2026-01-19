using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.DTOs;
using RecursosHumanosWeb.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RecursosHumanosWeb.Controllers
{
    public class ApiKeysController : Controller
    {
        public string Clave { get; set; }
        private readonly RecursosHumanosContext _context;
        private readonly AutoMapper.IMapper _mapper;

        public ApiKeysController(RecursosHumanosContext context, AutoMapper.IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: ApiKeys
        public async Task<IActionResult> Index()
        {
            var recursosHumanosContext = _context.ApiKeys.Include(a => a.IdUsuarioCreaNavigation).Include(a => a.IdUsuarioModificaNavigation);
            return View(await recursosHumanosContext.ToListAsync());
        }

        // GET: ApiKeys/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var apiKey = await _context.ApiKeys
                .Include(a => a.IdUsuarioCreaNavigation)
                .Include(a => a.IdUsuarioModificaNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (apiKey == null)
            {
                return NotFound();
            }

            return View(apiKey);
        }

        // GET: ApiKeys/Create

        public IActionResult Create()
        {
            // CORRECCIÓN: Usamos proyección segura para evitar nulos en el SelectList
            var usuariosActivos = _context.Usuarios
                .Where(u => u.Estatus)
                .Select(u => new { u.Id, u.Nombre }) // Solo traemos lo necesario
                .OrderBy(u => u.Nombre)
                .ToList();

            // Usamos "Nombre" que es la propiedad anónima definida arriba
            ViewData["Usuarios"] = new SelectList(usuariosActivos, "Id", "Nombre");

            ViewBag.Tablas = _context.Tablas.Where(t => t.Estatus ?? false).ToList();
            ViewBag.ApiPermisos = _context.ApiPermisos.Where(p => p.Estatus ?? false).ToList();

            // Lógica de roles (ajusta según tus necesidades reales)
            ViewBag.IsAdministrador = User.HasClaim("TipoUsuario", "4");

            return View(new ApiKeyCreateViewModel());
        }

        // POST: ApiKeys/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApiKeyCreateViewModel viewModel)
        {
            // 1. OBTENER ID REAL DEL USUARIO LOGUEADO
            // Parseamos el Claim. Si falla o es nulo, asigna 0.
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
            {
                ModelState.AddModelError("", "No se pudo identificar al usuario en sesión.");
            }

            // 2. LÓGICA DE TITULARIDAD
            // Si el dropdown vino vacío (null), asignamos el usuario logueado.
            // Si el dropdown trajo un ID, respetamos ese ID (viewModel.IdUsuarioTitular).
            if (!viewModel.IdUsuarioTitular.HasValue)
            {
                viewModel.IdUsuarioTitular = userId;
            }

            // VALIDACIÓN DEL MODELO
            if (!ModelState.IsValid || viewModel.IdUsuarioTitular == null || userId == 0)
            {
                // Recargar listas en caso de error (con la misma lógica segura del GET)
                var usuariosList = _context.Usuarios
                                        .Where(u => u.Estatus)
                                        .Select(u => new { u.Id, u.Nombre })
                                        .OrderBy(u => u.Nombre)
                                        .ToList();

                ViewData["Usuarios"] = new SelectList(usuariosList, "Id", "Nombre", viewModel.IdUsuarioTitular);
                ViewBag.Tablas = _context.Tablas.Where(t => t.Estatus ?? false).ToList();
                ViewBag.ApiPermisos = _context.ApiPermisos.Where(p => p.Estatus ?? false).ToList();
                ViewBag.IsAdministrador = User.HasClaim("TipoUsuario", "4");

                return View(viewModel);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var currentDateTime = DateTime.Now; // Fecha uniforme para todo el proceso

                    // 3. CREAR API KEY
                    var newApiKey = new ApiKey
                    {
                        Clave = Guid.NewGuid().ToString("N"),
                        Estatus = true,
                        FechaCreacion = currentDateTime,

                        // CORRECCIÓN CRÍTICA: DateTime.Now en lugar de default
                        FechaModificacion = currentDateTime,

                        IdUsuarioCrea = userId,
                        IdUsuarioModifica = userId

                        // Nota: Si tienes un campo IdUsuarioTitular en la tabla ApiKey, asígnalo aquí:
                        // IdUsuarioTitular = viewModel.IdUsuarioTitular.Value 
                    };

                    _context.Add(newApiKey);
                    await _context.SaveChangesAsync(); // Se genera el ID de la ApiKey

                    // 4. CREAR PERMISOS (ApiPermisosApiKeysTabla)
                    if (viewModel.Assignments != null)
                    {
                        foreach (var assignment in viewModel.Assignments)
                        {
                            if (assignment.IdsApiPermisosSeleccionados != null)
                            {
                                foreach (var idPermiso in assignment.IdsApiPermisosSeleccionados.Distinct())
                                {
                                    var newAssignment = new ApiPermisosApiKeysTabla
                                    {
                                        IdApiKey = newApiKey.Id, // Usamos el ID recién creado
                                        IdApiPermiso = idPermiso,
                                        IdTabla = assignment.IdTabla,
                                        Estatus = true,
                                        FechaCreacion = currentDateTime,

                                        // CORRECCIÓN CRÍTICA: DateTime.Now en lugar de default
                                        FechaModificacion = currentDateTime,

                                        IdUsuarioCrea = userId,
                                        IdUsuarioModifica = userId,
                                    };
                                    _context.Add(newAssignment);
                                }
                            }
                        }
                        // Guardamos los permisos dentro de la transacción
                        await _context.SaveChangesAsync();
                    }

                    // 5. CONFIRMAR TRANSACCIÓN
                    // Al no haber errores de fecha, esto se ejecutará y guardará ambas tablas.
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"API Key generada con éxito.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Error al generar la API Key: " + ex.Message);

                    // Recargar datos para la vista (Copy-paste de la lógica de arriba)
                    var usuariosList = _context.Usuarios
                                            .Where(u => u.Estatus)
                                            .Select(u => new { u.Id, u.Nombre })
                                            .OrderBy(u => u.Nombre)
                                            .ToList();
                    ViewData["Usuarios"] = new SelectList(usuariosList, "Id", "Nombre", viewModel.IdUsuarioTitular);
                    ViewBag.Tablas = _context.Tablas.Where(t => t.Estatus ?? false).ToList();
                    ViewBag.ApiPermisos = _context.ApiPermisos.Where(p => p.Estatus ?? false).ToList();
                    ViewBag.IsAdministrador = User.HasClaim("TipoUsuario", "4");

                    return View(viewModel);
                }
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetUsuariosParaBusqueda(string term)
        {
            // Si no hay término de búsqueda, devolvemos una lista vacía o los primeros 10.
            if (string.IsNullOrWhiteSpace(term))
            {
                term = string.Empty;
            }

            var usuarios = await _context.Usuarios
                .Where(u => u.Nombre.Contains(term) || u.Correo.Contains(term))
                .OrderBy(u => u.Nombre)
                .Take(10) // Limitar resultados para eficiencia
                .Select(u => new
                {
                    id = u.Id,
                    text = u.Nombre ?? u.Correo
                })
                .ToListAsync();

            return Json(new { results = usuarios });
        }

        // GET: ApiKeys/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey == null)
            {
                return NotFound();
            }
            ViewData["IdUsuarioCrea"] = new SelectList(_context.Usuarios, "Id", "Id", apiKey.IdUsuarioCrea);
            ViewData["IdUsuarioModifica"] = new SelectList(_context.Usuarios, "Id", "Id", apiKey.IdUsuarioModifica);
            return View(apiKey);
        }

        // POST: ApiKeys/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Clave,FechaCreacion,FechaModificacion,Estatus,IdUsuarioCrea,IdUsuarioModifica")] ApiKey apiKey)
        {
            if (id != apiKey.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(apiKey);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApiKeyExists(apiKey.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdUsuarioCrea"] = new SelectList(_context.Usuarios, "Id", "Id", apiKey.IdUsuarioCrea);
            ViewData["IdUsuarioModifica"] = new SelectList(_context.Usuarios, "Id", "Id", apiKey.IdUsuarioModifica);
            return View(apiKey);
        }

        // GET: ApiKeys/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var apiKey = await _context.ApiKeys
                .Include(a => a.IdUsuarioCreaNavigation)
                .Include(a => a.IdUsuarioModificaNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (apiKey == null)
            {
                return NotFound();
            }

            return View(apiKey);
        }

        // POST: ApiKeys/Delete/5 (form POST) - borrado lógico
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey != null)
            {
                apiKey.Estatus = false;
                apiKey.FechaModificacion = DateTime.Now;
                _context.Update(apiKey);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // AJAX: Delete via fetch with confirmation flow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] ActionRequestDTO request)
        {
            if (request == null || request.Id <= 0)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "Error", Message = "ID no válido.", Icon = "error" });
            }

            var apiKey = await _context.ApiKeys.FindAsync(request.Id);
            if (apiKey == null)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "No encontrado", Message = "API Key no encontrada.", Icon = "error" });
            }

            if (!request.Confirmed)
            {
                return Json(new AlertResponseDTO
                {
                    ShowConfirmation = true,
                    Title = "¿Eliminar API Key?",
                    Message = $"¿Estás seguro de que deseas eliminar la API Key " + apiKey.Clave + "?",
                    Icon = "question",
                    ConfirmButtonText = "Sí, eliminar"
                });
            }

                try
                {
                    // Borrado lógico
                    apiKey.Estatus = false;
                    apiKey.FechaModificacion = DateTime.Now;
                    _context.Update(apiKey);
                    await _context.SaveChangesAsync();
                    return Json(new AlertResponseDTO
                    {
                        Success = true,
                        Title = "Eliminado",
                        Message = "API Key inactivada correctamente.",
                        Icon = "success",
                        RedirectUrl = "/ApiKeys/Index"
                    });
                }
                catch (Exception ex)
                {
                    return Json(new AlertResponseDTO { Success = false, Title = "Error del Servidor", Message = $"Ocurrió un error al inactivar: {ex.Message}", Icon = "error" });
                }
        }

        private bool ApiKeyExists(int id)
        {
            return _context.ApiKeys.Any(e => e.Id == id);
        }
    }
}
