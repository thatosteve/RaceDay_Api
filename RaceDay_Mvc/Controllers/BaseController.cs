using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Extensions;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly ApiClient Api;
        protected BaseController(ApiClient api) => Api = api;

        protected int UserId => HttpContext.Session.GetInt32(UserSession.UserIdKey) ?? 0;
        protected string? Role => HttpContext.Session.GetRole();

        /// <summary>The API says our session is gone (e.g. the API restarted): send the user back to login.</summary>
        protected IActionResult SessionExpired()
        {
            HttpContext.Session.Clear();
            TempData["Error"] = "Your session has expired. Please log in again.";
            return RedirectToAction("Login", "Account");
        }

        protected IActionResult HomeRedirect() =>
            Role == "Organiser" ? RedirectToAction("Index", "Organiser") : RedirectToAction("Index", "Home");

        protected void Flash(bool success, string okMessage, string? error) =>
            TempData[success ? "Success" : "Error"] = success ? okMessage : (error ?? "Something went wrong.");
    }
}
