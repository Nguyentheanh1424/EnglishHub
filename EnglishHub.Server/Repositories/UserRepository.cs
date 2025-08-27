using EnglishHub.Server.Data;
using EnglishHub.Server.Models;
using MongoDB.Driver;

namespace EnglishHub.Server.Repositories
{
    public class UserRepository
    {
        private IMongoCollection<User> _user;
        public UserRepository(MongoDbContext context)
        {
            _user = context.Users;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _user
                .Find(u => u.Email == email)
                .FirstOrDefaultAsync();
        }
        public async Task<User> GetUserByIdAsync(string userId)
        {
            return await _user
                .Find(u => u.Id == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _user
                .Find(u => u.Username == username)
                .FirstOrDefaultAsync();
        }

        public async Task AddUserAsync(User user)
        {
            await _user.InsertOneAsync(user);
        }

        public async Task UpdateUserAsync(User user)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
            await _user.ReplaceOneAsync(filter, user);
        }

        public async Task<string?> GetUserEmailByIdAsync(string userId)
        {
            return await _user
                .Find(u => u.Id == userId)
                .Project(u => u.Email)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetUserNameByIdAsync(string userId)
        {
            return await _user
                .Find(u => u.Id == userId)
                .Project(u => u.Username)
                .FirstOrDefaultAsync();
        }
    }
}