using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ChatAPI
{
    public class ChatHub1 : Hub
    {
        public async Task SendPrivateMessage(string sender, string recipient, string message)
        {
            await Clients.User(recipient).SendAsync("ReceiveMessage", new { sender, recipient, content = message });
        }

        public async Task SendGroupMessage(string sender, string group, string message)
        {
            await Clients.Group(group).SendAsync("ReceiveMessage", new { sender, group, content = message });
        }
    }
}
