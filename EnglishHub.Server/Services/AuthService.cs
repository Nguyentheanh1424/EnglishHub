using EnglishHub.Server.Models;
using EnglishHub.Server.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EnglishHub.Server.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;
        private readonly RefreshTokenRepository _refreshTokenRepository;
        private readonly IConfiguration _configuration;

        public AuthService(UserRepository userRepository, RefreshTokenRepository refreshTokenRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _configuration = configuration;
        }

        public async Task<string> RequestRegisterAsync(string username, string email, string password)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
            var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

            // Kiểm tra xem người dùng đã tồn tại chưa qua Email
            var existingUser = await _userRepository.GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("This email has been registered.");
            }

            // Mã hóa mật khẩu
            var hashPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // Sinh token dùng để gửi vào email xác nhận user
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("username", username),
                new Claim("email", email),
                new Claim("hashPassword", hashPassword)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task ConfirmRegisterAsync(string token)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
            var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,

                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidAudience = audience,
                ValidateAudience = true,

                ClockSkew = TimeSpan.Zero
            }, out _);

            var username = principal.FindFirst("username")?.Value;
            var email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var hashPassword = principal.FindFirst("hashPassword")?.Value;

            if (string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(hashPassword))
                throw new InvalidOperationException("Invalid token data");

            var existingUser = await _userRepository.GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("This email has been registered.");
            }

            var user = new User
            {
                Username = username,
                Email = email,
                HashPassword = hashPassword,
                // Role mặc định là User
            };

            await _userRepository.AddUserAsync(user);
        }

        public async Task<(string accessToken, string refreshToken)> LoginAsync(string username, string password)
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Notfound username.");
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.HashPassword);

            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Wrong password.");
            }

            // Thu hồi tất cả RefreshToken, chỉ cho 1 lượt đăng nhập tại 1 thời điểm.
            await _refreshTokenRepository.DeleteRefreshTokenByUserId(user.Id);

            return await GeneratorTokensAsync(user);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            // Kiểm tra Refresh Token và đánh dấu là đã thu hồi
            await _refreshTokenRepository.RevokeRefreshTokenAsync(refreshToken);
        }

        public async Task<(string accessToken, string refreshToken)> GeneratorTokensAsync(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
            var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
            var accessTokenExpirationStr = _configuration["Jwt:AccessTokenExpireMinutes"] ?? throw new InvalidOperationException("Jwt:AccessTokenExpireMinutes is not configured.");
            var refreshTokenExpirationStr = _configuration["Jwt:RefreshTokenExpireDays"] ?? throw new InvalidOperationException("Jwt:RefreshTokenExpireDays is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            // Sử dụng hàm băm một chiều để mã hóa khóa bí mật
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(accessTokenExpirationStr)),
                signingCredentials: creds
            );

            string jwt = new JwtSecurityTokenHandler().WriteToken(token);

            // 2. Tạo Refresh Token
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(refreshTokenExpirationStr))
            };

            await _refreshTokenRepository.AddRefreshTokenAsync(refreshToken);

            return (jwt, refreshToken.Token);
        }

        public async Task<(string accessToken, string refreshToken)> RefreshTokensAsync(string refreshToken, string userId)
        {
            // 1. Kiểm tra Refresh Token
            var token = await _refreshTokenRepository.GetValidRefreshTokenAsync(refreshToken, userId);

            if (token == null)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            // 2. Lấy thông tin người dùng từ Refresh Token
            var user = await _userRepository.GetUserByIdAsync(token.UserId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            // 3. Tạo mới Access Token và Refresh Token và đánh dấu Refresh Token cũ là đã bị thu hồi
            var (newAccessToken, newRefreshToken) = await GeneratorTokensAsync(user);
            await _refreshTokenRepository.RevokeRefreshTokenAsync(refreshToken);

            return (newAccessToken, newRefreshToken);
        }
    }
}
