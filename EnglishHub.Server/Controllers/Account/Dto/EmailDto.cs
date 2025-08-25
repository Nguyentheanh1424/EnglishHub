namespace EnglishHub.Server.Controllers.Account.Dto
{
    public class SendOtpUpdateEmailDto
    {
        public required string UserId { get; set; }
        public required string NewEmail { get; set; }
    }

    public class UpdateEmailDto
    {
        public required string UserId { get; set; }
        public required string NewEmail { get; set; }
        public required string OtpCode { get; set; }
    }
}

