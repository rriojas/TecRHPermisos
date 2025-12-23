using Microsoft.AspNetCore.Mvc;
using RecursosHumanosWeb.Models.DTOs;
using RecursosHumanosWeb.Models;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RecursosHumanosWeb.Controllers
{
    [Route("Permisos/[action]")]
    public class PermisosAjaxController : Controller
    {
        private readonly RecursosHumanosContext _context;
        private readonly AutoMapper.IMapper _mapper;

        public PermisosAjaxController(RecursosHumanosContext context, AutoMapper.IMapper mapper)
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

            var permiso = await _context.Permisos
                .Include(p => p.IdUsuarioSolicitaNavigation)
                .FirstOrDefaultAsync(p => p.Id == request.Id);

            if (permiso == null)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "No encontrado", Message = "Permiso no encontrado.", Icon = "error" });
            }

            if (!request.Confirmed)
            {
                return Json(new AlertResponseDTO
                {
                    ShowConfirmation = true,
                    Title = "¿Eliminar permiso?",
                    Message = $"¿Estás seguro de que deseas eliminar el permiso solicitado por {permiso.IdUsuarioSolicitaNavigation?.Nombre ?? "el usuario"} en {permiso.Fecha1:dd/MM/yyyy}?",
                    Icon = "question",
                    ConfirmButtonText = "Sí, eliminar",
                });
            }

            try
            {
                permiso.Estatus = false; // logical delete
                permiso.FechaModificacion = DateTime.Now;
                _context.Update(permiso);
                await _context.SaveChangesAsync();

                return Json(new AlertResponseDTO
                {
                    Success = true,
                    Title = "Eliminado",
                    Message = "El permiso fue inactivado correctamente.",
                    Icon = "success",
                    RedirectUrl = "/Permisos/Index"
                });
            }
            catch (Exception ex)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "Error del Servidor", Message = $"Ocurrió un error al eliminar: {ex.Message}", Icon = "error" });
            }
        }
    }
}
