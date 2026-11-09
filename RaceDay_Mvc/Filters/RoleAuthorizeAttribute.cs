using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Mvc.Extensions;

namespace RaceDay.Mvc.Filters
{
    /// <summary>
    /// [RoleAuthorize] = any logged-in user; [RoleAuthorize("Organiser")] = that role only.
    /// Not logged in -> Login page. Wrong role -> Access Denied page.
    /// </summary>
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string? _role;
        public RoleAuthorizeAttribute(string? role = null) => _role = role;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            if (!session.IsLoggedIn())
            {
                context.Result = new RedirectToActionResult("Login", "Account",
                    new { returnUrl = context.HttpContext.Request.Path.ToString() });
                return;
            }

            if (_role != null && session.GetRole() != _role)
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
