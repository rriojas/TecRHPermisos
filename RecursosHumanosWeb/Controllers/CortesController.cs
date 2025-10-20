using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.DTOs;
using RecursosHumanosWeb.Models.ViewModels;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList.Extensions;

namespace RecursosHumanosWeb.Controllers
{
    public class CortesController : Controller
    {
        private readonly RecursosHumanosContext _context;

        public CortesController(RecursosHumanosContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? usuarioCreadorFilter = null, bool? statusFilter = null)
        {
            // Validar parámetros de paginación
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 5;

            // Obtener información del usuario autenticado
            bool isAdministrador = User.HasClaim("TipoUsuario", "4");

            // Construir la consulta base
            var cortesQuery = _context.Cortes
                .Include(c => c.IdUsuarioCreaNavigation)
                .Include(c => c.IdUsuarioModificaNavigation)
                .AsQueryable();

            // Aplicar filtros
            if (fechaDesde.HasValue)
            {
                cortesQuery = cortesQuery.Where(c => c.Inicia >= fechaDesde.Value || c.Inicia == null);
            }
            if (fechaHasta.HasValue)
            {
                cortesQuery = cortesQuery.Where(c => c.Termina <= fechaHasta.Value || c.Termina == null);
            }
            if (usuarioCreadorFilter.HasValue)
            {
                cortesQuery = cortesQuery.Where(c => c.IdUsuarioCrea == usuarioCreadorFilter.Value);
            }
            if (statusFilter.HasValue)
            {
                cortesQuery = cortesQuery.Where(c => c.Estatus == statusFilter.Value);
            }

            // Obtener usuarios para el filtro
            var usuarios = await _context.Cortes
                .Include(c => c.IdUsuarioCreaNavigation)
                .Where(c => c.IdUsuarioCreaNavigation != null)
                .Select(c => new UsuariosDTO
                {
                    Id = c.IdUsuarioCreaNavigation.Id,
                    Nombre = c.IdUsuarioCreaNavigation.Nombre
                })
                .Distinct()
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            // Preparar la consulta filtrada con CortesDTO
            var cortesQueryFiltered = cortesQuery
                .Select(c => new CortesDTO
                {
                    Id = c.Id,
                    Inicia = c.Inicia,
                    Termina = c.Termina,
                    FechaCreacion = c.FechaCreacion,
                    FechaModificacion = c.FechaModificacion,
                    Estatus = c.Estatus,
                    IdUsuarioCrea = c.IdUsuarioCrea,
                    IdUsuarioModifica = c.IdUsuarioModifica
                })
                .OrderByDescending(c => c.Id); // Ordenar por Id descendente

            // Depuración: Registrar los IDs para verificar el orden
            var cortesList = await cortesQueryFiltered.ToListAsync();
            System.Diagnostics.Debug.WriteLine($"Cortes ordenados: {string.Join(", ", cortesList.Select(c => c.Id))}");

            // Obtener resultados paginados
            var cortes = cortesQueryFiltered.ToPagedList(page, pageSize);

            // Preparar el ViewModel
            var model = new SearchCorteViewModel
            {
                Cortes = cortes,
                Usuarios = usuarios,
                FechaDesde = fechaDesde?.ToString("yyyy-MM-dd"),
                FechaHasta = fechaHasta?.ToString("yyyy-MM-dd"),
                UsuarioCreadorFilter = usuarioCreadorFilter?.ToString(),
                StatusFilter = statusFilter?.ToString(),
                IsAdministrador = isAdministrador
            };

            // Pasar datos a la vista
            ViewBag.FechaDesde = fechaDesde;
            ViewBag.FechaHasta = fechaHasta;
            ViewBag.UsuarioCreadorFilter = usuarioCreadorFilter;
            ViewBag.StatusFilter = statusFilter;

            return View(model);
        }

        // GET: Cortes/Create
        public IActionResult Create()
        {
            // Verificar que el usuario esté autenticado
            if (!ViewData.ContainsKey("IdUsuario") || string.IsNullOrEmpty(ViewData["IdUsuario"]?.ToString()))
            {
                return Unauthorized(); // O redirigir a una página de error si el usuario no está autenticado
            }

            // Crear un nuevo modelo CorteCreateViewModel con valores por defecto
            var viewModel = new CorteCreateViewModel();

            // Obtener la fecha de fin del último corte
            var ultimoCorte = _context.Cortes
                .OrderByDescending(c => c.Termina)
                .FirstOrDefault();

            if (ultimoCorte != null && ultimoCorte.Termina.HasValue)
            {
                // Establecer Inicia como un segundo después de Termina del último corte
                viewModel.Inicia = ultimoCorte.Termina.Value.AddSeconds(1);
            }
            else
            {
                // Si no hay cortes previos, usar la fecha y hora actual
                viewModel.Inicia = DateTime.Now;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CorteCreateViewModel viewModel)
        {
            // Verificar que el usuario esté autenticado
            if (!ViewData.ContainsKey("IdUsuario") || string.IsNullOrEmpty(ViewData["IdUsuario"]?.ToString()))
            {
                return Unauthorized();
            }

            // Validaciones adicionales
            var ultimoCorte = await _context.Cortes
                .OrderByDescending(c => c.Id) // Ordenar por Id para obtener el más reciente
                .FirstOrDefaultAsync();

            // Validar que las fechas sean coherentes si se proporcionan
            if (viewModel.Inicia.HasValue && viewModel.Termina.HasValue)
            {
                var ultimoCorteConFechas = await _context.Cortes
                    .Where(c => c.Termina.HasValue)
                    .OrderByDescending(c => c.Termina.Value)
                    .FirstOrDefaultAsync();

                if (ultimoCorteConFechas != null && viewModel.Inicia <= ultimoCorteConFechas.Termina.Value)
                {
                    ModelState.AddModelError("Inicia", "La fecha de inicio debe ser posterior a la fecha de fin del último corte con fechas.");
                }

                if (viewModel.Termina < viewModel.Inicia)
                {
                    ModelState.AddModelError("Termina", "La fecha de fin no puede ser anterior a la fecha de inicio.");
                }

                if (ultimoCorteConFechas != null && viewModel.Termina <= ultimoCorteConFechas.Termina.Value)
                {
                    ModelState.AddModelError("Termina", "La fecha de fin debe ser posterior a la fecha de fin del último corte con fechas.");
                }
            }

            if (ModelState.IsValid)
            {
                // Obtener el ID del usuario autenticado
                int idUsuario = int.Parse(ViewData["IdUsuario"].ToString());
                DateTime ahora = DateTime.Now;

                // 1. No actualizar el corte existente; solo crear un nuevo registro
                var nuevoCorte = new Corte
                {
                    Inicia = viewModel.Inicia, // Usar las fechas del formulario, o null si no se proporcionan
                    Termina = viewModel.Termina,
                    IdUsuarioCrea = idUsuario,
                    IdUsuarioModifica = idUsuario,
                    FechaCreacion = ahora,
                    FechaModificacion = ahora,
                    Estatus = viewModel.Inicia.HasValue && viewModel.Termina.HasValue // true si hay fechas, false si son null
                };

                _context.Add(nuevoCorte);

                // 2. Asegurarse de que el último corte (con Id más grande) siempre tenga Inicia y Termina null si es el marcador de futuro
                if (ultimoCorte != null && (!ultimoCorte.Inicia.HasValue || !ultimoCorte.Termina.HasValue))
                {
                    ultimoCorte.Inicia = null;
                    ultimoCorte.Termina = null;
                    ultimoCorte.Estatus = false; // Asegurar que sea inactivo
                    ultimoCorte.FechaModificacion = ahora;
                    ultimoCorte.IdUsuarioModifica = idUsuario;
                    _context.Update(ultimoCorte);
                }
                else if (ultimoCorte == null || (ultimoCorte.Inicia.HasValue && ultimoCorte.Termina.HasValue))
                {
                    // Si no hay último corte o el último tiene fechas, crear un marcador de futuro inactivo
                    var marcadorFuturo = new Corte
                    {
                        Inicia = null,
                        Termina = null,
                        IdUsuarioCrea = idUsuario,
                        IdUsuarioModifica = idUsuario,
                        FechaCreacion = ahora,
                        FechaModificacion = ahora,
                        Estatus = false // Marcador de futuro siempre inactivo
                    };
                    _context.Add(marcadorFuturo);
                }

                // Guardar todos los cambios en una sola transacción
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si el modelo no es válido, devolver la vista con los errores
            return View(viewModel);
        }

        // GET: Cortes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var corte = await _context.Cortes.FindAsync(id);
            if (corte == null)
            {
                return NotFound();
            }
            ViewData["IdUsuarioCrea"] = new SelectList(_context.Usuarios, "Id", "Nombre", corte.IdUsuarioCrea);
            ViewData["IdUsuarioModifica"] = new SelectList(_context.Usuarios, "Id", "Nombre", corte.IdUsuarioModifica);
            return View(corte);
        }

        // POST: Cortes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Inicia,Termina,FechaCreacion,FechaModificacion,Estatus,IdUsuarioCrea,IdUsuarioModifica")] Corte corte)
        {
            if (id != corte.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    corte.FechaModificacion = DateTime.Now; // FechaModificacion remains DateTime
                    _context.Update(corte);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CorteExists(corte.Id))
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
            ViewData["IdUsuarioCrea"] = new SelectList(_context.Usuarios, "Id", "Nombre", corte.IdUsuarioCrea);
            ViewData["IdUsuarioModifica"] = new SelectList(_context.Usuarios, "Id", "Nombre", corte.IdUsuarioModifica);
            return View(corte);
        }

        // GET: Cortes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var corte = await _context.Cortes
                .Include(c => c.IdUsuarioCreaNavigation)
                .Include(c => c.IdUsuarioModificaNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (corte == null)
            {
                return NotFound();
            }

            return View(corte);
        }

        // POST: Cortes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var corte = await _context.Cortes.FindAsync(id);
            if (corte != null)
            {
                _context.Cortes.Remove(corte);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CorteExists(int id)
        {
            return _context.Cortes.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var corte = await _context.Cortes
                .Include(c => c.IdUsuarioCreaNavigation)
                .Include(c => c.IdUsuarioModificaNavigation)
                .Include(c => c.Permisos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (corte == null)
            {
                return NotFound();
            }

            // Obtener permisos asociados al corte, excluyendo aquellos con Estatus = 0
            var permisos = await _context.Permisos
                .Where(p => p.IdCorte == id && p.Estatus != false)
                .Include(p => p.IdTipoPermisoNavigation)
                .Include(p => p.IdUsuarioSolicitaNavigation)
                .ThenInclude(u => u.IdAreaNavigation)
                .Include(p => p.IdUsuarioAutorizaNavigation)
                .ToListAsync();

            // Debug: Verificar permisos y sus propiedades
            Console.WriteLine($"Permisos encontrados para Corte ID {id}: {permisos.Count}");
            foreach (var permiso in permisos)
            {
                Console.WriteLine($"Permiso ID: {permiso.Id}, IdTipoPermiso: {permiso.IdTipoPermiso}, Tipo: {permiso.IdTipoPermisoNavigation?.Descripcion ?? "Nulo"}, IdUsuarioSolicita: {permiso.IdUsuarioSolicita}, Area: {permiso.IdUsuarioSolicitaNavigation?.IdAreaNavigation?.Descripcion ?? "Nulo"}");
            }

            // Permisos por tipo (manejar IdTipoPermisoNavigation nulo)
            var permisosPorTipo = permisos
                .GroupBy(p => p.IdTipoPermisoNavigation != null ? p.IdTipoPermisoNavigation.Descripcion : "Desconocido")
                .Select(g => new
                {
                    Name = g.Key,
                    Value = g.Count(),
                    Color = g.Key switch
                    {
                        "Falta" => "#3b82f6",
                        "Retardo" => "#10b981",
                        "Cambio de Horario" => "#f59e0b",
                        "Turno por Turno" => "#8b5cf6",
                        _ => "#6b7280"
                    }
                })
                .ToList();

            // Debug: Verificar permisosPorTipo
            Console.WriteLine($"PermisosPorTipo: {JsonConvert.SerializeObject(permisosPorTipo)}");

            // Permisos por área (manejar IdAreaNavigation nulo)
            var permisosPorArea = permisos
                .GroupBy(p => p.IdUsuarioSolicitaNavigation?.IdAreaNavigation?.Descripcion ?? "Desconocido")
                .Select(g => new
                {
                    Name = g.Key,
                    Value = g.Count(),
                    Color = g.Key switch
                    {
                        "SUBDIRECCION DE SERVICIOS ADMINISTRATIVOS" => "#3b82f6",
                        "DIRECCION GENERAL" => "#10b981",
                        "DIRECCION DE PLANEACION Y VINCULACION" => "#8b5cf6",
                        "DIRECCION ACADEMICA" => "#f59e0b",
                        _ => "#6b7280"
                    }
                })
                .ToList();

            // Debug: Verificar permisosPorArea
            Console.WriteLine($"PermisosPorArea: {JsonConvert.SerializeObject(permisosPorArea)}");

            // Tendencia temporal
            var tendencia = permisos
                .GroupBy(p => new { p.Fecha1.Year, p.Fecha1.Month })
                .Select(g => new
                {
                    Month = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month)} {g.Key.Year}",
                    Aprobados = g.Count(p => p.IdUsuarioAutoriza != null),
                    Pendientes = g.Count(p => p.IdUsuarioAutoriza == null && p.Revisado == false),
                    Total = g.Count()
                })
                .OrderBy(g => DateTime.Parse(g.Month))
                .ToList();

            // Debug: Verificar tendencia
            Console.WriteLine($"Tendencia: {JsonConvert.SerializeObject(tendencia)}");

            // Mapa de calor (actividad semanal)
            var heatmapData = permisos
                .GroupBy(p => new
                {
                    Week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(p.Fecha1, CalendarWeekRule.FirstDay, DayOfWeek.Monday),
                    Day = p.Fecha1.DayOfWeek
                })
                .Select(g => new
                {
                    Week = $"Sem {g.Key.Week}",
                    Day = g.Key.Day switch
                    {
                        DayOfWeek.Monday => "Lun",
                        DayOfWeek.Tuesday => "Mar",
                        DayOfWeek.Wednesday => "Mié",
                        DayOfWeek.Thursday => "Jue",
                        DayOfWeek.Friday => "Vie",
                        DayOfWeek.Saturday => "Sáb",
                        DayOfWeek.Sunday => "Dom",
                        _ => ""
                    },
                    Value = g.Count()
                })
                .ToList();

            // Debug: Verificar heatmapData
            Console.WriteLine($"HeatmapData: {JsonConvert.SerializeObject(heatmapData)}");

            // KPI: Métricas de progreso
            var kpiData = new
            {
                TotalPermisos = permisos.Count,
                Aprobados = permisos.Count(p => p.IdUsuarioAutoriza != null),
                Pendientes = permisos.Count(p => p.IdUsuarioAutoriza == null && p.Revisado == false),
                DiasPromedio = permisos.Any() ? Math.Round(permisos.Where(p => p.Dias.HasValue).Average(p => p.Dias.Value), 1) : 0
            };

            // Debug: Verificar kpiData
            Console.WriteLine($"KPIData: {JsonConvert.SerializeObject(kpiData)}");

            ViewBag.PermisosPorTipo = JsonConvert.SerializeObject(permisosPorTipo);
            ViewBag.PermisosPorArea = JsonConvert.SerializeObject(permisosPorArea);
            ViewBag.Tendencia = JsonConvert.SerializeObject(tendencia);
            ViewBag.HeatmapData = JsonConvert.SerializeObject(heatmapData);
            ViewBag.KPIData = JsonConvert.SerializeObject(kpiData);

            return View(corte);
        }

        // Método auxiliar para obtener el número de semana
        private int GetWeekNumber(DateTime date)
        {
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                date,
                CalendarWeekRule.FirstDay,
                DayOfWeek.Monday
            );
        }
    }
}