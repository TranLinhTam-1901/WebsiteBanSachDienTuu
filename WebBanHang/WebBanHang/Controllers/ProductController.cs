using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebBanHang.Models;
using WebBanHang.Helpers;


namespace WebBanHang.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        public ProductController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _context = context;
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string sort = "newest", int? categoryId = null)
        {
            var query = _db.Products
                .Include(p => p.Category)
                .AsNoTracking();

            // Lọc theo thể loại nếu được chọn
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Sắp xếp theo tiêu chí
            switch (sort.ToLower())
            {
                case "newest":
                    query = query.OrderByDescending(p => p.Id);
                    break;
                case "price_low_high":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price_high_low":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                default:
                    query = query.OrderByDescending(p => p.Id);
                    break;
            }

            var products = await query.ToListAsync();
            
            // Lấy danh sách thể loại
            var categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
            
            // Truyền thông tin cho view
            ViewBag.CurrentSort = sort;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.Categories = categories;
            
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                 .Include(p => p.Reviews)            
                    .ThenInclude(r => r.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            // ✅ Tính trung bình rating
            double averageRating = 0;
            int reviewCount = 0;
            if (product.Reviews != null && product.Reviews.Any())
            {
                averageRating = product.Reviews.Average(r => r.Rating);
                reviewCount = product.Reviews.Count();
            }

            ViewBag.AvgRating = averageRating;
            ViewBag.ReviewCount = reviewCount;

            var user = await _userManager.GetUserAsync(User);
            bool alreadyOwned = false;
            bool inCart = false;

            if (user != null)
            {
                alreadyOwned = await _db.Orders
                    .AnyAsync(o => o.UserId == user.Id && o.IsPaid &&
                                   o.Items.Any(i => i.ProductId == id));

                inCart = await _db.CartItems
                    .AnyAsync(ci => ci.Cart.UserId == user.Id && ci.ProductId == id);
            }

            // ✅ Gửi thông tin sang view
            ViewBag.AlreadyOwned = alreadyOwned;
            ViewBag.InCart = inCart;

            // Load reviews for this product
            var reviews = await _db.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            
            ViewBag.Reviews = reviews;
            ViewBag.CurrentUserId = user?.Id; // Pass current user ID for checking ownership in view
            
            // Calculate average rating
            if (reviews.Any())
            {
                ViewBag.AverageRating = Math.Round(reviews.Average(r => r.Rating), 1);
                ViewBag.TotalRatings = reviews.Count;
            }
            else
            {
                ViewBag.AverageRating = 0;
                ViewBag.TotalRatings = 0;
            }

            // Lưu sản phẩm đã xem vào session (tối đa 20 sản phẩm)
            var viewedProducts = HttpContext.Session.GetString("ViewedProducts");
            var productIds = new List<int>();
            
            if (!string.IsNullOrEmpty(viewedProducts))
            {
                productIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(viewedProducts) ?? new List<int>();
            }

            // Thêm sản phẩm hiện tại vào đầu danh sách (không thêm nếu đã có)
            if (!productIds.Contains(id))
            {
                productIds.Insert(0, id);
                // Giới hạn tối đa 20 sản phẩm
                if (productIds.Count > 20)
                {
                    productIds = productIds.Take(20).ToList();
                }
            }
            else
            {
                // Đưa sản phẩm lên đầu nếu đã tồn tại
                productIds.Remove(id);
                productIds.Insert(0, id);
            }

            HttpContext.Session.SetString("ViewedProducts", System.Text.Json.JsonSerializer.Serialize(productIds));

            // Lấy các sản phẩm đã xem (trừ sản phẩm hiện tại)
            var viewedProductsList = productIds
                .Where(pId => pId != id)
                .Take(20) // Lấy tối đa 20 sản phẩm
                .ToList();

            ViewBag.ViewedProducts = await _db.Products
                .Include(p => p.Category)
                .Where(p => viewedProductsList.Contains(p.Id))
                .AsNoTracking()
                .ToListAsync();

            // Lấy sản phẩm cùng thể loại (trừ sản phẩm hiện tại)
            ViewBag.RelatedProducts = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .AsNoTracking()
                .Take(20)
                .ToListAsync();

            return View(product);
        }


        [HttpGet]
        public IActionResult Search(string keyword, decimal? minPrice, decimal? maxPrice, int? categoryId, string author, bool? inStock)
        {
            // ✅ Lấy danh mục để hiển thị dropdown
            ViewBag.Categories = _context.Categories.ToList();

            // ✅ Nếu không nhập gì
            if (string.IsNullOrWhiteSpace(keyword)
                && !minPrice.HasValue && !maxPrice.HasValue
                && !categoryId.HasValue && string.IsNullOrWhiteSpace(author)
                && !inStock.HasValue)
            {
                ViewBag.ErrorMessage = "Vui lòng nhập từ khóa hoặc chọn bộ lọc tìm kiếm.";
                return View("SearchResults", new List<Product>());
            }

            // ✅ Khởi tạo query
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // ✅ 1. Tìm kiếm theo từ khóa
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = TextHelper.NormalizeText(keyword);

                // ❌ Loại bỏ lọc SQL (vì EF không hiểu NormalizeText)
                // ✅ Lấy toàn bộ ra bộ nhớ trước
                var allProducts = query
                    .Include(p => p.Category)
                    .AsNoTracking()
                    .ToList();

                // ✅ Lọc trong RAM — NormalizeText hoạt động chuẩn
                query = allProducts
                    .Where(p =>
                        TextHelper.NormalizeText(p.Name).Contains(normalizedKeyword) ||
                        TextHelper.NormalizeText(p.Description).Contains(normalizedKeyword) ||
                        TextHelper.NormalizeText(p.Author).Contains(normalizedKeyword) ||
                        TextHelper.NormalizeText(p.Category?.Name ?? "")
                            .Contains(normalizedKeyword))
                    .AsQueryable();
            }



            // ✅ 2. Lọc giá
            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // ✅ 3. Lọc theo thể loại
            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            // ✅ 4. Lọc theo tác giả (không phân biệt hoa thường)
            if (!string.IsNullOrWhiteSpace(author))
                query = query.Where(p => EF.Functions.Like(p.Author.ToLower(), $"%{author.ToLower()}%"));

            // ✅ 5. Lọc tồn kho
            if (inStock.HasValue)
                query = query.Where(p => inStock.Value ? p.StockQuantity > 0 : p.StockQuantity <= 0);

            // ✅ 6. Trả kết quả
            var products = query.OrderBy(p => p.Name).ToList();
            ViewBag.Keyword = keyword?.Trim();

            // Thông báo tìm kiếm
            if (products.Any())
            {
                ViewBag.Message = $"🔍 Tìm thấy <strong>{products.Count}</strong> kết quả phù hợp.";
            }
            else
            {
                ViewBag.Message = $"❌ Không tìm thấy sản phẩm nào khớp với từ khóa \"{keyword}\".";
            }

            // Gửi kết quả về view
            return View("SearchResults", products);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string? comment)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            // Validate rating
            if (rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Vui lòng chọn đánh giá từ 1 đến 5 sao" });
            }

            // Check if user already reviewed this product
            var existingReview = await _db.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == user.Id);

            if (existingReview != null)
            {
                // Update existing review
                existingReview.Rating = rating;
                existingReview.Comment = comment;
                existingReview.CreatedAt = DateTime.Now;
            }
            else
            {
                // Create new review
                var review = new Review
                {
                    ProductId = productId,
                    UserId = user.Id,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                };
                _db.Reviews.Add(review);
            }

            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Đánh giá đã được gửi thành công" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == user.Id);
            
            if (review == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đánh giá hoặc bạn không có quyền xóa đánh giá này" });
            }

            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa đánh giá thành công" });
        }

        [HttpGet]
        public async Task<IActionResult> GetReview(int reviewId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            var review = await _db.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == user.Id);
            
            if (review == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đánh giá" });
            }

            return Json(new { success = true, rating = review.Rating, comment = review.Comment ?? "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReview(int reviewId, int rating, string? comment)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            // Validate rating
            if (rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Vui lòng chọn đánh giá từ 1 đến 5 sao" });
            }

            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == user.Id);
            
            if (review == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đánh giá hoặc bạn không có quyền chỉnh sửa đánh giá này" });
            }

            review.Rating = rating;
            review.Comment = comment;
            review.CreatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Đã cập nhật đánh giá thành công" });
        }

    }
}
