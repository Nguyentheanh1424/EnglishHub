namespace EnglishHub.Server.Controllers.Authentication.Dto
{
    public class RefreshTokenDto
    {
        public required string UserId { get; set; }
        public required string RefreshToken { get; set; }
    }

    public class TokenResponseDto
    {
        public required string Token { get; set; }

        public required string RefreshToken { get; set; }
    }
}
