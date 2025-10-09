using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _db.Products.AsNoTracking().ToListAsync();
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            var reviews = await _db.Reviews
                .Include(r => r.User) // nếu có liên kết với ApplicationUser
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                Reviews = reviews
            };

            return View(viewModel);
        }



        [HttpPost]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment, IFormFile? imageFile)
        {
            // 🔹 Nếu người dùng chưa đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                TempData["LoginRequired"] = "Vui lòng đăng nhập để gửi đánh giá!";
                return RedirectToAction("Details", new { id = productId });
            }


            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            string? imagePath = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/reviews", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                imagePath = "/uploads/reviews/" + fileName;
            }

            var review = new Review
            {
                ProductId = productId,
                Rating = rating,      // ⭐ quan trọng — nhận giá trị từ form
                Comment = comment,
                ImageUrl = imagePath,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            return RedirectToAction("Details", new { id = productId });


            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/reviews");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                review.ImageUrl = "/uploads/reviews/" + fileName; // ✅ Đường dẫn tương đối để hiển thị
            }
        }

            [HttpGet]
            public async Task<IActionResult> Search(string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    // Nếu người dùng không nhập gì thì trả về toàn bộ sản phẩm
                    var allProducts = await _db.Products.AsNoTracking().ToListAsync();
                    ViewBag.Keyword = "";
                    return View("SearchResults", allProducts);
                }

                var results = await _db.Products
                    .AsNoTracking()
                    .Where(p =>
                        p.Name.Contains(keyword) ||
                        p.Description.Contains(keyword))
                    .ToListAsync();

                ViewBag.Keyword = keyword;
                return View("SearchResults", results);
            }

        }
    }
