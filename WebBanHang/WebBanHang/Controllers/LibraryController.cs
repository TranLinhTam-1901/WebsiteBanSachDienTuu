using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class LibraryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public LibraryController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var purchasedBooks = await _db.Orders
                .Where(o => o.UserId == user.Id && o.IsPaid)
                .SelectMany(o => o.Items)
                .Include(i => i.Product)
                .Select(i => i.Product)
                .Distinct()
                .ToListAsync();

            return View(purchasedBooks);
        }

        // 📖 Trang đọc sách
        public async Task<IActionResult> Read(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Kiểm tra user có quyền đọc sách này không (phải từ đơn đã thanh toán)
            var hasAccess = await _db.Orders
                .AnyAsync(o => o.UserId == user.Id && o.IsPaid && 
                               o.Items.Any(i => i.ProductId == id));
            
            if (!hasAccess)
            {
                TempData["Error"] = "Bạn chưa sở hữu sách này hoặc đơn hàng chưa được xác nhận.";
                return RedirectToAction("Index", "Home");
            }

            var progress = await _db.ReadingProgresses
                .FirstOrDefaultAsync(r => r.ProductId == id && r.UserId == user.Id);

            if (progress == null)
            {
                progress = new ReadingProgress
                {
                    ProductId = id,
                    UserId = user.Id,
                    CurrentPage = 1,
                    ProgressPercent = 0
                };
                _db.ReadingProgresses.Add(progress);
                await _db.SaveChangesAsync();
            }

            ViewBag.ProgressPercent = progress?.ProgressPercent ?? 0;

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> SaveProgress([FromBody] ProgressUpdateDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            var progress = await _db.ReadingProgresses
                .FirstOrDefaultAsync(r => r.ProductId == dto.ProductId && r.UserId == user.Id);

            if (progress != null)
            {
                progress.ProgressPercent = dto.Percent;
                progress.LastReadAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            return Ok();
        }

        public class ProgressUpdateDto
        {
            public int ProductId { get; set; }
            public double Percent { get; set; }
        }

    }
}
