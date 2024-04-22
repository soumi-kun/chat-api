using ChatAPI.Models;
using ChatAPI.Repositories;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAPI
{
    public partial class ChatHub : Hub
    {
        //private readonly IMongoCollection<Message> _messages;
        //private readonly IMongoCollection<User> _users;
        //private readonly IMongoCollection<Group> _groups;
        private readonly MessageRepository _messageRepository;
        private readonly UserRepository _userRepository;
        private readonly GroupRepository _groupRepository;
        private readonly ConnectionRepository _connectionRepository;




        //public ChatHub(IMongoDatabase database)
        //{
        //    _messages = database.GetCollection<Message>("Messages");
        //    _users = database.GetCollection<User>("Users");
        //    _groups = database.GetCollection<Group>("Groups");

        //}

        //override the base hub ondisconnect
        public override Task OnDisconnectedAsync(Exception exception)
        {
            var currentUserConnection = _connectionRepository.SearchConnection(Context.ConnectionId);
            if(currentUserConnection.Result != null)
            {
                //delete the logged user from connection when disconnecting
                _connectionRepository.DeleteConnection(currentUserConnection.Result.Id);
                Clients.Others.SendAsync("userOff", currentUserConnection.Result.UserId);
            }
            return base.OnDisconnectedAsync(exception);
        }
        public void Logout(string userId)
        {
            _connectionRepository.DeleteConnection(userId);
            Clients.Caller.SendAsync("logoutResponse");
            Clients.Others.SendAsync("userOff", userId);
        }

        public async Task SendMsg(string signalrId, string msg)
        {
            var receiverConnection = _connectionRepository.SearchConnection(signalrId);
            var senderConnection = _connectionRepository.SearchConnection(Context.ConnectionId);
            Message message = new Message
            {
                SenderId =  senderConnection.Result.UserId,
                ReceiverId = receiverConnection.Result.UserId,
                Content = msg
            };
            //saving message to db
            await _messageRepository.SaveMessage(message);

            await Clients.Client(signalrId).SendAsync("sendMsgResponse", Context.ConnectionId, msg);
        }

        public async Task GetOnlineUsers()
        {
            var currentUserConnection = _connectionRepository.SearchConnection(Context.ConnectionId);
            List<Connection> onlineConnections = await _connectionRepository.GetAllOtherOnlineUserConnectionsAsync(currentUserConnection.Result.UserId);
            var onlineUsers = onlineConnections.Select(c => new User
            {
                Id = c.UserId,
                Name = _userRepository.SearchUserNameFromId(c.UserId),
                SignalrId = c.SignalrId
            });
            await Clients.Caller.SendAsync("getOnlineUserResponse", onlineUsers);
        }

        public async Task AuthMe(User userInfo)
        {
            string currentSignalrId = Context.ConnectionId;
            var tempUser = await _userRepository.AuthUser(userInfo);

            //when user credentials are correct
            if(tempUser != null)
            {
                Connection currUser = new Connection
                {
                    UserId = tempUser.Id,
                    SignalrId = currentSignalrId,
                    Timestamp = DateTime.UtcNow
                };
                await _connectionRepository.SaveConnection(currUser);

                User newUser = new User
                {
                    Id = tempUser.Id,
                    Name = tempUser.Name,
                    Username = tempUser.Username,
                    SignalrId = currentSignalrId
                };
                //sends auth response to the requester
                await Clients.Caller.SendAsync("authMeResponseSuccess", newUser);
                //sends newly logged user data to all other users
                await Clients.Others.SendAsync("userOn", newUser);
            }
            //if the credentials are incorrect
            else
            {
                await Clients.Caller.SendAsync("authMeResponseFail");
            }
        }

        public async Task ReauthMe(string userId)
        {
            string currentSignalrId = Context.ConnectionId;
            var tempUser = await _userRepository.ReauthUser(userId);

            if(tempUser != null)
            {
                Connection currUser = new Connection
                {
                    UserId = tempUser.Id,
                    SignalrId = currentSignalrId,
                    Timestamp = DateTime.UtcNow
                };
                await _connectionRepository.SaveConnection(currUser);

                User newUser = new User
                {
                    Id = tempUser.Id,
                    Name = tempUser.Name,
                    Username = tempUser.Username,
                    SignalrId = currentSignalrId
                };
                //sends auth response to the requester
                await Clients.Caller.SendAsync("reauthMeResponse", newUser);
                //sends newly logged user data to all other users
                await Clients.Others.SendAsync("userOn", newUser);
            }

        }
        public ChatHub(MessageRepository messageRepository, UserRepository userRepository, GroupRepository groupRepository, ConnectionRepository connectionRepository)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _groupRepository = groupRepository;
            _connectionRepository = connectionRepository;
        }

        public async Task askServer(string clientTest)
        {
            string temp;
            if(clientTest == "hey")
            {
                temp = "hello";
            }
            else
            {
                temp = "Something else";
            }

            await Clients.Client(this.Context.ConnectionId).SendAsync("askServerResponse", temp);
        }
        //old
        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            //check if receiverId is a group id
            if (receiverId.StartsWith("group-"))
            {
                //var group = await _groups.Find(g => g.Id == receiverId).FirstOrDefaultAsync();
                var group = await _groupRepository.SearchGroupFromId(receiverId);

                if (group != null)
                {
                    var msg = new Message
                    {
                        SenderId = senderId,
                        ReceiverId = receiverId,
                        Content = message, 
                        Timestamp = DateTime.UtcNow
                    };

                    //save group message to MongoDb
                    //await _messages.InsertOneAsync(msg);
                    await _messageRepository.SaveMessage(msg);

                    //send message to all members of the group
                    await Clients.Group(receiverId).SendAsync("ReceiveMessage", new { senderId, message });

                }
            }
            //send message to private chat
            else
            {
                var msg = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = message,
                    Timestamp = DateTime.UtcNow
                };

                //save message to MongoDb
                //await _messages.InsertOneAsync(msg);
                await _messageRepository.SaveMessage(msg);

                //send message to receiver
                await Clients.User(receiverId).SendAsync("ReceiveMessage", new { senderId, message });
            }
        }

        //get private chat history
        public async Task GetChatHistory(string senderId, string receiverId)
        {
            //retrieve chat history between sender and receiver
            //var chatHistory = await _messages.Find(msg =>
            //(msg.SenderId == senderId && msg.ReceiverId == receiverId) ||
            //(msg.SenderId == receiverId && msg.ReceiverId == senderId))
            //.SortByDescending(msg => msg.Timestamp)
            //.Limit(50)
            //.ToListAsync();
            var chatHistory = await _messageRepository.GetChatHistory(senderId, receiverId);

            //send chat history to the user
            await Clients.Caller.SendAsync("ReceiveChatHistory", chatHistory.Select(msg => new { senderId = msg.SenderId, message = msg.Content }));
        }

        //creates a new group chat with the given name and adds the spefied users to the group
        public async Task CreateGroup(string groupName, List<string> userIds)
        {
            var group = new Group
            {
                Id = "group-" + Guid.NewGuid().ToString(),
                Name = groupName,
                Members = userIds
            };

            // save group to mongo db
            //await _groups.InsertOneAsync(group);
            await _groupRepository.SaveGroup(group);

            //Add users to the group
            foreach(var userId in userIds)
            {
                //await Groups.AddToGroupAsync(userId, $"group-{groupId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, group.Id);
            }
        }

        //Adds users to an existing group
        public async Task AddUsersToGroup(string groupId, List<string> userIds)
        {
            //Add users to existing group
            //var group = await _groups.Find(g => g.Id == groupId).FirstOrDefaultAsync();
            var group = await _groupRepository.SearchGroupFromId(groupId);
            if (group != null)
            {
                group.Members.AddRange(userIds);
                //await _groups.ReplaceOneAsync(g => g.Id == groupId, group);
                await _groupRepository.AddUsersToGroup(groupId, group);

                //Add users to the group
                foreach (var userId in userIds)
                {
                    await Groups.AddToGroupAsync(userId, groupId);
                }
            }
        }

        //gets chat history of a group from db
        public async Task GetGroupChatHistory(string groupId)
        {
            //var chatHistory = await _messages.Find(msg => msg.ReceiverId == groupId)
            //                                                .SortByDescending(msg => msg.Timestamp)
            //                                                .Limit(50)
            //                                                .ToListAsync();
            var chatHistory = await _messageRepository.GetGroupChatHistory(groupId);
            //send chat history to user
            await Clients.Caller.SendAsync("ReceiveGroupChatHistory", chatHistory.Select(msg => new {senderId = msg.SenderId, message = msg.Content}));
        }

        //creates a new user to the db
        public async Task CreateUser(string username)
        {
            //Check if the user name already exists in the database
            //var existingUser = await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
            var existingUser = await _userRepository.SearchUserFromName(username);
            if (existingUser == null)
            {
                var newUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = username
                };
                //await _users.InsertOneAsync(newUser);
                await _userRepository.SaveUser(newUser);
            }
        }

        //search Users in db
        public async Task<List<User>> SearchUsers(string query)
        {
            ////uses a case-insensitive regular expression for filter
            //var filter = Builders<User>.Filter.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression(query, "i"));
            //var users = await _users.Find(filter).ToListAsync();
            var users = await _userRepository.SearchUsersFromName(query);
            return users;
        }

        //search groups in db
        public async Task<List<Group>> SearchGroups(string query)
        {
            ////uses a case-insensitive regular expression for filter
            //var filter = Builders<Group>.Filter.Regex(g => g.Name, new MongoDB.Bson.BsonRegularExpression(query, "i"));
            //var groups = await _groups.Find(filter).ToListAsync();
            var groups = await _groupRepository.SearchGroupsFromName(query);
            return groups;

        }

        //public async Task SendGroupMessage(string sender, string group, string message)
        //{
        //    await Clients.Group(group).SendAsync("ReceiveMessage", new { sender, group, content = message });
        //}
    }
}
