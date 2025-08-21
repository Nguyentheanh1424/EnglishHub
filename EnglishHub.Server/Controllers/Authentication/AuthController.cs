using EnglishHub.Server.Controllers.Authentication.Dto;
using EnglishHub.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishHub.Server.Controllers.Authentication
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            if (request == null ||
                string.IsNullOrEmpty(request.Username) ||
                string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Invalid registration request.");
            }
            try
            {
                await _authService.RegisterAsync(request.Username, request.Email, request.Password);
                return Ok("Registration successful.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Registration failed: {ex.Message}");
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            if (request == null ||
                string.IsNullOrEmpty(request.Username) ||
                string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Invalid login request.");
            }

            try
            {
                var tokens = await _authService.LoginAsync(request.Username, request.Password);

                var res = new TokenResponseDto
                {
                    Token = tokens.accessToken,
                    RefreshToken = tokens.refreshToken
                };

                return Ok(res);
            }
            catch (Exception ex)
            {
                return Unauthorized($"Login failed: {ex.Message}");
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto request)
        {
            if (request == null ||
                string.IsNullOrEmpty(request.RefreshToken) ||
                string.IsNullOrEmpty(request.UserId))
            {
                return BadRequest("Invalid data to refresh token request.");
            }

            try
            {
                var tokens = await _authService.RefreshTokensAsync(request.RefreshToken, request.UserId);

                var res = new TokenResponseDto
                {
                    Token = tokens.accessToken,
                    RefreshToken = tokens.refreshToken
                };

                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error refreshing token: {ex.Message}");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> LogoutAsync([FromBody] LogoutDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest("Invalid data to logout");
            }

            try
            {
                await _authService.LogoutAsync(request.RefreshToken);
                return Ok("Logout successful");
            }
            catch (Exception ex)
            {
                return BadRequest($"Logout failed: {ex.Message}");
            }
        }
    }
}
