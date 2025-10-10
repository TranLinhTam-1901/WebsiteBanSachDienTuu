using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public class ReadingProgress
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        // Vị trí đọc hiện tại (nếu là PDF, có thể lưu trang số)
        public int CurrentPage { get; set; } = 1;

        // Nếu là EPUB hoặc HTML text, bạn có thể lưu phần trăm (0-100)
        public double ProgressPercent { get; set; } = 0;

        public DateTime LastReadAt { get; set; } = DateTime.Now;
    }
}
