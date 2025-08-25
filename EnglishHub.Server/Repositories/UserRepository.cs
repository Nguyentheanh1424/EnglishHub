using EnglishHub.Server.Data;
using EnglishHub.Server.Models;
using MongoDB.Driver;

namespace EnglishHub.Server.Repositories
{
    public class UserRepository
    {
        private readonly MongoDbContext _context;
        public UserRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            return await _context.Users
                .Find(u => u.Id == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Find(u => u.Username == username)
                .FirstOrDefaultAsync();
        }

        public async Task AddUserAsync(User user)
        {
            await _context.Users.InsertOneAsync(user);
        }

        public async Task UpdateUserAsync(User user)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
            await _context.Users.ReplaceOneAsync(filter, user);
        }

        public async Task<string> GetUserEmailByIdAsync(string userId)
        {
            var user = await _context.Users
                .Find(u => u.Id == userId)
                .FirstOrDefaultAsync();
            return user?.Email;
        }

        public async Task<string> GetUserNameByIdAsync(string userId)
        {
            var user = await _context.Users
                .Find(u => u.Id == userId)
                .FirstOrDefaultAsync();
            return user?.Username;
        }
    }
}