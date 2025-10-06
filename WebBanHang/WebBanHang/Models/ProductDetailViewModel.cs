using System.Collections.Generic;
using WebBanHang.Models;

namespace YourProjectName.Models
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }      // Thông tin sản phẩm
        public List<Review> Reviews { get; set; } // Danh sách bình luận
        public Review NewReview { get; set; }     // Review mới để nhập từ form
    }
}
