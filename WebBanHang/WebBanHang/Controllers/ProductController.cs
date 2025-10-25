using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
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
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

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


        private readonly ApplicationDbContext _context;

        [HttpGet]
        public IActionResult Search(string keyword, decimal? minPrice, decimal? maxPrice, int? categoryId, string author, bool? inStock)
        {
            // ⚠️ Nếu không nhập từ khóa hoặc chọn bộ lọc nào
            if (string.IsNullOrWhiteSpace(keyword)
                && !minPrice.HasValue && !maxPrice.HasValue
                && !categoryId.HasValue && string.IsNullOrWhiteSpace(author)
                && !inStock.HasValue)
            {
                ViewBag.ErrorMessage = " Vui lòng nhập từ khóa hoặc chọn bộ lọc tìm kiếm.";
                ViewBag.Categories = _context.Categories.ToList();
                return View("SearchResults", new List<Product>()); // ✅ KHÔNG redirect
            }

            // ✅ Nếu có dữ liệu, tiếp tục tìm kiếm
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p =>
                    p.Name.Contains(keyword) ||
                    p.Description.Contains(keyword) ||
                    p.Author.Contains(keyword));
                ViewBag.Keyword = keyword;
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
                ViewBag.MinPrice = minPrice;
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
                ViewBag.MaxPrice = maxPrice;
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
                ViewBag.CategoryId = categoryId;
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                query = query.Where(p => p.Author.Contains(author));
                ViewBag.Author = author;
            }

            if (inStock.HasValue)
            {
                if (inStock.Value)
                    query = query.Where(p => p.StockQuantity > 0);
                else
                    query = query.Where(p => p.StockQuantity <= 0);
                ViewBag.InStock = inStock;
            }

            var products = query.OrderBy(p => p.Name).ToList();
            ViewBag.Categories = _context.Categories.ToList();

            if (!products.Any())
                ViewBag.Message = "Không tìm thấy sản phẩm phù hợp.";
            else
                ViewBag.Message = $"Tìm thấy {products.Count} kết quả.";

            return View("SearchResults", products);
        }


    }
}
