using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatApiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet("messages")]
        [Authorize]
        public async Task<IActionResult> GetMessages()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Tìm conversation của user
            var conversation = await _db.Conversations
                .Where(c => c.UserId == user.Id && c.IsActive)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .FirstOrDefaultAsync();

            if (conversation == null)
            {
                return Ok(new { messages = new List<object>() });
            }

            // Lấy messages
            var messages = await _db.Messages
                .Where(m => m.ConversationId == conversation.Id)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    id = m.Id,
                    senderId = m.SenderId,
                    content = m.Content,
                    imageUrl = m.ImageUrl,
                    createdAt = m.CreatedAt,
                    isRead = m.IsRead
                })
                .ToListAsync();

            return Ok(new { conversationId = conversation.Id, messages });
        }

        [HttpPost("send")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { success = false, message = "Tin nhắn không được để trống" });
            }

            // Tìm hoặc tạo conversation
            var conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.IsActive);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    UserId = user.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _db.Conversations.Add(conversation);
                await _db.SaveChangesAsync();
            }

            // Tạo message
            var message = new Message
            {
                ConversationId = conversation.Id,
                SenderId = user.Id,
                Content = request.Message,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _db.Messages.Add(message);
            conversation.LastMessageAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = message });
        }

        [HttpPost("upload-image")]
        [Authorize]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Không có file nào được chọn" });
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { success = false, message = "File quá lớn! Vui lòng chọn file nhỏ hơn 5MB." });
            }

            // Validate file type
            if (!file.ContentType.StartsWith("image/"))
            {
                return BadRequest(new { success = false, message = "Vui lòng chọn file hình ảnh!" });
            }

            try
            {
                // Create upload directory if not exists
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chat");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return URL
                var imageUrl = $"/uploads/chat/{fileName}";
                return Ok(new { success = true, imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi upload file: {ex.Message}" });
            }
        }
    }

    public class SendMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
