using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Get all active conversations with unread message count
            var conversations = await _db.Conversations
                .Include(c => c.User)
                .Include(c => c.Messages)
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();

            var conversationList = conversations.Select(c => new
            {
                Id = c.Id,
                UserId = c.UserId,
                UserName = c.User?.UserName ?? "Unknown",
                UserFullName = c.User?.FullName,
                LastMessageAt = c.LastMessageAt ?? c.CreatedAt,
                UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderId != "Admin"),
                LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.Content ?? ""
            }).ToList();

            ViewBag.Conversations = conversationList;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            var messages = await _db.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var messageList = messages.Select(m => new
            {
                id = m.Id,
                senderId = m.SenderId,
                content = m.Content,
                imageUrl = m.ImageUrl,
                createdAt = m.CreatedAt,
                isRead = m.IsRead
            }).ToList();

            return Json(messageList);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int conversationId)
        {
            var messages = await _db.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsRead && m.SenderId != "Admin")
                .ToListAsync();

            foreach (var msg in messages)
            {
                msg.IsRead = true;
            }

            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
