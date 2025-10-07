
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int Rating { get; set; } // từ 1 → 5

        [MaxLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Khóa ngoại
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string? ImageUrl { get; set; } // thêm dòng này


    }
}
