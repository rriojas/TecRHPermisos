using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.DTOs;
using RecursosHumanosWeb.Models.ViewModels;
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList.Extensions;
using AutoMapper.QueryableExtensions;

namespace RecursosHumanosWeb.Controllers
{
    public class CortesController : Controller
    {
        private readonly RecursosHumanosContext _context;
        private readonly AutoMapper.IMapper _mapper;

        public CortesController(RecursosHumanosContext context, AutoMapper.IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? usuarioCreadorFilter = null, bool? statusFilter = null)
        {
            // Validar parámetros de paginación
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 5;

            // Obtener información del usuario autenticado
            bool isAdministrador = User.HasClaim("TipoUsuario", "4");
            bool isRecursosHumanos = User.HasClaim("TipoUsuario", "2");

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

            // Nota: ya no obtenemos la lista completa de usuarios aquí.
            // El filtro de usuario en la vista utilizará un endpoint centralizado en UsuariosController (SearchUsers)
            var usuarios = new List<UsuariosDTO>();

            // Preparar la consulta filtrada usando AutoMapper ProjectTo a CortesDTO
            var cortesQueryFiltered = cortesQuery
                .ProjectTo<CortesDTO>(_mapper.ConfigurationProvider)
                .OrderByDescending(c => c.Id); // Ordenar por Id descendente

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
            ViewData["IsRH"] = isRecursosHumanos; // Añadido para que la vista lo use
            ViewBag.FechaDesde = fechaDesde;
            ViewBag.FechaHasta = fechaHasta;
            ViewBag.UsuarioCreadorFilter = usuarioCreadorFilter;
            ViewBag.StatusFilter = statusFilter;

            return View(model);
        }


        // ... resto del código del controlador

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(int id)
        {
            try
            {
                // Obtener el corte
                var corte = await _context.Cortes
                    .Include(c => c.IdUsuarioCreaNavigation)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (corte == null)
                {
                    return NotFound("Corte no encontrado");
                }

                // Obtener todas las áreas
                var areas = await _context.Areas
                    .OrderBy(a => a.Descripcion)
                    .ToListAsync();

                if (!areas.Any())
                {
                    return BadRequest("No hay áreas disponibles para exportar");
                }

                // Crear el libro de Excel
                using (var workbook = new XLWorkbook())
                {
                    // Procesar cada área
                    foreach (var area in areas)
                    {
                        // Consulta para permisos generales (excluye tipo 5)
                        var permisosGenerales = await _context.Permisos
                            .Where(p => p.IdCorte == id &&
                                        p.IdUsuarioSolicitaNavigation.IdArea == area.Id &&
                                        p.IdTipoPermiso != 5 &&
                                        p.Estatus != false)
                            .Include(p => p.IdUsuarioSolicitaNavigation)
                            .Include(p => p.IdUsuarioAutorizaNavigation)
                            .Include(p => p.IdTipoPermisoNavigation)
                            .Select(p => new
                            {
                                ID_SOLICITUD = p.Id,
                                NOMBRE_DEL_SOLICITANTE = p.IdUsuarioSolicitaNavigation.Nombre,
                                ID_TIPO_PERMISO = p.IdTipoPermiso,
                                TIPO_PERMISO = p.IdTipoPermisoNavigation.Descripcion,
                                DESCRIPCIÓN = p.Motivo ?? "",
                                // Calcular días entre Fecha1 y Fecha2
                                DÍAS_SOLICITADOS = p.Fecha1.HasValue && p.Fecha2.HasValue 
                                    ? (int)((p.Fecha2.Value - p.Fecha1.Value).TotalDays + 1)
                                    : (p.Dias ?? 0),
                                CON_GOCE = p.Goce ? "Sí" : "No",
                                EVIDENCIA = p.Evidencia ?? "",
                                DESDE_EL_DÍA = p.Fecha1,
                                HASTA_EL_DÍA = p.Fecha2,
                                FECHA_DE_SOLICITUD = p.FechaCreacion,
                                FECHA_DE_AUTORIZACIÓN = p.FechaAutorizacion,
                                ID_SOLICITANTE = p.IdUsuarioSolicita,
                                CORREO_DEL_SOLICITANTE = p.IdUsuarioSolicitaNavigation.Correo
                            })
                            .ToListAsync();

                        // Si hay permisos generales, crear hoja
                        if (permisosGenerales.Any())
                        {
                            var worksheetGeneral = workbook.Worksheets.Add($"Área - {area.Id}");

                            // Agregar encabezados
                            var headers = new[] {
                            "ID_SOLICITUD", "NOMBRE_DEL_SOLICITANTE", "ID_TIPO_PERMISO",
                            "TIPO_PERMISO", "DESCRIPCIÓN", "DÍAS_SOLICITADOS", "CON_GOCE",
                            "EVIDENCIA", "DESDE_EL_DÍA", "HASTA_EL_DÍA",
                            "FECHA_DE_SOLICITUD", "FECHA_DE_AUTORIZACIÓN",
                            "ID_SOLICITANTE", "CORREO_DEL_SOLICITANTE"
                        };

                            for (int i = 0; i < headers.Length; i++)
                            {
                                worksheetGeneral.Cell(1, i + 1).Value = headers[i];
                                worksheetGeneral.Cell(1, i + 1).Style.Font.Bold = true;
                                worksheetGeneral.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                            }

                            // Agregar datos
                            int row = 2;
                            foreach (var permiso in permisosGenerales)
                            {
                                worksheetGeneral.Cell(row, 1).Value = permiso.ID_SOLICITUD;
                                worksheetGeneral.Cell(row, 2).Value = permiso.NOMBRE_DEL_SOLICITANTE;
                                worksheetGeneral.Cell(row, 3).Value = permiso.ID_TIPO_PERMISO;
                                worksheetGeneral.Cell(row, 4).Value = permiso.TIPO_PERMISO;
                                worksheetGeneral.Cell(row, 5).Value = permiso.DESCRIPCIÓN;
                                worksheetGeneral.Cell(row, 6).Value = permiso.DÍAS_SOLICITADOS;
                                worksheetGeneral.Cell(row, 7).Value = permiso.CON_GOCE;
                                worksheetGeneral.Cell(row, 8).Value = permiso.EVIDENCIA;
                                worksheetGeneral.Cell(row, 9).Value = permiso.DESDE_EL_DÍA;
                                worksheetGeneral.Cell(row, 10).Value = permiso.HASTA_EL_DÍA;
                                worksheetGeneral.Cell(row, 11).Value = permiso.FECHA_DE_SOLICITUD;
                                worksheetGeneral.Cell(row, 12).Value = permiso.FECHA_DE_AUTORIZACIÓN;
                                worksheetGeneral.Cell(row, 13).Value = permiso.ID_SOLICITANTE;
                                worksheetGeneral.Cell(row, 14).Value = permiso.CORREO_DEL_SOLICITANTE;
                                row++;
                            }

                            worksheetGeneral.Style.Font.FontName = "Arial";
                            worksheetGeneral.Columns().AdjustToContents();
                        }

                        // Consulta para permisos de turno (tipo 5)
                        var permisosTurno = await _context.Permisos
                            .Where(p => p.IdCorte == id &&
                                        p.IdUsuarioSolicitaNavigation.IdArea == area.Id &&
                                        p.IdTipoPermiso == 5 &&
                                        p.Estatus != false)
                            .Include(p => p.IdUsuarioSolicitaNavigation)
                            .Include(p => p.IdUsuarioAutorizaNavigation)
                            .Include(p => p.IdTipoPermisoNavigation)
                            .Select(p => new
                            {
                                ID_SOLICITUD = p.Id,
                                NOMBRE_DEL_SOLICITANTE = p.IdUsuarioSolicitaNavigation.Nombre,
                                ID_TIPO_PERMISO = p.IdTipoPermiso,
                                TIPO_PERMISO = p.IdTipoPermisoNavigation.Descripcion,
                                DESCRIPCIÓN = p.Motivo ?? "",
                                DÍAS_SOLICITADOS = p.Dias ?? 0,
                                CON_GOCE = p.Goce ? "Sí" : "No",
                                EVIDENCIA = p.Evidencia ?? "",
                                DEL_DÍA = p.Fecha1,
                                POR_EL_DÍA = p.Fecha2,
                                FECHA_DE_SOLICITUD = p.FechaCreacion,
                                FECHA_DE_AUTORIZACIÓN = p.FechaAutorizacion,
                                ID_SOLICITANTE = p.IdUsuarioSolicita,
                                CORREO_DEL_SOLICITANTE = p.IdUsuarioSolicitaNavigation.Correo
                            })
                            .ToListAsync();

                        // Si hay permisos de turno, crear hoja
                        if (permisosTurno.Any())
                        {
                            var worksheetTurno = workbook.Worksheets.Add($"{area.Id} - Turno por Turno");

                            // Agregar encabezados
                            var headersTurno = new[] {
                            "ID_SOLICITUD", "NOMBRE_DEL_SOLICITANTE", "ID_TIPO_PERMISO",
                            "TIPO_PERMISO", "DESCRIPCIÓN", "DÍAS_SOLICITADOS", "CON_GOCE",
                            "EVIDENCIA", "DEL_DÍA", "POR_EL_DÍA",
                            "FECHA_DE_SOLICITUD", "FECHA_DE_AUTORIZACIÓN",
                            "ID_SOLICITANTE", "CORREO_DEL_SOLICITANTE"
                        };

                            for (int i = 0; i < headersTurno.Length; i++)
                            {
                                worksheetTurno.Cell(1, i + 1).Value = headersTurno[i];
                                worksheetTurno.Cell(1, i + 1).Style.Font.Bold = true;
                                worksheetTurno.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
                            }

                            // Agregar datos
                            int row = 2;
                            foreach (var permiso in permisosTurno)
                            {
                                worksheetTurno.Cell(row, 1).Value = permiso.ID_SOLICITUD;
                                worksheetTurno.Cell(row, 2).Value = permiso.NOMBRE_DEL_SOLICITANTE;
                                worksheetTurno.Cell(row, 3).Value = permiso.ID_TIPO_PERMISO;
                                worksheetTurno.Cell(row, 4).Value = permiso.TIPO_PERMISO;
                                worksheetTurno.Cell(row, 5).Value = permiso.DESCRIPCIÓN;
                                worksheetTurno.Cell(row, 6).Value = permiso.DÍAS_SOLICITADOS;
                                worksheetTurno.Cell(row, 7).Value = permiso.CON_GOCE;
                                worksheetTurno.Cell(row, 8).Value = permiso.EVIDENCIA;
                                worksheetTurno.Cell(row, 9).Value = permiso.DEL_DÍA;
                                worksheetTurno.Cell(row, 10).Value = permiso.POR_EL_DÍA;
                                worksheetTurno.Cell(row, 11).Value = permiso.FECHA_DE_SOLICITUD;
                                worksheetTurno.Cell(row, 12).Value = permiso.FECHA_DE_AUTORIZACIÓN;
                                worksheetTurno.Cell(row, 13).Value = permiso.ID_SOLICITANTE;
                                worksheetTurno.Cell(row, 14).Value = permiso.CORREO_DEL_SOLICITANTE;
                                row++;
                            }

                            worksheetTurno.Style.Font.FontName = "Arial";
                            worksheetTurno.Columns().AdjustToContents();
                        }
                    }

                    // Agregar hoja de Tipos de Permisos
                    var tiposPermisos = await _context.TipoPermisos
                        .Select(tp => new { ID_TIPO_PERMISO = tp.Id, DESCRIPCIÓN = tp.Descripcion })
                        .ToListAsync();

                    if (tiposPermisos.Any())
                    {
                        var worksheetTipos = workbook.Worksheets.Add("Tipos De Permisos");
                        worksheetTipos.Cell(1, 1).Value = "ID_TIPO_PERMISO";
                        worksheetTipos.Cell(1, 2).Value = "DESCRIPCIÓN";
                        worksheetTipos.Cell(1, 1).Style.Font.Bold = true;
                        worksheetTipos.Cell(1, 2).Style.Font.Bold = true;
                        worksheetTipos.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
                        worksheetTipos.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.LightYellow;

                        int row = 2;
                        foreach (var tipo in tiposPermisos)
                        {
                            worksheetTipos.Cell(row, 1).Value = tipo.ID_TIPO_PERMISO;
                            worksheetTipos.Cell(row, 2).Value = tipo.DESCRIPCIÓN;
                            row++;
                        }

                        worksheetTipos.Style.Font.FontName = "Arial";
                        worksheetTipos.Style.Font.FontSize = 16;
                        worksheetTipos.Columns().AdjustToContents();
                    }

                    // Agregar hoja de Áreas
                    var areasData = await _context.Areas
                        .Select(a => new { ID = a.Id, DESCRIPCIÓN = a.Descripcion })
                        .ToListAsync();

                    if (areasData.Any())
                    {
                        var worksheetAreas = workbook.Worksheets.Add("Áreas Dentro Del Plantel");
                        worksheetAreas.Cell(1, 1).Value = "ID";
                        worksheetAreas.Cell(1, 2).Value = "DESCRIPCIÓN";
                        worksheetAreas.Cell(1, 1).Style.Font.Bold = true;
                        worksheetAreas.Cell(1, 2).Style.Font.Bold = true;
                        worksheetAreas.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                        worksheetAreas.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.LightGray;

                        int row = 2;
                        foreach (var areaData in areasData)
                        {
                            worksheetAreas.Cell(row, 1).Value = areaData.ID;
                            worksheetAreas.Cell(row, 2).Value = areaData.DESCRIPCIÓN;
                            row++;
                        }

                        worksheetAreas.Style.Font.FontName = "Arial";
                        worksheetAreas.Style.Font.FontSize = 16;
                        worksheetAreas.Columns().AdjustToContents();
                    }

                    // Preparar el archivo para descargar
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();

                        string fileName = $"CorteTECNM - {corte.Inicia?.ToString("dddd, dd 'de' MMMM 'del' yyyy") ?? "Sin fecha"}.xlsx";

                        return File(
                            content,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al exportar: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToCsv(int id)
        {
            try
            {
                var corte = await _context.Cortes
                    .Include(c => c.IdUsuarioCreaNavigation)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (corte == null)
                {
                    return NotFound("Corte no encontrado");
                }

                var permisos = await _context.Permisos
                    .Where(p => p.IdCorte == id && p.Estatus != false)
                    .Include(p => p.IdUsuarioSolicitaNavigation)
                        .ThenInclude(u => u.IdAreaNavigation)
                    .Include(p => p.IdUsuarioAutorizaNavigation)
                    .Include(p => p.IdTipoPermisoNavigation)
                    .ToListAsync();

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("ID,Solicitante,Área,Tipo Permiso,Motivo,Días,Con Goce,Del Día,Al Día,Revisado,Autorizado Por,Fecha Creación");

                foreach (var p in permisos)
                {
                    csv.AppendLine($"{p.Id}," +
                        $"\"{p.IdUsuarioSolicitaNavigation?.Nombre}\"," +
                        $"\"{p.IdUsuarioSolicitaNavigation?.IdAreaNavigation?.Descripcion}\"," +
                        $"\"{p.IdTipoPermisoNavigation?.Descripcion}\"," +
                        $"\"{p.Motivo?.Replace("\"", "\"\"")}\"," +
                        $"{p.Dias ?? 0}," +
                        $"{(p.Goce ? "Sí" : "No")}," +
                        $"{p.Fecha1?.ToString("yyyy-MM-dd")}," +
                        $"{p.Fecha2?.ToString("yyyy-MM-dd")}," +
                        $"{(p.Revisado ? "Sí" : "No")}," +
                        $"\"{p.IdUsuarioAutorizaNavigation?.Nombre}\"," +
                        $"{p.FechaCreacion.ToString("yyyy-MM-dd HH:mm")}");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                string fileName = $"Corte_{corte.Inicia?.ToString("yyyyMMdd") ?? "SinFecha"}.csv";

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al exportar: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToJson(int id)
        {
            try
            {
                var corte = await _context.Cortes
                    .Include(c => c.IdUsuarioCreaNavigation)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (corte == null)
                {
                    return NotFound("Corte no encontrado");
                }

                var permisos = await _context.Permisos
                    .Where(p => p.IdCorte == id && p.Estatus != false)
                    .Include(p => p.IdUsuarioSolicitaNavigation)
                        .ThenInclude(u => u.IdAreaNavigation)
                    .Include(p => p.IdUsuarioAutorizaNavigation)
                    .Include(p => p.IdTipoPermisoNavigation)
                    .Select(p => new
                    {
                        Id = p.Id,
                        Solicitante = p.IdUsuarioSolicitaNavigation.Nombre,
                        Area = p.IdUsuarioSolicitaNavigation.IdAreaNavigation.Descripcion,
                        TipoPermiso = p.IdTipoPermisoNavigation.Descripcion,
                        Motivo = p.Motivo,
                        Dias = p.Dias,
                        ConGoce = p.Goce,
                        DelDia = p.Fecha1,
                        AlDia = p.Fecha2,
                        Revisado = p.Revisado,
                        AutorizadoPor = p.IdUsuarioAutorizaNavigation != null ? p.IdUsuarioAutorizaNavigation.Nombre : null,
                        FechaCreacion = p.FechaCreacion
                    })
                    .ToListAsync();

                var data = new
                {
                    Corte = new
                    {
                        Id = corte.Id,
                        FechaInicio = corte.Inicia,
                        FechaFin = corte.Termina,
                        Estatus = corte.Estatus
                    },
                    Permisos = permisos,
                    Estadisticas = new
                    {
                        Total = permisos.Count,
                        Aprobados = permisos.Count(p => p.Revisado),
                        Pendientes = permisos.Count(p => !p.Revisado),
                        DiasPromedio = permisos.Any() ? permisos.Average(p => p.Dias ?? 0) : 0
                    }
                };

                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                string fileName = $"Corte_{corte.Inicia?.ToString("yyyyMMdd") ?? "SinFecha"}.json";

                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al exportar: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToXml(int id)
        {
            try
            {
                var corte = await _context.Cortes
                    .Include(c => c.IdUsuarioCreaNavigation)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (corte == null)
                {
                    return NotFound("Corte no encontrado");
                }

                var permisos = await _context.Permisos
                    .Where(p => p.IdCorte == id && p.Estatus != false)
                    .Include(p => p.IdUsuarioSolicitaNavigation)
                        .ThenInclude(u => u.IdAreaNavigation)
                    .Include(p => p.IdUsuarioAutorizaNavigation)
                    .Include(p => p.IdTipoPermisoNavigation)
                    .ToListAsync();

                using (var stringWriter = new StringWriter())
                using (var xmlWriter = System.Xml.XmlWriter.Create(stringWriter, new System.Xml.XmlWriterSettings { Indent = true }))
                {
                    xmlWriter.WriteStartDocument();
                    xmlWriter.WriteStartElement("Corte");
                    
                    xmlWriter.WriteElementString("Id", corte.Id.ToString());
                    xmlWriter.WriteElementString("FechaInicio", corte.Inicia?.ToString("yyyy-MM-dd") ?? "");
                    xmlWriter.WriteElementString("FechaFin", corte.Termina?.ToString("yyyy-MM-dd") ?? "");
                    xmlWriter.WriteElementString("Estatus", corte.Estatus.ToString());

                    xmlWriter.WriteStartElement("Estadisticas");
                    xmlWriter.WriteElementString("Total", permisos.Count.ToString());
                    xmlWriter.WriteElementString("Aprobados", permisos.Count(p => p.Revisado).ToString());
                    xmlWriter.WriteElementString("Pendientes", permisos.Count(p => !p.Revisado).ToString());
                    xmlWriter.WriteElementString("DiasPromedio", (permisos.Any() ? permisos.Average(p => p.Dias ?? 0) : 0).ToString("F2"));
                    xmlWriter.WriteEndElement(); // Estadisticas

                    xmlWriter.WriteStartElement("Permisos");
                    foreach (var p in permisos)
                    {
                        xmlWriter.WriteStartElement("Permiso");
                        xmlWriter.WriteElementString("Id", p.Id.ToString());
                        xmlWriter.WriteElementString("Solicitante", p.IdUsuarioSolicitaNavigation?.Nombre ?? "");
                        xmlWriter.WriteElementString("Area", p.IdUsuarioSolicitaNavigation?.IdAreaNavigation?.Descripcion ?? "");
                        xmlWriter.WriteElementString("TipoPermiso", p.IdTipoPermisoNavigation?.Descripcion ?? "");
                        xmlWriter.WriteElementString("Motivo", p.Motivo ?? "");
                        xmlWriter.WriteElementString("Dias", (p.Dias ?? 0).ToString());
                        xmlWriter.WriteElementString("ConGoce", p.Goce ? "Sí" : "No");
                        xmlWriter.WriteElementString("DelDia", p.Fecha1?.ToString("yyyy-MM-dd") ?? "");
                        xmlWriter.WriteElementString("AlDia", p.Fecha2?.ToString("yyyy-MM-dd") ?? "");
                        xmlWriter.WriteElementString("Revisado", p.Revisado ? "Sí" : "No");
                        xmlWriter.WriteElementString("AutorizadoPor", p.IdUsuarioAutorizaNavigation?.Nombre ?? "");
                        xmlWriter.WriteElementString("FechaCreacion", p.FechaCreacion.ToString("yyyy-MM-dd HH:mm"));
                        xmlWriter.WriteEndElement(); // Permiso
                    }
                    xmlWriter.WriteEndElement(); // Permisos

                    xmlWriter.WriteEndElement(); // Corte
                    xmlWriter.WriteEndDocument();

                    var xmlString = stringWriter.ToString();
                    var bytes = System.Text.Encoding.UTF8.GetBytes(xmlString);
                    string fileName = $"Corte_{corte.Inicia?.ToString("yyyyMMdd") ?? "SinFecha"}.xml";

                    return File(bytes, "application/xml", fileName);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al exportar: {ex.Message}");
            }
        }
        // GET: Cortes/Create
        public async Task<IActionResult> Create()
        {
            // Preparar información de usuario para la vista y site.js
            ViewData["IsAuthenticated"] = User.Identity?.IsAuthenticated ?? false;
            ViewData["UserName"] = User.Identity?.Name ?? string.Empty;
            ViewBag.IsRH = User.HasClaim("TipoUsuario", "2");
            ViewBag.IsAdministrador = User.HasClaim("TipoUsuario", "4");
            ViewData["IdUsuario"] = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var viewModel = new CorteCreateViewModel();

            // Buscar el último corte con fechas (que sería el corte "actual" o "penúltimo")
            var ultimoCorteConFechas = await _context.Cortes
                .Where(c => c.Termina.HasValue)
                .OrderByDescending(c => c.Termina.Value)
                .FirstOrDefaultAsync();

            if (ultimoCorteConFechas != null)
            {
                // Establecer Inicia sugerido como un segundo después de Termina del último corte real
                viewModel.Inicia = ultimoCorteConFechas.Termina.Value.AddSeconds(1);
            }
            else
            {
                // Si no hay cortes previos con fechas, usar la fecha y hora actual
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

            // Obtener el ID del usuario autenticado y la fecha actual
            int idUsuario = int.Parse(ViewData["IdUsuario"].ToString());
            DateTime ahora = DateTime.Now;

            // --- Inicio de la Lógica de Cortes ---

            // 1. Obtener los últimos dos cortes
            // El 'ultimoCorte' DEBE ser el marcador de futuro (Inicia/Termina NULL)
            // El 'penultimoCorte' DEBE ser el corte activo actual (con fechas)
            var ultimosDosCortes = await _context.Cortes
                .OrderByDescending(c => c.Id)
                .Take(2)
                .ToListAsync();

            var ultimoCorte = ultimosDosCortes.FirstOrDefault(); // El marcador de futuro (para actualizar)
            var penultimoCorte = ultimosDosCortes.Skip(1).FirstOrDefault(); // El corte actual (para desactivar)

            // --- Caso 1: Estado Inicial (No hay cortes en la BD) ---
            if (ultimoCorte == null)
            {
                // Validaciones para el primer corte
                if (!viewModel.Inicia.HasValue || !viewModel.Termina.HasValue)
                    ModelState.AddModelError(string.Empty, "Debe proporcionar fechas de inicio y fin.");
                if (viewModel.Termina < viewModel.Inicia)
                    ModelState.AddModelError("Termina", "La fecha de fin no puede ser anterior a la fecha de inicio.");

                if (ModelState.IsValid)
                {
                    // Crear el primer corte *activo*
                    var primerCorte = new Corte
                    {
                        Inicia = viewModel.Inicia,
                        Termina = viewModel.Termina,
                        IdUsuarioCrea = idUsuario,
                        IdUsuarioModifica = idUsuario,
                        FechaCreacion = ahora,
                        FechaModificacion = ahora,
                        Estatus = true // Activo
                    };
                    _context.Add(primerCorte);

                    // Crear el primer marcador de futuro *inactivo*
                    var marcadorFuturo = new Corte
                    {
                        Inicia = null,
                        Termina = null,
                        IdUsuarioCrea = idUsuario,
                        IdUsuarioModifica = idUsuario,
                        FechaCreacion = ahora,
                        FechaModificacion = ahora,
                        Estatus = false // Inactivo
                    };
                    _context.Add(marcadorFuturo);

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                return View(viewModel); // Retorna si el modelo no es válido en estado inicial
            }

            // --- Caso 2: Operación Normal (Ya existen cortes) ---

            // Validar que el 'ultimoCorte' sea realmente un marcador de futuro
            if (ultimoCorte.Inicia.HasValue || ultimoCorte.Termina.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Error de lógica: El último corte en la base de datos no es un marcador de futuro (fechas nulas). No se puede crear un nuevo corte. Contacte al administrador.");
            }

            // Validaciones estándar
            if (!viewModel.Inicia.HasValue || !viewModel.Termina.HasValue)
                ModelState.AddModelError(string.Empty, "Debe proporcionar fechas de inicio y fin.");
            if (viewModel.Termina < viewModel.Inicia)
                ModelState.AddModelError("Termina", "La fecha de fin no puede ser anterior a la fecha de inicio.");

            // Validar que Inicia sea posterior al Termina del corte anterior (penultimoCorte)
            if (penultimoCorte != null && penultimoCorte.Termina.HasValue && viewModel.Inicia <= penultimoCorte.Termina.Value)
            {
                ModelState.AddModelError("Inicia", "La fecha de inicio debe ser posterior a la fecha de fin del corte anterior.");
            }

            if (ModelState.IsValid)
            {
                // 1. Desactivar el corte actual (penultimoCorte)
                // (Solo si existe y no es el primer corte que se crea)
                if (penultimoCorte != null)
                {
                    penultimoCorte.Estatus = false; // Desactivar
                    penultimoCorte.FechaModificacion = ahora;
                    penultimoCorte.IdUsuarioModifica = idUsuario;
                    _context.Update(penultimoCorte);
                }

                // 2. Actualizar el marcador (ultimoCorte) para convertirlo en el NUEVO corte activo
                ultimoCorte.Inicia = viewModel.Inicia;
                ultimoCorte.Termina = viewModel.Termina;
                ultimoCorte.Estatus = true; // Activar
                ultimoCorte.FechaModificacion = ahora;
                ultimoCorte.IdUsuarioModifica = idUsuario;
                // No se actualiza IdUsuarioCrea, pues el registro ya existía. IdUsuarioModifica indica quién lo activó.
                _context.Update(ultimoCorte);

                // 3. Crear el NUEVO marcador de futuro (inactivo)
                var nuevoMarcadorFuturo = new Corte
                {
                    Inicia = null,
                    Termina = null,
                    IdUsuarioCrea = idUsuario, // Creado por el usuario actual
                    IdUsuarioModifica = idUsuario,
                    FechaCreacion = ahora,
                    FechaModificacion = ahora,
                    Estatus = false // Siempre inactivo
                };
                _context.Add(nuevoMarcadorFuturo);

                // Guardar todos los cambios
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

        // POST: Cortes/DeleteConfirmed/5 (form POST) - borrado lógico
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var corte = await _context.Cortes.FindAsync(id);
            if (corte != null)
            {
                corte.Estatus = false;
                corte.FechaModificacion = DateTime.Now;
                _context.Update(corte);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // AJAX: Delete via fetch with confirmation flow using ActionRequestDTO and AlertResponseDTO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax([FromBody] RecursosHumanosWeb.Models.DTOs.ActionRequestDTO request)
        {
            if (request == null || request.Id <= 0)
            {
                return Json(new AlertResponseDTO { Success = false, Title = "Error", Message = "ID no válido.", Icon = "error" });
            }

            // Authorization: only Administrador can delete cortes
            if (!User.HasClaim("TipoUsuario", "4"))
            {
                return Json(new AlertResponseDTO { Success = false, Title = "Acceso denegado", Message = "No tiene permisos para eliminar cortes.", Icon = "error" });
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
                    Message = $"¿Estás seguro de que deseas eliminar el corte que inicia {corte.Inicia?.ToString("dd/MM/yyyy")}? Esta acción es irreversible.",
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

        private bool CorteExists(int id)
        {
            return _context.Cortes.Any(e => e.Id == id);
        }

        // Reemplaza tu método [HttpGet] Details(int? id) existente por este:

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 1. Obtener el corte
            var corte = await _context.Cortes
                .Include(c => c.IdUsuarioCreaNavigation)
                .Include(c => c.IdUsuarioModificaNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (corte == null)
            {
                return NotFound();
            }

            // 2. Obtener los permisos filtrados y mapearlos a DTO
            var permisos = await _context.Permisos
                .Where(p => p.IdCorte == id && p.Estatus != false)
                .Include(p => p.IdTipoPermisoNavigation)
                .Include(p => p.IdUsuarioSolicitaNavigation)
                    .ThenInclude(u => u.IdAreaNavigation)
                .Include(p => p.IdUsuarioAutorizaNavigation)
                .Include(p => p.IdUsuarioCreaNavigation)
                .Include(p => p.IdUsuarioModificaNavigation)
                .Select(p => new PermisosDTO
                {
                    Id = p.Id,
                    Fecha1 = p.Fecha1,
                    Fecha2 = p.Fecha2,
                    Dias = p.Dias,
                    Motivo = p.Motivo,
                    Evidencia = p.Evidencia,
                    FechaAutorizacion = p.FechaAutorizacion,
                    FechaCreacion = p.FechaCreacion,
                    FechaModificacion = p.FechaModificacion,
                    Revisado = p.Revisado,
                    Goce = p.Goce,
                    Estatus = p.Estatus,
                    IdCorte = p.IdCorte,
                    IdTipoPermiso = p.IdTipoPermiso,
                    IdUsuarioSolicita = p.IdUsuarioSolicita,
                    IdUsuarioAutoriza = p.IdUsuarioAutoriza,
                    IdUsuarioCrea = p.IdUsuarioCrea,
                    IdUsuarioModifica = p.IdUsuarioModifica,
                    Nombre = p.IdUsuarioSolicitaNavigation.Nombre,
                    Correo = p.IdUsuarioSolicitaNavigation.Correo,
                    TipoPermiso = p.IdTipoPermisoNavigation.Descripcion,
                    Area = p.IdUsuarioSolicitaNavigation.IdAreaNavigation.Descripcion,
                    AutorizadoPor = p.IdUsuarioAutorizaNavigation != null ? p.IdUsuarioAutorizaNavigation.Nombre : null,
                    CreadoPor = p.IdUsuarioCreaNavigation != null ? p.IdUsuarioCreaNavigation.Nombre : null,
                    ModificadoPor = p.IdUsuarioModificaNavigation != null ? p.IdUsuarioModificaNavigation.Nombre : null
                })
                .ToListAsync();

            // 3. Calcular KPIs - Usar campo Dias que ya fue calculado al crear
            double diasPromedio = 0;
            if (permisos.Any())
            {
                // Usar el campo Dias que ya contiene el valor calculado
                var permisosConDias = permisos.Where(p => p.Dias.HasValue && p.Dias > 0).ToList();
                diasPromedio = permisosConDias.Any() ? permisosConDias.Average(p => p.Dias.Value) : 0;
            }
            
            int totalPermisos = permisos.Count;
            int aprobados = permisos.Count(p => p.IdUsuarioAutoriza != null);
            int conGoce = permisos.Count(p => p.Goce);
            int pendientesPorRevisar = permisos.Count(p => !p.Revisado); // Cambio: contar los que NO están revisados

            // 4. Datos para gráficos - Permisos por tipo
            var permisosPorTipo = permisos
                .GroupBy(p => p.TipoPermiso ?? "Desconocido")
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

            // 5. Permisos por área
            var permisosPorArea = permisos
                .GroupBy(p => p.Area ?? "Desconocido")
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

            // 6. Tendencia temporal
            var permisosValidos = permisos.Where(p => p.Fecha1.HasValue).ToList();
            var tendencia = permisosValidos
                .GroupBy(p => new { p.Fecha1.Value.Year, p.Fecha1.Value.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1)
                        .ToString("MMMM yyyy", new CultureInfo("es-ES")),

                    Aprobados = g.Count(p => p.IdUsuarioAutoriza != null),
                    Pendientes = g.Count(p => p.IdUsuarioAutoriza == null && p.Revisado == false),
                    Total = g.Count()
                })
                .ToList();

            // 7. Mapa de Calor
            var heatmapData = permisosValidos
                .GroupBy(p => new
                {
                    Day = p.Fecha1.Value.DayOfWeek,
                    Week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                        p.Fecha1.Value,
                        CalendarWeekRule.FirstDay,
                        DayOfWeek.Monday)
                })
                .Select(g => new
                {
                    g.Key.Day,
                    g.Key.Week,
                    Count = g.Count()
                })
                .ToList();

            // 8. Crear el DTO de respuesta
            var model = new CorteDetailsDTO
            {
                Id = corte.Id,
                Inicia = corte.Inicia,
                Termina = corte.Termina,
                Estatus = corte.Estatus,
                FechaCreacion = corte.FechaCreacion,
                FechaModificacion = corte.FechaModificacion,
                UsuarioCrea = corte.IdUsuarioCreaNavigation?.Nombre,
                UsuarioModifica = corte.IdUsuarioModificaNavigation?.Nombre,
                Permisos = permisos,
                TotalPermisos = totalPermisos,
                PermisosAprobados = aprobados,
                PermisosPendientes = pendientesPorRevisar, // Cambio: usar pendientesPorRevisar
                DiasPromedio = diasPromedio,
                ConGoce = conGoce,
                SinGoce = pendientesPorRevisar, // Reutilizar SinGoce para almacenar pendientesPorRevisar
                PermisosPorTipoJson = JsonConvert.SerializeObject(permisosPorTipo),
                PermisosPorAreaJson = JsonConvert.SerializeObject(permisosPorArea),
                TendenciaJson = JsonConvert.SerializeObject(tendencia),
                HeatmapDataJson = JsonConvert.SerializeObject(heatmapData),
                KpiDataJson = JsonConvert.SerializeObject(new
                {
                    total = totalPermisos,
                    aprobados = aprobados,
                    pendientes = pendientesPorRevisar,
                    diasPromedio = diasPromedio
                })
            };

            return View(model);
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