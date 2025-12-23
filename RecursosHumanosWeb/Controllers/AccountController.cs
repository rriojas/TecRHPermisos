using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecursosHumanosWeb.Models;
using RecursosHumanosWeb.Models.DTOs;
using RecursosHumanosWeb.Models.ViewModels;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace RecursosHumanosWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly RecursosHumanosContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(RecursosHumanosContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 1) Mostrar vista de login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Permisos");

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // 2) Procesar login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Errores de validación: {Errores}", string.Join(" | ", errors));

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Ejecutar SP o consulta con FromSqlRaw
                // The stored procedure hashes the password in the database (HASHBYTES('SHA2_256', CONVERT(VARCHAR(100), @password), 2)),
                // so pass the plain password and let the SP perform hashing/comparison.
                var result = await _context.LoginResultDTO
                    .FromSqlInterpolated($"EXEC dbo.LoginUsuario @mail={model.Correo}, @password={model.Clave}")
                    .ToListAsync();

                var usuario = result.FirstOrDefault();


                // Validar si no se encontró usuario
                if (usuario == null)
                {
                    ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos");
                    return View(model);
                }

                // Validar campos nulos (defensivo)
                if (string.IsNullOrEmpty(usuario.Correo) || string.IsNullOrEmpty(usuario.Nombre))
                {
                    ModelState.AddModelError(string.Empty, "Los datos del usuario son inválidos");
                    return View(model);
                }

                // Crear claims (para el cookie)
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Correo),
            new Claim("TipoUsuario", usuario.IdTipoUsuario.ToString())
        };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme
                );

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe, // "Recordarme"
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) // Expira en 8h
                };

                // Guardar cookie de autenticación
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                // Redirigir a la URL de retorno o a Usuarios/Index
                return Redirect(model.ReturnUrl ?? Url.Action("Index", "Permisos")!);
            }
            catch (Exception ex)
            {
                // Loguear el error en el logger inyectado
                _logger.LogError(ex, "Error procesando el login para {Correo}", model.Correo);

                // Mostrar mensaje genérico al usuario
                ModelState.AddModelError(string.Empty, "Error interno al procesar el inicio de sesión");
                return View(model);
            }
        }


        // 3) Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}