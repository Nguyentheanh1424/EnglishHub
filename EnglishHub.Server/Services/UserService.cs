using EnglishHub.Server.Repositories;

namespace EnglishHub.Server.Services
{
    public class UserService
    {
        private readonly UserRepository _userRepository;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public UserService(UserRepository userRepository, EmailService emailService, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task UpdateEmailAsync(string userId, string otpCode, string newEmail)
        {
            var isValidOtp = await _emailService.ConfirmOTPCode(userId, otpCode);
            if (!isValidOtp)
            {
                throw new InvalidOperationException("Invalid OTP code.");
            }
            var user = await _userRepository.GetUserByIdAsync(userId);
            user.Email = newEmail;
            await _userRepository.UpdateUserAsync(user);
        }
    }
}
