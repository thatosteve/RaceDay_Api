using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    /// <summary>Organiser-only pages: dashboard, events, categories, enrolments, results entry.</summary>
    [RoleAuthorize("Organiser")]
    public class OrganiserController : BaseController
    {
        public OrganiserController(ApiClient api) : base(api) { }

        // ---------- Dashboard & event list ----------
        public async Task<IActionResult> Index()
        {
            var res = await Api.GetAsync<List<OrganiserEventDto>>("api/events/mine");
            if (res.StatusCode == 401) return SessionExpired();
            if (!res.Success) ViewBag.LoadError = res.Error;
            return View(res.Data ?? new List<OrganiserEventDto>());
        }

        public async Task<IActionResult> Events()
        {
            var res = await Api.GetAsync<List<OrganiserEventDto>>("api/events/mine");
            if (res.StatusCode == 401) return SessionExpired();
            if (!res.Success) ViewBag.LoadError = res.Error;
            return View(res.Data ?? new List<OrganiserEventDto>());
        }

        // ---------- Create / edit / delete event ----------
        [HttpGet]
        public IActionResult CreateEvent()
        {
            ViewBag.IsEdit = false;
            return View("EventForm", new EventFormViewModel { EventDate = DateTime.Today.AddDays(30).AddHours(7) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventFormViewModel vm)
        {
            ViewBag.IsEdit = false;
            if (!ModelState.IsValid) return View("EventForm", vm);

            var res = await Api.PostAsync<EventDto>("api/events", ToPayload(vm));
            if (res.StatusCode == 401) return SessionExpired();
            if (!res.Success || res.Data == null)
            {
                ModelState.AddModelError("", res.Error ?? "Could not create the event.");
                return View("EventForm", vm);
            }

            var bannerNote = await UploadBannerIfAny(res.Data.EventId, vm.Banner);
            TempData["Success"] = "Event created. Now add the categories people can enter." + bannerNote;
            return RedirectToAction(nameof(Categories), new { id = res.Data.EventId });
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var (ev, error) = await LoadOwnEvent(id);
            if (error != null) return error;

            ViewBag.IsEdit = true;
            return View("EventForm", new EventFormViewModel
            {
                EventId = ev!.EventId, Name = ev.Name, Description = ev.Description, EventDate = ev.EventDate,
                Location = ev.Location, DistanceKm = ev.DistanceKm, EventType = ev.EventType,
                ExistingBannerUrl = ev.BannerImageUrl
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, EventFormViewModel vm)
        {
            ViewBag.IsEdit = true;
            vm.EventId = id;
            if (!ModelState.IsValid) return View("EventForm", vm);

            var res = await Api.PutAsync<object>($"api/events/{id}", ToPayload(vm));
            if (res.StatusCode == 401) return SessionExpired();
            if (!res.Success)
            {
                ModelState.AddModelError("", res.Error ?? "Could not update the event.");
                return View("EventForm", vm);
            }

            var bannerNote = await UploadBannerIfAny(id, vm.Banner);
            TempData["Success"] = "Event updated." + bannerNote;
            return RedirectToAction(nameof(Events));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var res = await Api.DeleteAsync<object>($"api/events/{id}");
            if (res.StatusCode == 401) return SessionExpired();
            Flash(res.Success, "Event deleted.", res.Error);
            return RedirectToAction(nameof(Events));
        }

        // ---------- Categories ----------
        public async Task<IActionResult> Categories(int id)
        {
            var (ev, error) = await LoadOwnEvent(id);
            return error ?? View(ev);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(int id, string name, string categoryType)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Flash(false, "", "Give the category a name, e.g. Under 20 or 21km.");
                return RedirectToAction(nameof(Categories), new { id });
            }
            var res = await Api.PostAsync<object>($"api/events/{id}/categories", new { name = name.Trim(), categoryType });
            if (res.StatusCode == 401) return SessionExpired();
            Flash(res.Success, "Category added.", res.Error);
            return RedirectToAction(nameof(Categories), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id, int categoryId)
        {
            var res = await Api.DeleteAsync<object>($"api/categories/{categoryId}");
            if (res.StatusCode == 401) return SessionExpired();
            Flash(res.Success, "Category removed.", res.Error);
            return RedirectToAction(nameof(Categories), new { id });
        }

        // ---------- Enrolments ----------
        public async Task<IActionResult> Enrolments(int id)
        {
            var (vm, error) = await LoadEventWithEnrolments(id);
            return error ?? View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, int enrolmentId, string status)
        {
            var res = await Api.PutAsync<object>($"api/enrolments/{enrolmentId}/status", new { status });
            if (res.StatusCode == 401) return SessionExpired();
            Flash(res.Success, $"Enrolment marked {status}.", res.Error);
            return RedirectToAction(nameof(Enrolments), new { id });
        }

        // ---------- Results entry ----------
        public async Task<IActionResult> Results(int id)
        {
            var (vm, error) = await LoadEventWithEnrolments(id);
            return error ?? View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResult(int id, int enrolmentId, int? resultId, string finishTime, int? finishPosition)
        {
            var (ev, error) = await LoadOwnEvent(id);
            if (error != null) return error;

            if (ev!.EventDate > DateTime.Now)
            {
                Flash(false, "", "Results can only be captured once the event has taken place.");
                return RedirectToAction(nameof(Results), new { id });
            }
            if (!Regex.IsMatch(finishTime ?? "", @"^\d{1,2}:[0-5]\d:[0-5]\d$"))
            {
                Flash(false, "", "Enter the finish time as hh:mm:ss, for example 01:45:30.");
                return RedirectToAction(nameof(Results), new { id });
            }
            if (finishPosition is null or < 1)
            {
                Flash(false, "", "Enter a finishing position of 1 or higher.");
                return RedirectToAction(nameof(Results), new { id });
            }

            var body = new { finishTime = TimeSpan.Parse(finishTime!), finishPosition };
            var res = resultId is > 0
                ? await Api.PutAsync<object>($"api/results/{resultId}", body)
                : await Api.PostAsync<object>($"api/enrolments/{enrolmentId}/result", body);

            if (res.StatusCode == 401) return SessionExpired();
            Flash(res.Success, "Result saved and published to the participant.", res.Error);
            return RedirectToAction(nameof(Results), new { id });
        }

        // ---------- helpers ----------
        private static object ToPayload(EventFormViewModel vm) => new
        {
            name = vm.Name.Trim(),
            description = vm.Description,
            eventDate = vm.EventDate,
            location = vm.Location.Trim(),
            distanceKm = vm.DistanceKm,
            eventType = vm.EventType
        };

        private async Task<string> UploadBannerIfAny(int eventId, IFormFile? banner)
        {
            if (banner == null || banner.Length == 0) return "";
            var res = await Api.PostFileAsync<UrlDto>($"api/events/{eventId}/banner", banner);
            return res.Success ? "" : $" The banner was not saved: {res.Error}";
        }

        private async Task<(EventDto? Event, IActionResult? Error)> LoadOwnEvent(int id)
        {
            var res = await Api.GetAsync<EventDto>($"api/events/{id}");
            if (res.StatusCode == 401) return (null, SessionExpired());
            if (!res.Success || res.Data == null)
            {
                Flash(false, "", res.StatusCode == 404 ? "We couldn't find that event." : res.Error);
                return (null, RedirectToAction(nameof(Events)));
            }
            if (res.Data.OrganiserId != UserId)
            {
                Flash(false, "", "That event belongs to another organiser.");
                return (null, RedirectToAction(nameof(Events)));
            }
            return (res.Data, null);
        }

        private async Task<(OrganiserEventViewModel? Vm, IActionResult? Error)> LoadEventWithEnrolments(int id)
        {
            var (ev, error) = await LoadOwnEvent(id);
            if (error != null) return (null, error);

            var en = await Api.GetAsync<List<EventEnrolmentDto>>($"api/events/{id}/enrolments");
            if (en.StatusCode == 401) return (null, SessionExpired());
            if (!en.Success) ViewBag.LoadError = en.Error;
            return (new OrganiserEventViewModel { Event = ev!, Enrolments = en.Data ?? new() }, null);
        }
    }
}
