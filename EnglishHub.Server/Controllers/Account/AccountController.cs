using EnglishHub.Server.Controllers.Account.Dto;
using EnglishHub.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishHub.Server.Controllers.Account
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly UserService _userService;

        public AccountController(EmailService emailService, UserService userService)
        {
            _emailService = emailService;
            _userService = userService;
        }

        [HttpPost("sendOtpUpdateEmail")]
        [Authorize]
        public async Task<IActionResult> SendOtpUpdateEmail([FromBody] SendOtpUpdateEmailDto request)
        {
            if (request == null ||
                string.IsNullOrEmpty(request.NewEmail) ||
                string.IsNullOrEmpty(request.UserId))
            {
                return BadRequest("Invalid request data.");
            }
            try
            {
                await _emailService.SendOTPCodeUpdateEmail(request.UserId, request.NewEmail);
                return Ok("OTP code sent successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error sending OTP code: {ex.Message}");
            }
        }

        [HttpPost("updateEmail")]
        [Authorize]
        public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailDto request)
        {
            if (request == null ||
                string.IsNullOrEmpty(request.UserId) ||
                string.IsNullOrEmpty(request.OtpCode) ||
                string.IsNullOrEmpty(request.NewEmail))
            {
                return BadRequest("Invalid request data.");
            }
            try
            {
                await _userService.UpdateEmailAsync(request.UserId, request.OtpCode, request.NewEmail);
                return Ok("Email updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating email: {ex.Message}");
            }
        }
    }
}
