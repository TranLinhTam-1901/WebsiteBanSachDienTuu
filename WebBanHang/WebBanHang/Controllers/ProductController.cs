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

        public async Task<IActionResult> Index()
        {
            var products = await _db.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .ToListAsync();

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

        }

    }
}
