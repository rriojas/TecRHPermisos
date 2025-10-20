using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace RecursosHumanosWeb.Controllers
{
    public class AuthenticationDataAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                controller.ViewData["IsAuthenticated"] = context.HttpContext.User.Identity.IsAuthenticated;
                controller.ViewData["IsAutorizador"] = context.HttpContext.User.HasClaim("TipoUsuario", "1");
                controller.ViewData["IsRH"] = context.HttpContext.User.HasClaim("TipoUsuario", "2");
                controller.ViewData["IsEmpleado"] = context.HttpContext.User.HasClaim("TipoUsuario", "3");
                controller.ViewData["IsAdministrador"] = context.HttpContext.User.HasClaim("TipoUsuario", "4");
                controller.ViewData["UserName"] = context.HttpContext.User.Identity.Name;
                controller.ViewData["IdUsuario"] = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            base.OnActionExecuting(context);
        }
    }
}