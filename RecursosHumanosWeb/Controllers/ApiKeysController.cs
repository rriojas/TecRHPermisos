using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecursosHumanosWeb.Models;

namespace RecursosHumanosWeb.Controllers
{
    public class ApiKeysController : Controller
    {
        public string Clave { get; set; }
        private readonly RecursosHumanosContext _context;

        public ApiKeysController(RecursosHumanosContext context)
        {
            _context = context;
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
            ViewData["IdUsuarioCrea"] = new SelectList(_context.Usuarios, "Id", "Nombre");
            ViewData["IdUsuarioModifica"] = new SelectList(_context.Usuarios, "Id", "Nombre");
            return View();
        }


        // POST: ApiKeys/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdUsuarioCrea,IdUsuarioModifica")] ApiKey apiKey)
        {
            // Eliminar errores de validación para propiedades generadas o de navegación
            ModelState.Remove("Clave");
            ModelState.Remove("FechaCreacion");
            ModelState.Remove("FechaModificacion");
            ModelState.Remove("Estatus");
            ModelState.Remove("IdUsuarioCreaNavigation");
            ModelState.Remove("IdUsuarioModificaNavigation");

            // Asignar valores
            apiKey.Clave = Guid.NewGuid().ToString();
            apiKey.FechaCreacion = DateTime.Now;
            apiKey.FechaModificacion = DateTime.Now;
            apiKey.Estatus = true; // Compatible con bit (0 o 1)

            // Validar que IdUsuarioCrea y IdUsuarioModifica no sean null
            if (apiKey.IdUsuarioCrea == 0 || apiKey.IdUsuarioModifica == 0)
            {
                ModelState.AddModelError("IdUsuarioCrea", "Debe seleccionar un usuario para IdUsuarioCrea.");
                ModelState.AddModelError("IdUsuarioModifica", "Debe seleccionar un usuario para IdUsuarioModifica.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(apiKey);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Depurar errores
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            Console.WriteLine("Errores de ModelState: " + string.Join("; ", errors));

            ViewData["IdUsuarioCrea"] = new SelectList(_context.Usuarios, "Id", "Nombre", apiKey.IdUsuarioCrea);
            ViewData["IdUsuarioModifica"] = new SelectList(_context.Usuarios, "Id", "Nombre", apiKey.IdUsuarioModifica);
            return View(apiKey);
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

        // POST: ApiKeys/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey != null)
            {
                _context.ApiKeys.Remove(apiKey);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApiKeyExists(int id)
        {
            return _context.ApiKeys.Any(e => e.Id == id);
        }
    }
}
