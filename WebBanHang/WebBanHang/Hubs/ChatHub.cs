using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _db;
        
        public ChatHub(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task SendMessage(string userId, string message)
        {
            var connectionId = Context.ConnectionId;
            
            // Tìm hoặc tạo conversation
            var conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    UserId = userId,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _db.Conversations.Add(conversation);
                await _db.SaveChangesAsync();
            }

            // Tạo message
            var newMessage = new Message
            {
                ConversationId = conversation.Id,
                SenderId = userId,
                Content = message,
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            
            _db.Messages.Add(newMessage);
            conversation.LastMessageAt = DateTime.Now;
            await _db.SaveChangesAsync();

            // Gửi tin nhắn đến admin và user
            await Clients.Group("Admin").SendAsync("ReceiveMessage", userId, message, newMessage.Id, newMessage.CreatedAt, null);
            await Clients.User(userId).SendAsync("ReceiveMessage", userId, message, newMessage.Id, newMessage.CreatedAt, null);
        }

        public async Task SendImage(string userId, string imageUrl)
        {
            // Tìm hoặc tạo conversation
            var conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    UserId = userId,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _db.Conversations.Add(conversation);
                await _db.SaveChangesAsync();
            }

            // Tạo message với hình ảnh
            var newMessage = new Message
            {
                ConversationId = conversation.Id,
                SenderId = userId,
                Content = "",
                ImageUrl = imageUrl,
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            
            _db.Messages.Add(newMessage);
            conversation.LastMessageAt = DateTime.Now;
            await _db.SaveChangesAsync();

            // Gửi hình ảnh đến admin và user
            await Clients.Group("Admin").SendAsync("ReceiveMessage", userId, "", newMessage.Id, newMessage.CreatedAt, imageUrl);
            await Clients.User(userId).SendAsync("ReceiveMessage", userId, "", newMessage.Id, newMessage.CreatedAt, imageUrl);
        }

        public async Task SendAdminReply(string userId, string message, string adminId)
        {
            // Tìm conversation
            var conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    UserId = userId,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _db.Conversations.Add(conversation);
                await _db.SaveChangesAsync();
            }

            // Tạo message từ admin
            var newMessage = new Message
            {
                ConversationId = conversation.Id,
                SenderId = adminId,
                Content = message,
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            
            _db.Messages.Add(newMessage);
            conversation.LastMessageAt = DateTime.Now;
            await _db.SaveChangesAsync();

            // Gửi tin nhắn đến user
            await Clients.User(userId).SendAsync("ReceiveMessage", adminId, message, newMessage.Id, newMessage.CreatedAt, null);
            await Clients.Group("Admin").SendAsync("ReceiveMessage", adminId, message, newMessage.Id, newMessage.CreatedAt, null);
        }

        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admin");
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
