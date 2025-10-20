using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.ViewModels;
using X.PagedList.Extensions;

namespace RecursosHumanosWeb.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly RecursosHumanosContext _context;

        public UsuariosController(RecursosHumanosContext context)
        {
            _context = context;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index(
            string nombreFilter,
            string correoFilter,
            int? departamentoFilter,
            bool? estadoFilter,
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
            var model = new SearchUsuarioViewModel
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

        // GET: Usuarios/Details/5
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

            return View(usuario);
        }

        // GET: Usuarios/Create
        public IActionResult Create()
        {
            ViewData["IdArea"] = new SelectList(_context.Areas, "Id", "Descripcion");
            ViewData["IdDepartamento"] = new SelectList(_context.Departamentos, "Id", "Nombre");
            ViewData["IdPuesto"] = new SelectList(_context.Puestos, "Id", "Nombre");
            ViewData["IdTipoUsuario"] = new SelectList(_context.TipoUsuarios, "Id", "Nombre");
            ViewData["IdUsuarioCrea"] = new SelectList(_context.Usuarios, "Id", "Nombre");
            ViewData["IdUsuarioModifica"] = new SelectList(_context.Usuarios, "Id", "Nombre");
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Correo,Clave,FechaCreacion,FechaModificacion,Estatus,IdTipoUsuario,IdArea,IdDepartamento,IdPuesto,IdUsuarioCrea,IdUsuarioModifica,IdApiKey")] Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                _context.Add(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdArea"] = new SelectList(_context.Areas, "Id", "Descripcion", usuario.IdArea);
            ViewData["IdDepartamento"] = new SelectList(_context.Departamentos, "Id", "Nombre", usuario.IdDepartamento);
            ViewData["IdPuesto"] = new SelectList(_context.Puestos, "Id", "Nombre", usuario.IdPuesto);
            ViewData["IdTipoUsuario"] = new SelectList(_context.TipoUsuarios, "Id", "Nombre", usuario.IdTipoUsuario);
            ViewData["IdUsuarioCrea"] = new SelectList(_context.Usuarios, "Id", "Nombre", usuario.IdUsuarioCrea);
            ViewData["IdUsuarioModifica"] = new SelectList(_context.Usuarios, "Id", "Nombre", usuario.IdUsuarioModifica);
            return View(usuario);
        }

        // GET: Usuarios/Edit/5
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
            ViewData["IdArea"] = new SelectList(_context.Areas, "Id", "Descripcion", usuario.IdArea);
            ViewData["IdDepartamento"] = new SelectList(_context.Departamentos, "Id", "Nombre", usuario.IdDepartamento);
            ViewData["IdPuesto"] = new SelectList(_context.Puestos, "Id", "Nombre", usuario.IdPuesto);
            ViewData["IdTipoUsuario"] = new SelectList(_context.TipoUsuarios, "Id", "Nombre", usuario.IdTipoUsuario);
            ViewData["IdUsuarioCrea"] = new SelectList(_context.Usuarios, "Id", "Nombre", usuario.IdUsuarioCrea);
            ViewData["IdUsuarioModifica"] = new SelectList(_context.Usuarios, "Id", "Nombre", usuario.IdUsuarioModifica);
            return View(usuario);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Correo,Clave,FechaCreacion,FechaModificacion,Estatus,IdTipoUsuario,IdArea,IdDepartamento,IdPuesto,IdUsuarioCrea,IdUsuarioModifica,IdApiKey")] Usuario usuario)
        {
            if (id != usuario.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(usuario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.Id))
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
            ViewData["IdArea"] = new SelectList(_context.Areas, "Id", "Descripcion", usuario.IdArea);
            ViewData["IdDepartamento"] = new SelectList(_context.Departamentos, "Id", "Nombre", usuario.IdDepartamento);
            ViewData["IdPuesto"] = new SelectList(_context.Puestos, "Id", "Nombre", usuario.IdPuesto);
            ViewData["IdTipoUsuario"] = new SelectList(_context.TipoUsuarios, "Id", "Nombre", usuario.IdTipoUsuario);
            ViewData["IdUsuarioCrea"] = new SelectList(_context.Usuarios, "Id", "Nombre", usuario.IdUsuarioCrea);
            ViewData["IdUsuarioModifica"] = new SelectList(_context.Usuarios, "Id", "Nombre", usuario.IdUsuarioModifica);
            return View(usuario);
        }

        // GET: Usuarios/Delete/5
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

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return NotFound();
            }

            var usuario = _context.Usuarios
                .Include(u => u.IdAreaNavigation)
                .Include(u => u.IdDepartamentoNavigation)
                .Include(u => u.IdPuestoNavigation)
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefault(u => u.Id.ToString() == userId);
            if (usuario == null)
            {
                return NotFound();
            }
            return View(usuario);
        }
    }
}