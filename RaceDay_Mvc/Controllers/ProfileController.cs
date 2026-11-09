using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Extensions;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    [RoleAuthorize]
    public class ProfileController : BaseController
    {
        public ProfileController(ApiClient api) : base(api) { }

        private async Task<ProfileViewModel?> LoadAsync()
        {
            var res = await Api.GetAsync<ProfileDto>("api/users/me");
            if (!res.Success || res.Data == null) return null;
            var p = res.Data;
            return new ProfileViewModel
            {
                FirstName = p.FirstName, LastName = p.LastName, PhoneNumber = p.PhoneNumber,
                DateOfBirth = p.DateOfBirth, Email = p.Email, Role = p.Role, ProfileImageUrl = p.ProfileImageUrl
            };
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = await LoadAsync();
            if (vm == null) return SessionExpired();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel vm)
        {
            var current = await LoadAsync();
            if (current == null) return SessionExpired();
            vm.Email = current.Email; vm.Role = current.Role; vm.ProfileImageUrl = current.ProfileImageUrl;

            if (!ModelState.IsValid) return View(vm);

            var res = await Api.PutAsync<object>("api/users/me",
                new { vm.FirstName, vm.LastName, vm.PhoneNumber, vm.DateOfBirth });
            if (res.StatusCode == 401) return SessionExpired();
            if (!res.Success)
            {
                ModelState.AddModelError("", res.Error ?? "Could not update your profile.");
                return View(vm);
            }

            HttpContext.Session.SetString(SessionExtensions.NameKey, $"{vm.FirstName} {vm.LastName}");
            Flash(true, "Profile updated.", null);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, RoleAuthorize("Participant")]
        public async Task<IActionResult> Picture(IFormFile? picture)
        {
            if (picture == null || picture.Length == 0)
            {
                Flash(false, "", "Choose an image first.");
                return RedirectToAction(nameof(Index));
            }

            var res = await Api.PostFileAsync<UrlDto>("api/users/me/profile-image", picture);
            if (res.StatusCode == 401) return SessionExpired();
            Flash(res.Success, "Profile picture updated.", res.Error);
            return RedirectToAction(nameof(Index));
        }
    }
}
