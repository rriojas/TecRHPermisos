using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.DTOs;

namespace RecursosHumanosWeb.Controllers
{
    [ApiController]
    [Route("Test")]
    public class TestController : ControllerBase
    {
        private readonly RecursosHumanosContext _context;

        public TestController(RecursosHumanosContext context)
        {
            _context = context;
        }

        // GET: /Test/RunCase?case=1
        [HttpGet("RunCase")]
        public async Task<IActionResult> RunCase(int @case = 1, bool confirmed = false)
        {
            // Buscar corte activo
            var corteActivo = await _context.Cortes.Where(c => c.Estatus).OrderByDescending(c => c.Id).FirstOrDefaultAsync();
            if (corteActivo == null)
                return BadRequest(new AlertResponseDTO { Success = false, Title = "Error de Corte", Message = "No se encontró un corte activo." });

            // Usuario RH de pruebas (si existe)
            var usuario = await _context.Usuarios.Where(u => u.IdTipoUsuario == 2 && u.Estatus).FirstOrDefaultAsync();
            if (usuario == null)
                return BadRequest(new AlertResponseDTO { Success = false, Title = "Error de Usuario", Message = "No se encontró un usuario RH de prueba." });

            if (@case == 1)
            {
                // Caso 1: Fecha1 anterior al inicio del corte
                var fecha1 = corteActivo.Inicia.AddHours(-1);
                if (fecha1 < corteActivo.Inicia)
                {
                    var dto = new AlertResponseDTO { Success = false, Title = "Error de Validación", Message = "La fecha y hora de inicio debe estar dentro del periodo del corte vigente." };
                    dto.Errors = new System.Collections.Generic.Dictionary<string, string[]>{{"Fecha1", new[]{"La fecha y hora de inicio debe estar dentro del periodo del corte vigente."}}};
                    return Ok(dto);
                }
            }

            if (@case == 2)
            {
                // Caso 2: Fecha2 posterior al término del corte
                var fecha1 = corteActivo.Inicia.AddDays(1);
                var fecha2 = corteActivo.Termina.AddDays(2);
                if (fecha2 > corteActivo.Termina && !confirmed)
                {
                    return Ok(new AlertResponseDTO
                    {
                        Success = false,
                        ShowConfirmation = true,
                        Title = "Fechas fuera del corte",
                        Message = "Las fechas proporcionadas exceden el periodo del corte vigente. ¿Desea crear el permiso de todas formas?",
                        Icon = "warning",
                        ConfirmButtonText = "Sí, crear permiso"
                    });
                }

                if (confirmed)
                {
                    var permiso = new Permiso
                    {
                        Motivo = "Test confirmado fuera de corte",
                        Fecha1 = corteActivo.Inicia.AddDays(1),
                        Fecha2 = corteActivo.Termina.AddDays(2),
                        Dias = (int)((corteActivo.Termina.AddDays(2) - corteActivo.Inicia.AddDays(1)).TotalDays + 1),
                        IdCorte = corteActivo.Id,
                        IdTipoPermiso = 2,
                        IdUsuarioSolicita = usuario.Id,
                        IdUsuarioCrea = usuario.Id,
                        IdUsuarioModifica = usuario.Id,
                        FechaCreacion = DateTime.Now,
                        FechaModificacion = DateTime.Now,
                        Estatus = true,
                        Revisado = false,
                        Goce = false
                    };
                    _context.Permisos.Add(permiso);
                    await _context.SaveChangesAsync();
                    return Ok(new AlertResponseDTO("Permisos", $"Permiso creado (ID: {permiso.Id})") );
                }
            }

            if (@case == 3)
            {
                // Caso 3: Fechas dentro del corte
                var fecha1 = corteActivo.Inicia.AddDays(1).Date;
                var fecha2 = corteActivo.Inicia.AddDays(2).Date;
                var permiso = new Permiso
                {
                    Motivo = "Test dentro del corte",
                    Fecha1 = fecha1,
                    Fecha2 = fecha2,
                    Dias = (int)((fecha2 - fecha1).TotalDays + 1),
                    IdCorte = corteActivo.Id,
                    IdTipoPermiso = 2,
                    IdUsuarioSolicita = usuario.Id,
                    IdUsuarioCrea = usuario.Id,
                    IdUsuarioModifica = usuario.Id,
                    FechaCreacion = DateTime.Now,
                    FechaModificacion = DateTime.Now,
                    Estatus = true,
                    Revisado = false,
                    Goce = false
                };
                _context.Permisos.Add(permiso);
                await _context.SaveChangesAsync();
                return Ok(new AlertResponseDTO("Permisos", $"Permiso creado (ID: {permiso.Id})"));
            }

            return Ok(new AlertResponseDTO { Success = false, Title = "No Test", Message = "Caso no reconocido." });
        }
    }
}
