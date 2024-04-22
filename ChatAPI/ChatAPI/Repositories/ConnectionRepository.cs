using ChatAPI.DbContext;
using ChatAPI.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.Repositories
{
    public class ConnectionRepository
    {
        private readonly IMongoCollection<Connection> _connections;

        public ConnectionRepository(MongoDbContext context)
        {
            _connections = context.Connections;
        }

        public async Task<List<Connection>> GetAllUserConnectionsAsync()
        {
            return await _connections.Find(user => true).ToListAsync();
        }
        public async Task<List<Connection>> GetAllOtherOnlineUserConnectionsAsync(string currentUserId)
        {
            return await _connections.Find(c => c.UserId != currentUserId).ToListAsync();
        }
        public async Task SaveConnection(Connection connection)
        {
            await _connections.InsertOneAsync(connection);
        }

        public Task<Connection> SearchConnection(string signalrId)
        {
            return _connections.Find(u => u.SignalrId == signalrId).FirstOrDefaultAsync();
        }

        public Task DeleteConnection(string connectionId)
        {
            return _connections.DeleteOneAsync(u => u.Id == connectionId);
        }

    }
}
