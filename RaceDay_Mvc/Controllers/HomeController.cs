using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    /// <summary>Participant-facing pages: browse events, event details, enrol, my events, my results.</summary>
    public class HomeController : BaseController
    {
        public HomeController(ApiClient api) : base(api) { }

        [RoleAuthorize("Participant")]
        public async Task<IActionResult> Index(string? type, string? search, DateTime? from, DateTime? to)
        {
            var url = string.IsNullOrWhiteSpace(type) ? "api/events" : $"api/events?type={Uri.EscapeDataString(type)}";
            var events = await Api.GetAsync<List<EventDto>>(url);
            if (events.StatusCode == 401) return SessionExpired();
            if (!events.Success) ViewBag.LoadError = events.Error;

            var mine = await Api.GetAsync<List<MyEnrolmentDto>>("api/enrolments/me");

            var q = (events.Data ?? new()).Where(e => e.EventDate >= DateTime.Today);   // upcoming only
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                              || e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
            if (from.HasValue) q = q.Where(e => e.EventDate.Date >= from.Value.Date);
            if (to.HasValue) q = q.Where(e => e.EventDate.Date <= to.Value.Date);

            return View(new HomeIndexViewModel
            {
                Events = q.OrderBy(e => e.EventDate).ToList(),
                EnrolledEventIds = (mine.Data ?? new()).Select(m => m.EventId).ToHashSet(),
                Type = type, Search = search, From = from, To = to
            });
        }

        [RoleAuthorize]
        public async Task<IActionResult> Details(int id)
        {
            var res = await Api.GetAsync<EventDto>($"api/events/{id}");
            if (res.StatusCode == 401) return SessionExpired();
            if (!res.Success || res.Data == null)
            {
                Flash(false, "", res.StatusCode == 404 ? "We couldn't find that event." : res.Error);
                return HomeRedirect();
            }

            var vm = new EventDetailsViewModel { Event = res.Data, IsParticipant = Role == "Participant" };
            if (vm.IsParticipant)
            {
                var mine = await Api.GetAsync<List<MyEnrolmentDto>>("api/enrolments/me");
                vm.Enrolment = mine.Data?.FirstOrDefault(m => m.EventId == id);
            }
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken, RoleAuthorize("Participant")]
        public async Task<IActionResult> Enrol(int id, int categoryId)
        {
            if (categoryId <= 0)
            {
                Flash(false, "", "Please choose a category before entering.");
                return RedirectToAction(nameof(Details), new { id });
            }

            var res = await Api.PostAsync<object>($"api/events/{id}/enrolments", new { categoryId });
            if (res.StatusCode == 401) return SessionExpired();

            Flash(res.Success, "You're entered! Your enrolment is pending until the organiser confirms it.", res.Error);
            return res.Success ? RedirectToAction(nameof(MyEvents)) : RedirectToAction(nameof(Details), new { id });
        }

        [RoleAuthorize("Participant")]
        public async Task<IActionResult> MyEvents()
        {
            var res = await Api.GetAsync<List<MyEnrolmentDto>>("api/enrolments/me");
            if (res.StatusCode == 401) return SessionExpired();
            if (!res.Success) ViewBag.LoadError = res.Error;
            return View(res.Data ?? new List<MyEnrolmentDto>());
        }

        [RoleAuthorize("Participant")]
        public async Task<IActionResult> MyResults()
        {
            var res = await Api.GetAsync<List<MyResultDto>>("api/results/me");
            if (res.StatusCode == 401) return SessionExpired();
            if (!res.Success) ViewBag.LoadError = res.Error;
            return View(res.Data ?? new List<MyResultDto>());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}
