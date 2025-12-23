using Microsoft.AspNetCore.Mvc;
using RecursosHumanosWeb.Models.DTOs;
using RecursosHumanosWeb.Models;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RecursosHumanosWeb.Controllers
{
    [Route("Cortes/[action]")]
    public class CortesAjaxController : Controller
    {
        private readonly RecursosHumanosContext _context;

        public CortesAjaxController(RecursosHumanosContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] ActionRequestDTO request)
        {
            if (request == null || request.Id <= 0)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "Error", Message = "ID no válido.", Icon = "error" });
            }

            var corte = await _context.Cortes.FindAsync(request.Id);
            if (corte == null)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "No encontrado", Message = "Corte no encontrado.", Icon = "error" });
            }

            if (!request.Confirmed)
            {
                return Json(new AlertResponseDTO
                {
                    ShowConfirmation = true,
                    Title = "¿Eliminar Corte?",
                    Message = $"¿Estás seguro de que deseas eliminar el corte que inicia {corte.Inicia:dd/MM/yyyy}? Esta acción es irreversible.",
                    Icon = "question",
                    ConfirmButtonText = "Sí, eliminar"
                });
            }

            try
            {
                // Borrado lógico
                corte.Estatus = false;
                corte.FechaModificacion = DateTime.Now;
                _context.Update(corte);
                await _context.SaveChangesAsync();

                return Json(new AlertResponseDTO
                {
                    Success = true,
                    Title = "Eliminado",
                    Message = "El corte fue inactivado correctamente.",
                    Icon = "success",
                    RedirectUrl = "/Cortes/Index"
                });
            }
            catch (Exception ex)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "Error del Servidor", Message = $"Ocurrió un error al inactivar: {ex.Message}", Icon = "error" });
            }
        }
    }
}
