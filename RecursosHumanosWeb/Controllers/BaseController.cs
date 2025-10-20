using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RecursosHumanosWeb.Controllers
{
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewData["IsAuthenticated"] = User.Identity.IsAuthenticated;
            ViewData["IsAutorizador"] = User.HasClaim("TipoUsuario", "1");
            ViewData["IsRH"] = User.HasClaim("TipoUsuario", "2");
            ViewData["IsEmpleado"] = User.HasClaim("TipoUsuario", "3");
            ViewData["IsAdministrador"] = User.HasClaim("TipoUsuario", "4");
            ViewData["UserName"] = User.Identity.Name;

            base.OnActionExecuting(context);
        }
    }
}
