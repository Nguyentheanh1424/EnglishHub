using EnglishHub.Server.Repositories;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;

namespace EnglishHub.Server.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly EmailCodeRepository _otpCodeRepository;
        private readonly UserRepository _userRepository;

        public EmailService(IConfiguration configuration, EmailCodeRepository otpCodeRepository, UserRepository userRepository)
        {
            _configuration = configuration;
            _otpCodeRepository = otpCodeRepository;
            _userRepository = userRepository;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var host = _configuration["Smtp:Host"];
            int port = int.Parse(_configuration["Smtp:Port"]);
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(username),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mailMessage.To.Add(new MailAddress(toEmail));

            await client.SendMailAsync(mailMessage);
        }

        public async Task<string> GenerateOTPCodeAsync(string userId)
        {
            // Tạo mã OTP ngẫu nhiên 6 ký tự
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var data = new byte[6];
            using var randomNumberGenerator = RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes(data);
            var otpCode = new string(data.Select(b => chars[b % chars.Length]).ToArray());

            // Lưu mã OTP vào cơ sở dữ liệu với thời gian hết hạn
            await _otpCodeRepository.CreateOTPCodeAsync(userId, otpCode);
            return otpCode;
        }

        public async Task<bool> ConfirmOTPCode(string userId, string otpCode)
        {
            var isValid = await _otpCodeRepository.ValidateOTPCodeAsync(userId, otpCode);
            if (isValid)
            {
                // Xóa mã OTP sau khi xác thực thành công
                await _otpCodeRepository.DeleteOTPCodeAsync(userId);
            }
            return isValid;
        }

        public async Task SendOTPCodeUpdateEmail(string userId, string newEmail)
        {
            var otpCode = await GenerateOTPCodeAsync(userId);
            var oldEmail = await _userRepository.GetUserEmailByIdAsync(userId);
            var userName = await _userRepository.GetUserNameByIdAsync(userId);
            var year = DateTime.UtcNow.Year;
            var expiresMinutes = 5;

            var subject = "Xác nhận đổi email tài khoản";

            var htmlBody = $@"
                <!doctype html>
                <html lang='vi'>
                <head>
                    <meta charset='utf-8' />
                    <title>Xác nhận đổi email</title>
                </head>
                <body style='font-family:Arial,sans-serif;background:#f6f7fb;padding:20px;'>
                    <div style='max-width:560px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;
                                box-shadow:0 4px 20px rgba(0,0,0,.06);'>
                    <div style='padding:16px;background:#111;color:#fff;font-weight:700;'>EnglishHub</div>
                    <div style='padding:24px'>
                        <h2 style='margin-top:0;'>Xác nhận đổi email tài khoản</h2>
                        <p>Xin chào {userName},</p>
                        <p>Bạn vừa yêu cầu đổi email đăng nhập từ <b>{oldEmail}</b> sang <b>{newEmail}</b>.</p>

                        <p>Nhập mã xác thực bên dưới để hoàn tất:</p>
                        <div style='font-size:22px;letter-spacing:0.25em;text-align:center;
                                    font-weight:bold;border:2px dashed #111;
                                    border-radius:8px;padding:12px;background:#fafafa;margin:12px 0;'>
                        {otpCode}
                        </div>
                        <p>Mã sẽ hết hạn sau <b>{expiresMinutes} phút</b>.</p>

                        <p style='font-size:12px;color:#666;'>Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email.</p>
                    </div>
                    <div style='padding:16px;text-align:center;font-size:12px;color:#888;'>
                        © {year} EnglishHub
                    </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(oldEmail, subject, htmlBody);
        }
    }
}
