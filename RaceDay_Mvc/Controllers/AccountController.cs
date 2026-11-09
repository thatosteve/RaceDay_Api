using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Extensions;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiClient _api;
        public AccountController(ApiClient api) => _api = api;

        [HttpGet]
        public IActionResult Login(string? returnUrl = null) =>
            HttpContext.Session.IsLoggedIn()
                ? RedirectToHome(HttpContext.Session.GetRole())
                : View(new LoginViewModel { ReturnUrl = returnUrl });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var res = await _api.PostAsync<ApiUser>("api/auth/login", new { vm.Email, vm.Password });
            if (!res.Success || res.Data == null)
            {
                ModelState.AddModelError("", res.Error ?? "Login failed.");
                return View(vm);
            }

            StartSession(res.Data);
            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);
            return RedirectToHome(res.Data.Role);
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var res = await _api.PostAsync<ApiUser>("api/auth/register", new
            {
                vm.FirstName, vm.LastName, vm.Email, vm.Password, vm.Role, vm.PhoneNumber, vm.DateOfBirth
            });
            if (!res.Success || res.Data == null)
            {
                ModelState.AddModelError("", res.Error ?? "Registration failed.");
                return View(vm);
            }

            StartSession(res.Data);   // the API already started a session on register
            return RedirectToHome(res.Data.Role);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _api.PostAsync<object>("api/auth/logout");
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        private void StartSession(ApiUser u) =>
            HttpContext.Session.StartUser(u.UserId, u.Role, $"{u.FirstName} {u.LastName}");

        private IActionResult RedirectToHome(string? role) =>
            role == "Organiser"
                ? RedirectToAction("Index", "Organiser")     // dashboard (next batch)
                : RedirectToAction("Index", "Home");
    }
}
