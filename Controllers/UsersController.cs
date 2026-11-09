using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [SessionAuthorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IBlobService _blob;
        public UsersController(AppDbContext db, IBlobService blob) { _db = db; _blob = blob; }

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

        // POST /api/users/me/profile-image  (multipart/form-data, field name "file")
    
        [HttpPost("me/profile-image")]
        [RoleAuthorize("Participant")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
            if (user == null) return Unauthorized();

            try
            {
                user.ProfileImageUrl = await _blob.UploadImageAsync(file, "profiles");
                await _db.SaveChangesAsync();
                return Ok(new { url = user.ProfileImageUrl });
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
