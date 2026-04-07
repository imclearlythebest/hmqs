using Microsoft.AspNetCore.Mvc;
using HMQS.API.DTOs;
using HMQS.API.Services;

namespace HMQS.API.Controllers
{
    // [ApiController] gives us automatic model validation and error responses
    // [Route] sets the base URL for all endpoints in this controller
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        // ASP.NET Core injects AuthService here automatically
        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // [FromBody] tells ASP.NET to read the request body as JSON
            // Model validation runs automatically because of [ApiController]
            // If Username/Email/Password are missing it returns 400 before reaching here

            var result = await _authService.RegisterAsync(dto);

            if (result == null)
                return Conflict(new { message = "Email or username already taken." });
            // 409 Conflict is the correct HTTP status for duplicate resource

            return Ok(result); // 200 with the AuthResponseDto as JSON
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (result == null)
                return Unauthorized(new { message = "Invalid email or password." });
            // 401 Unauthorized - do not tell the user which field was wrong
            // That would help attackers know if an email exists

            return Ok(result);
        }

        // POST api/auth/refresh
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            var result = await _authService.RefreshAsync(refreshToken);

            if (result == null)
                return Unauthorized(new { message = "Invalid or expired refresh token." });

            return Ok(result);
        }
    }
}