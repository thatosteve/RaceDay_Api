using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Auth
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public RoleAuthorizeAttribute(params string[] roles) => _roles = roles;

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            var role = context.HttpContext.Session.GetString("Role");

            if (userId == null)
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Not authenticated." });
                return;
            }

            if (_roles.Length > 0 && !_roles.Contains(role))
            {
                context.Result = new ObjectResult(new { message = "Forbidden." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}