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

        public async Task<User> RequestRegisterAsync(string username, string email, string password)
        {
            // Kiểm tra xem người dùng đã tồn tại chưa
            var existingUser = await _userRepository.GetUserByUsernameAsync(username);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Username already exists.");
            }

            // Mã hóa mật khẩu
            var hashPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // Tạo người dùng mới
            var user = new User
            {
                Username = username,
                Email = email,
                HashPassword = hashPassword,
                // Role mặc định là User khi đăng ký
            };

            // Lưu người dùng vào cơ sở dữ liệu
            await _userRepository.AddUserAsync(user);
            return user;
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
            // 1. Tạo JWT Access Token
            var jwtKey = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var accessTokenExpirationStr = _configuration["Jwt:AccessTokenExpireMinutes"];
            var refreshTokenExpirationStr = _configuration["Jwt:RefreshTokenExpireDays"];

            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT key is not configured.");

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
