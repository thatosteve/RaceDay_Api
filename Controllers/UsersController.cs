using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [SessionAuthorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UsersController(AppDbContext db) => _db = db;

        private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

        // GET /api/users/me
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);

            if (user == null) return Unauthorized();

            return Ok(new
            {
                user.UserId,
                user.FirstName,
                user.LastName,
                user.Email,
                Role = user.Role.RoleName,
                user.PhoneNumber,
                user.DateOfBirth,
                user.ProfileImageUrl,
                user.CreatedAt
            });
        }

        // PUT /api/users/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
            if (user == null) return Unauthorized();

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;
            user.DateOfBirth = dto.DateOfBirth;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Profile updated." });
        }

        // POST /api/users/me/profile-image  -- placeholder for Part 3 (Azure Blob)
        [HttpPost("me/profile-image")]
        [RoleAuthorize("Participant")]
        public IActionResult UploadProfileImage()
        {
            // Part 3 will implement Azure Blob upload here.
            return Ok(new { message = "Profile image endpoint reserved for Part 3." });
        }
    }
}
