using EnglishHub.Server.Data;
using EnglishHub.Server.Models;
using MongoDB.Driver;

namespace EnglishHub.Server.Repositories
{
    public class EmailCodeRepository
    {
        private IMongoCollection<OtpCode> _otpCodes;
        public EmailCodeRepository(MongoDbContext context)
        {
            _otpCodes = context.OtpCodes;
        }

        public async Task<OtpCode> CreateOTPCodeAsync(string userId, string code, int expirationMinutes = 5)
        {
            var otpCode = new OtpCode
            {
                UserId = userId,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            };
            await _otpCodes.InsertOneAsync(otpCode);
            return otpCode;
        }

        public async Task<bool> ValidateOTPCodeAsync(string userId, string otpCode)
        {
            var now = DateTime.UtcNow;
            var filter = Builders<OtpCode>.Filter.And(
                Builders<OtpCode>.Filter.Eq(otp => otp.UserId, userId),
                Builders<OtpCode>.Filter.Eq(otp => otp.Code, otpCode),
                Builders<OtpCode>.Filter.Gt(otp => otp.ExpiresAt, now)
            );
            var otp = await _otpCodes.Find(filter).FirstOrDefaultAsync();
            return otp != null;
        }

        public async Task DeleteOTPCodeAsync(string userId)
        {
            var filter = Builders<OtpCode>.Filter.Eq(otp => otp.UserId, userId);
            await _otpCodes.DeleteManyAsync(filter);
        }

    }
}
