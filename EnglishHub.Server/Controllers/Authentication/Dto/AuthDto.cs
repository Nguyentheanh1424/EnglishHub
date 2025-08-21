namespace EnglishHub.Server.Controllers.Authentication.Dto
{
    public class LoginDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class RegisterDto
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class LogoutDto
    {
        public required string RefreshToken { get; set; }
    }
}
