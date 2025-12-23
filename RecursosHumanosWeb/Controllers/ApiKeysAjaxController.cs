using Microsoft.AspNetCore.Mvc;
using RecursosHumanosWeb.Models.DTOs;
using RecursosHumanosWeb.Models;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RecursosHumanosWeb.Controllers
{
    [Route("ApiKeys/[action]")]
    public class ApiKeysAjaxController : Controller
    {
        private readonly RecursosHumanosContext _context;
        private readonly AutoMapper.IMapper _mapper;

        public ApiKeysAjaxController(RecursosHumanosContext context, AutoMapper.IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

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
    }
}
