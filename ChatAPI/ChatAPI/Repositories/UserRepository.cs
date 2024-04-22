using ChatAPI.DbContext;
using ChatAPI.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.Repositories
{
    public class UserRepository
    {
        private readonly IMongoCollection<User> _users;

        public UserRepository(MongoDbContext context)
        {
            _users = context.Users;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _users.Find(user => true).ToListAsync();
        }

        public async Task SaveUser(User user)
        {
            await _users.InsertOneAsync(user);
        }

        public async Task<List<User>> SearchUsersFromName(string query)
        {
            //uses a case-insensitive regular expression for filter
            var filter = Builders<User>.Filter.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression(query, "i"));
            return await _users.Find(filter).ToListAsync();
        }

        public async Task<User> SearchUserFromName(string username)
        {
            return await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        }

        public string SearchUserNameFromId(string userId)
        {
            var user = _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            return user.Result.Name;
        }

        public async Task<User> AuthUser(User user)
        {
            return await _users.Find(u => u.Username == user.Username && u.Password == user.Password).FirstOrDefaultAsync();
        }

        public async Task<User> ReauthUser(string userId)
        {
            return await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        }
    }
}
