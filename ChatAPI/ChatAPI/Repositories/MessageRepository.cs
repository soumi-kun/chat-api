using ChatAPI.DbContext;
using ChatAPI.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.Repositories
{
    public class MessageRepository
    {
        private readonly IMongoCollection<Message> _messages;

        public MessageRepository(MongoDbContext context)
        {
            _messages = context.Messages;
        }
        public async Task<List<Message>> GetMessagesAsync(string senderId, string receiverId)
        {
            return await _messages.Find(msg => msg.SenderId == senderId && msg.ReceiverId == receiverId).ToListAsync();
        }
        public async Task<IEnumerable<Message>> GetChatHistory(string senderId, string receiverId)
        {
            //retrieve chat history between sender and receiver
            return await _messages.Find(msg =>
            (msg.SenderId == senderId && msg.ReceiverId == receiverId) ||
            (msg.SenderId == receiverId && msg.ReceiverId == senderId))
            .SortByDescending(msg => msg.Timestamp)
            .Limit(50)
            .ToListAsync();
        }
        public async Task<IEnumerable<Message>> GetGroupChatHistory(string groupId)
        {
            //retrieve chat history of the group id
            return await _messages.Find(msg => msg.ReceiverId == groupId)
                                                            .SortByDescending(msg => msg.Timestamp)
                                                            .Limit(50)
                                                            .ToListAsync();
        }
        public async Task SaveMessage(Message message)
        {
            await _messages.InsertOneAsync(message);
        }
    }
}
