using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ResultsController(AppDbContext db) => _db = db;

        private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

        // POST /api/enrolments/{enrolmentId}/result
        [HttpPost("api/enrolments/{enrolmentId:int}/result")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> Capture(int enrolmentId, ResultCreateDto dto)
        {
            var enrolment = await _db.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

            if (enrolment == null) return NotFound();
            if (enrolment.Event.OrganiserId != CurrentUserId) return Forbid();

            if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
                return Conflict(new { message = "Result already captured." });

            var result = new Result
            {
                EnrolmentId = enrolmentId,
                FinishTime = dto.FinishTime,
                FinishPosition = dto.FinishPosition
            };

            _db.Results.Add(result);
            await _db.SaveChangesAsync();
            return StatusCode(201, result);
        }

        // PUT /api/results/{id}
        [HttpPut("api/results/{id:int}")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> Update(int id, ResultUpdateDto dto)
        {
            var result = await _db.Results
                .Include(r => r.Enrolment).ThenInclude(e => e.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null) return NotFound();
            if (result.Enrolment.Event.OrganiserId != CurrentUserId) return Forbid();

            result.FinishTime = dto.FinishTime;
            result.FinishPosition = dto.FinishPosition;
            await _db.SaveChangesAsync();
            return Ok(result);
        }

        // GET /api/results/me
        [HttpGet("api/results/me")]
        [RoleAuthorize("Participant")]
        public async Task<IActionResult> MyResults()
        {
            var results = await _db.Results
                .Include(r => r.Enrolment).ThenInclude(e => e.Event)
                .Include(r => r.Enrolment).ThenInclude(e => e.Category)
                .Where(r => r.Enrolment.UserId == CurrentUserId)
                .Select(r => new
                {
                    r.ResultId,
                    EventName = r.Enrolment.Event.Name,
                    EventDate = r.Enrolment.Event.EventDate,
                    Category = r.Enrolment.Category.Name,
                    r.FinishTime,
                    r.FinishPosition
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}