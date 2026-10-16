using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CategoriesController(AppDbContext db) => _db = db;

        private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

        // GET /api/events/{eventId}/categories
        [HttpGet("api/events/{eventId:int}/categories")]
        public async Task<IActionResult> GetForEvent(int eventId)
        {
            if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
                return NotFound(new { message = "Event does not exist." });

            var cats = await _db.Categories
                .Where(c => c.EventId == eventId)
                .ToListAsync();

            return Ok(cats);
        }

        // POST /api/events/{eventId}/categories
        [HttpPost("api/events/{eventId:int}/categories")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> Create(int eventId, CategoryCreateDto dto)
        {
            var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null) return NotFound();
            if (ev.OrganiserId != CurrentUserId) return Forbid();

            if (dto.CategoryType != "Age" && dto.CategoryType != "Distance")
                return BadRequest(new { message = "CategoryType must be Age or Distance." });

            var cat = new Category
            {
                EventId = eventId,
                Name = dto.Name,
                CategoryType = dto.CategoryType
            };

            _db.Categories.Add(cat);
            await _db.SaveChangesAsync();
            return StatusCode(201, cat);
        }

        // PUT /api/categories/{id}
        [HttpPut("api/categories/{id:int}")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> Update(int id, CategoryUpdateDto dto)
        {
            var cat = await _db.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (cat == null) return NotFound();
            if (cat.Event.OrganiserId != CurrentUserId) return Forbid();

            if (dto.CategoryType != "Age" && dto.CategoryType != "Distance")
                return BadRequest(new { message = "CategoryType must be Age or Distance." });

            cat.Name = dto.Name;
            cat.CategoryType = dto.CategoryType;
            await _db.SaveChangesAsync();
            return Ok(cat);
        }

        // DELETE /api/categories/{id}
        [HttpDelete("api/categories/{id:int}")]
        [RoleAuthorize("Organiser")]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _db.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (cat == null) return NotFound();
            if (cat.Event.OrganiserId != CurrentUserId) return Forbid();

            _db.Categories.Remove(cat);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}