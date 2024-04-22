using ChatAPI.DbContext;
using ChatAPI.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.Repositories
{
    public class GroupRepository
    {
        private readonly IMongoCollection<Group> _groups;

        public GroupRepository(MongoDbContext conetxt)
        {
            _groups = conetxt.Groups;
        }

        public async Task<List<Group>> GetAllGroupssAsync()
        {
            return await _groups.Find(group => true).ToListAsync();
        }

        public async Task SaveGroup(Group group)
        {
            await _groups.InsertOneAsync(group);
        }

        public async Task AddUsersToGroup(string groupId, Group group)
        {
            await _groups.ReplaceOneAsync(g => g.Id == groupId, group);
        }

        public async Task<Group> SearchGroupFromId(string groupId)
        {
            return await _groups.Find(g => g.Id == groupId).FirstOrDefaultAsync();
        }

        public async Task<List<Group>> SearchGroupsFromName(string query)
        {
            //uses a case-insensitive regular expression for filter
            var filter = Builders<Group>.Filter.Regex(g => g.Name, new MongoDB.Bson.BsonRegularExpression(query, "i"));
            return await _groups.Find(filter).ToListAsync();
        }
    }
}
