using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IBlobService _blob;
        public EventsController(AppDbContext db, IBlobService blob) { _db = db; _blob = blob; }

        private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

        // GET /api/events
        [HttpGet]
        [SessionAuthorize]
        public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] DateTime? date)
        {
            var query = _db.Events
                .Include(e => e.Categories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(e => e.EventType == type);

            if (date.HasValue)
                query = query.Where(e => e.EventDate.Date == date.Value.Date);

            var events = await query.OrderBy(e => e.EventDate).ToListAsync();
            return Ok(events);
        }

        // GET /api/events/mine
        /// <summary>Lists the logged-in Organiser's own events with category, enrolment and result counts (used by the dashboard).</summary>
        [HttpGet("mine")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> Mine()
        {
            var userId = CurrentUserId;
            var list = await _db.Events
                .Where(e => e.OrganiserId == userId)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventId,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.DistanceKm,
                    e.EventType,
                    e.BannerImageUrl,
                    CategoryCount = e.Categories.Count(),
                    EnrolmentCount = e.Enrolments.Count(),
                    ResultCount = e.Enrolments.Count(en => en.Result != null)
                })
                .ToListAsync();
            return Ok(list);
        }

        // GET /api/events/{id}
        [HttpGet("{id:int}")]
        [SessionAuthorize]
        public async Task<IActionResult> GetById(int id)
        {
            var ev = await _db.Events
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null) return NotFound();
            return Ok(ev);
        }

        // POST /api/events
        [HttpPost]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> Create(EventCreateDto dto)
        {
            if (!new[] { "run", "walk", "cycle" }.Contains(dto.EventType.ToLower()))
                return BadRequest(new { message = "EventType must be run, walk, or cycle." });

            var ev = new Event
            {
                OrganiserId = CurrentUserId,
                Name = dto.Name,
                Description = dto.Description,
                EventDate = dto.EventDate,
                Location = dto.Location,
                DistanceKm = dto.DistanceKm,
                EventType = dto.EventType.ToLower()
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ev);
        }

        // PUT /api/events/{id}
        [HttpPut("{id:int}")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> Update(int id, EventUpdateDto dto)
        {
            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != CurrentUserId) return StatusCode(403, new { message = "You do not own this resource." });

            if (!new[] { "run", "walk", "cycle" }.Contains(dto.EventType.ToLower()))
                return BadRequest(new { message = "EventType must be run, walk, or cycle." });

            ev.Name = dto.Name;
            ev.Description = dto.Description;
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location;
            ev.DistanceKm = dto.DistanceKm;
            ev.EventType = dto.EventType.ToLower();

            await _db.SaveChangesAsync();
            return Ok(ev);
        }

        // DELETE /api/events/{id}
        [HttpDelete("{id:int}")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _db.Events
                .Include(e => e.Categories)
                .Include(e => e.Enrolments).ThenInclude(en => en.Result)
                .FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != CurrentUserId) return StatusCode(403, new { message = "You do not own this resource." });

            // Foreign keys are RESTRICT, so remove the dependants first (results -> enrolments -> categories -> event)
            foreach (var en in ev.Enrolments)
                if (en.Result != null) _db.Results.Remove(en.Result);
            _db.Enrolments.RemoveRange(ev.Enrolments);
            _db.Categories.RemoveRange(ev.Categories);
            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/events/{id}/banner  (multipart/form-data, field name "file")
        /// <summary>Uploads a banner image for an event owned by the logged-in Organiser and stores it in Azure Blob Storage.</summary>
        /// <response code="200">Returns the stored image URL.</response>
        /// <response code="400">Missing file, wrong type (JPG/PNG/WEBP only) or larger than 5 MB.</response>
        /// <response code="403">The event belongs to another Organiser.</response>
        /// <response code="404">Event not found.</response>
        [HttpPost("{id:int}/banner")]
        [RoleAuthorize("Organiser")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadBanner(int id, IFormFile file)
        {
            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null) return NotFound(new { message = "Event not found." });
            if (ev.OrganiserId != CurrentUserId) return StatusCode(403, new { message = "You do not own this resource." });

            try
            {
                ev.BannerImageUrl = await _blob.UploadImageAsync(file, "banners");
                await _db.SaveChangesAsync();
                return Ok(new { url = ev.BannerImageUrl });
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}