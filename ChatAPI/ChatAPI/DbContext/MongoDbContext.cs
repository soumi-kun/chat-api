using ChatAPI.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI.DbContext
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration config)
        {
            var connectionString = config.GetConnectionString("MongoDB");
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase("ChatAppDB");
        }

        public IMongoCollection<Message> Messages => _database.GetCollection<Message>("Messages");
        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoCollection<Group> Groups => _database.GetCollection<Group>("Groups");
        public IMongoCollection<Connection> Connections => _database.GetCollection<Connection>("Connections");


    }
}
