using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public EnrolmentsController(AppDbContext db) => _db = db;

        private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

        // POST /api/events/{eventId}/enrolments
        [HttpPost("api/events/{eventId:int}/enrolments")]
        [RoleAuthorize("Participant")]
        public async Task<IActionResult> Enrol(int eventId, EnrolmentCreateDto dto)
        {
            if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
                return NotFound(new { message = "Event does not exist." });

            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.EventId == eventId);

            if (category == null)
                return BadRequest(new { message = "Invalid category for this event." });

            if (await _db.Enrolments.AnyAsync(e => e.UserId == CurrentUserId && e.EventId == eventId))
                return Conflict(new { message = "Already enrolled for this event." });

            var enrolment = new Enrolment
            {
                UserId = CurrentUserId,
                EventId = eventId,
                CategoryId = dto.CategoryId,
                Status = "Pending"
            };

            _db.Enrolments.Add(enrolment);
            await _db.SaveChangesAsync();
            return StatusCode(201, enrolment);
        }

        // GET /api/enrolments/me
        [HttpGet("api/enrolments/me")]
        [RoleAuthorize("Participant")]
        public async Task<IActionResult> MyEnrolments()
        {
            var userId = CurrentUserId;
            var list = await _db.Enrolments
                .Where(e => e.UserId == userId)
                .OrderBy(e => e.Event.EventDate)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.EventId,
                    EventName = e.Event.Name,
                    EventDate = e.Event.EventDate,
                    e.CategoryId,
                    CategoryName = e.Category.Name,
                    e.Status,
                    e.EnrolledAt
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET /api/events/{eventId}/enrolments
        [HttpGet("api/events/{eventId:int}/enrolments")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> EventEnrolments(int eventId)
        {
            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != CurrentUserId) return StatusCode(403, new { message = "You do not own this resource." });

            // Projection (not entities) so password hashes are never returned
            var list = await _db.Enrolments
                .Where(e => e.EventId == eventId)
                .OrderBy(e => e.EnrolledAt)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.Status,
                    e.EnrolledAt,
                    ParticipantId = e.UserId,
                    ParticipantName = e.User.FirstName + " " + e.User.LastName,
                    ParticipantEmail = e.User.Email,
                    e.CategoryId,
                    CategoryName = e.Category.Name,
                    FinishTime = e.Result != null ? e.Result.FinishTime : null,
                    FinishPosition = e.Result != null ? e.Result.FinishPosition : null
                })
                .ToListAsync();

            return Ok(list);
        }

        // PUT /api/enrolments/{id}/status
        [HttpPut("api/enrolments/{id:int}/status")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> UpdateStatus(int id, EnrolmentStatusUpdateDto dto)
        {
            if (dto.Status != "Pending" && dto.Status != "Confirmed")
                return BadRequest(new { message = "Status must be Pending or Confirmed." });

            var enrolment = await _db.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null) return NotFound();
            if (enrolment.Event.OrganiserId != CurrentUserId) return StatusCode(403, new { message = "You do not own this resource." });

            enrolment.Status = dto.Status;
            await _db.SaveChangesAsync();
            return Ok(enrolment);
        }
    }
}