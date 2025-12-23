using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.ViewModels.ResetPassword;
using RecursosHumanosWeb.Models.DTOs;
using RecursosHumanosWeb.Helpers;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace RecursosHumanosWeb.Controllers
{
    public class ResetPasswordController : Controller
    {
        private readonly RecursosHumanosContext _context;
        private readonly IMemoryCache _cache;
        private readonly IEmailSender _emailSender;
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<ResetPasswordController> _logger;

        private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(20);

        public ResetPasswordController(RecursosHumanosContext context, IMemoryCache cache, IEmailSender emailSender, IOptions<SmtpSettings> smtpOptions, ILogger<ResetPasswordController> logger)
        {
            _context = context;
            _cache = cache;
            _emailSender = emailSender;
            _smtpSettings = smtpOptions.Value;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult RequestToken()
        {
            return View(new RequestTokenViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Accept DTO via normal model binding (form posts / FormData). Avoid [FromBody] because multipart/form-data
        // requests (forms) will cause a 415 when ASP.NET cannot select an input formatter.
        public async Task<IActionResult> RequestToken(Models.DTOs.RequestTokenRequest? dto)
        {
            // Support both JSON (AJAX) and form posts: if dto is null, try bind from form
            if (dto == null)
            {
                var form = new RequestTokenViewModel();
                await TryUpdateModelAsync(form);
                dto = new Models.DTOs.RequestTokenRequest { Email = form.Email };
            }

            // Validate
            if (string.IsNullOrWhiteSpace(dto.Email) || !new EmailAddressAttribute().IsValid(dto.Email))
            {
                var bad = new AlertResponseDTO { Success = false, Title = "Error", Message = "Correo inválido.", Icon = "error" };
                return Json(bad);
            }

            var emailNormalized = dto.Email.Trim();

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == emailNormalized && u.Estatus);
            // Do not reveal whether email exists

            if (user == null)
            {
                var resp = new AlertResponseDTO { Success = true, Title = "Enviado", Message = "Si la cuenta existe, se ha enviado un enlace de restablecimiento al correo proporcionado.", Icon = "info" };
                return Json(resp);
            }

            // Generate selector + verifier
            var selector = GenerateRandomString(12);
            var verifier = GenerateRandomString(32);
            var tokenHash = ComputeSha256Hash(verifier);

            var token = new ResetPasswordToken
            {
                Selector = selector,
                TokenHash = tokenHash,
                Utilizado = false,
                IdUsuarioSolicita = user.Id,
                IdUsuarioCrea = null,
                IdUsuarioModifica = null,
                Estatus = true,
                FechaCreacion = DateTime.Now,
                FechaCaducidad = DateTime.Now.Add(TokenLifetime)
            };

            _context.ResetPasswordTokens.Add(token);
            await _context.SaveChangesAsync();

            var link = Url.Action(nameof(ResetPassword), "ResetPassword", new { sel = selector, tok = verifier }, Request.Scheme)!;

            var emailModel = new RequestTokenEmailModel
            {
                Email = dto.Email,
                Selector = selector,
                Verifier = verifier,
                Link = link,
                ExpirationMinutes = (int)TokenLifetime.TotalMinutes
            };

            var sent = await _emailSender.SendTokenEmailAsync(emailModel);
            if (!sent)
            {
                var fail = new AlertResponseDTO { Success = false, Title = "Error", Message = "No se pudo enviar el correo. Intente más tarde.", Icon = "error" };
                return Json(fail);
            }

            var ok = new AlertResponseDTO { Success = true, Title = "Enviado", Message = "Si la cuenta existe, se ha enviado un enlace de restablecimiento al correo proporcionado.", Icon = "success", RedirectUrl = Url.Action("Login", "Account") };
            return Json(ok);
        }

        [HttpGet]
        public IActionResult ConfirmToken()
        {
            var vm = new ConfirmTokenViewModel
            {
                Email = TempData["ResetEmail"] as string
            };
            return View(vm);
        }

        [HttpGet]
        public IActionResult ResetPassword(string sel, string tok)
        {
            if (string.IsNullOrEmpty(sel) || string.IsNullOrEmpty(tok))
            {
                return RedirectToAction(nameof(RequestToken));
            }

            // Validate selector and token quickly to populate email and avoid ModelState invalid on POST
            var tokenEntry = _context.ResetPasswordTokens
                .Where(t => t.Selector == sel && t.Utilizado == false && t.Estatus == true)
                .OrderByDescending(t => t.FechaCreacion)
                .FirstOrDefault();

            if (tokenEntry == null)
            {
                TempData["ResetPasswordMessage"] = "Token inválido o no encontrado.";
                return RedirectToAction(nameof(RequestToken));
            }

            if (tokenEntry.FechaCaducidad < DateTime.Now)
            {
                TempData["ResetPasswordMessage"] = "Token expirado.";
                return RedirectToAction(nameof(RequestToken));
            }

            // Compare hash of provided tok
            var providedHash = ComputeSha256Hash(tok);
            if (!string.Equals(providedHash, tokenEntry.TokenHash, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ResetPasswordMessage"] = "Token inválido.";
                return RedirectToAction(nameof(RequestToken));
            }

            // Populate Email for the view model to satisfy validation and show user (non-sensitive)
            var user = _context.Usuarios.Find(tokenEntry.IdUsuarioSolicita);
            var vm = new ResetPasswordViewModel
            {
                Selector = sel,
                Token = tok,
                Email = user?.Correo ?? string.Empty
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(m => m.Value.Errors.Any())
                    .ToDictionary(k => k.Key, v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray());

                // Log ModelState and submitted form keys for debugging
                try
                {
                    _logger.LogWarning("ResetPassword POST - ModelState invalid. Errors: {Errors}", errors);

                    // Log form values (may be empty for AJAX JSON), but useful when FormData is used
                    if (Request.HasFormContentType)
                    {
                        var formKeys = Request.Form.Keys.ToDictionary(k => k, k => Request.Form[k].ToString());
                        _logger.LogWarning("ResetPassword POST - Form values: {FormKeys}", formKeys);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error logging ModelState or form when ResetPassword POST invalid.");
                }

                var bad = new AlertResponseDTO
                {
                    Success = false,
                    Title = "Error",
                    Message = "Datos inválidos",
                    Icon = "error",
                    Errors = errors
                };

                return Json(bad);
            }

            // Find token by selector
            var tokenEntry = await _context.ResetPasswordTokens
                .Where(t => t.Selector == model.Selector && t.Utilizado == false && t.Estatus == true)
                .OrderByDescending(t => t.FechaCreacion)
                .FirstOrDefaultAsync();

            if (tokenEntry == null)
            {
                var bad = new AlertResponseDTO { Success = false, Title = "Error", Message = "Token inválido o ya usado.", Icon = "error" };
                return Json(bad);
            }

            if (tokenEntry.FechaCaducidad < DateTime.Now)
            {
                var bad = new AlertResponseDTO { Success = false, Title = "Error", Message = "Token expirado.", Icon = "error" };
                return Json(bad);
            }

            var providedHash = ComputeSha256Hash(model.Token);
            if (!string.Equals(providedHash, tokenEntry.TokenHash, StringComparison.OrdinalIgnoreCase))
            {
                var bad = new AlertResponseDTO { Success = false, Title = "Error", Message = "Token inválido.", Icon = "error" };
                return Json(bad);
            }

            // Token valid -> update user password
            var user = await _context.Usuarios.FindAsync(tokenEntry.IdUsuarioSolicita);
            if (user == null)
            {
                var bad = new AlertResponseDTO { Success = false, Title = "Error", Message = "Usuario no encontrado.", Icon = "error" };
                return Json(bad);
            }

            user.Clave = PasswordHasher.HashSha256(model.NewPassword);
            tokenEntry.Utilizado = true;
            tokenEntry.IdUsuarioModifica = user.Id;
            tokenEntry.FechaModificacion = DateTime.Now;

            _context.Update(user);
            _context.Update(tokenEntry);
            await _context.SaveChangesAsync();

            var successMessage = "Contraseña actualizada correctamente.";

            var ok = new AlertResponseDTO { Success = true, Title = "Éxito", Message = successMessage, Icon = "success", RedirectUrl = Url.Action("Login", "Account") };
            return Json(ok);
        }

        // Debug endpoint to inspect incoming request payload and model binding
        [HttpPost]
        [Route("ResetPassword/DebugEcho")]
        public async Task<IActionResult> DebugEcho()
        {
            try
            {
                var result = new Dictionary<string, object?>();

                if (Request.HasFormContentType)
                {
                    var form = Request.Form.ToDictionary(k => k.Key, v => (object?)v.Value.ToString());
                    result["form"] = form;
                }
                else
                {
                    Request.Body.Seek(0, System.IO.SeekOrigin.Begin);
                    using var reader = new System.IO.StreamReader(Request.Body, Encoding.UTF8, true, 1024, true);
                    var body = await reader.ReadToEndAsync();
                    result["body"] = body;
                }

                var vm = new ResetPasswordViewModel();
                await TryUpdateModelAsync(vm);
                result["boundModel"] = vm;

                var errors = ModelState.Where(m => m.Value.Errors.Any()).ToDictionary(k => k.Key, v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                result["modelStateErrors"] = errors;

                return Json(new { ok = true, data = result });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DebugEcho error");
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // Helpers
        private static string GenerateRandomString(int bytes)
        {
            var b = new byte[bytes];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(b);
            return Convert.ToBase64String(b).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        private static string ComputeSha256Hash(string raw)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(raw);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var x in hash)
                sb.Append(x.ToString("X2"));
            return sb.ToString();
        }
    }
}
