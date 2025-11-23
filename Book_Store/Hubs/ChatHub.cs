using Book_Store.Data;
using Book_Store.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Book_Store.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(int sessionId, string sender, string message)
        {
            await Clients.Group($"chat_{sessionId}").SendAsync("ReceiveMessage", sender, message);
        }

        public async Task JoinSession(int sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{sessionId}");
        }
    }
}
