using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IPasswordService _password;

        public AuthController(AppDbContext db, IPasswordService password)
        {
            _db = db;
            _password = password;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict(new { message = "Email already registered." });

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == dto.Role);
            if (role == null)
                return BadRequest(new { message = "Role must be Organiser or Participant." });

            var user = new User
            {
                RoleId = role.RoleId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = _password.Hash(dto.Password),
                PhoneNumber = dto.PhoneNumber,
                DateOfBirth = dto.DateOfBirth
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            SetSession(user, role.RoleName);

            return StatusCode(201, new
            {
                user.UserId,
                user.FirstName,
                user.LastName,
                user.Email,
                Role = role.RoleName
            });
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !_password.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            SetSession(user, user.Role.RoleName);

            return Ok(new
            {
                user.UserId,
                user.FirstName,
                user.LastName,
                user.Email,
                Role = user.Role.RoleName
            });
        }

        // POST /api/auth/logout
        [HttpPost("logout")]
        [SessionAuthorize]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Session ended." });
        }

        private void SetSession(User user, string roleName)
        {
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Role", roleName);
            HttpContext.Session.SetString("FullName", $"{user.FirstName} {user.LastName}");
        }
    }
}