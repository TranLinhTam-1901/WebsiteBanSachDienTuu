using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastMessageAt { get; set; }

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    public class Message
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; }

        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
