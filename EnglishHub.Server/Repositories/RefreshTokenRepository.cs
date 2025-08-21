using EnglishHub.Server.Data;
using EnglishHub.Server.Models;
using MongoDB.Driver;

namespace EnglishHub.Server.Repositories
{
    public class RefreshTokenRepository
    {
        private IMongoCollection<RefreshToken> _refreshToken;

        public RefreshTokenRepository(MongoDbContext context)
        {
            _refreshToken = context.RefreshTokens;
        }

        public async Task<RefreshToken> AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _refreshToken.InsertOneAsync(refreshToken);
            return refreshToken;
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var updateDefinition = Builders<RefreshToken>.Update
                .Set(rt => rt.IsRevoked, true);
            await _refreshToken.UpdateOneAsync(
                rt => rt.Token == refreshToken && !rt.IsRevoked,
                updateDefinition);
        }

        public async Task<RefreshToken> GetValidRefreshTokenAsync(string refreshToken, string userId)
        {
            var now = DateTime.UtcNow;
            return await _refreshToken
                .Find(rt =>
                    rt.Token == refreshToken &&
                    rt.UserId == userId &&
                    !rt.IsRevoked &&
                    rt.ExpiresAt > now
                )
                .FirstOrDefaultAsync();
        }
    }
}
