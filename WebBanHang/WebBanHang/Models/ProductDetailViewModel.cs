using System.Collections.Generic;
namespace WebBanHang.Models
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public List<Review> Reviews { get; set; } = new();

        // Dành cho form thêm đánh giá
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }
    }
}
