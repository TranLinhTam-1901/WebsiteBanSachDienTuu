using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;
using WebBanHang.Models;
namespace WebBanHang.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        
        public async Task<IActionResult> Index()
        {
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

           
            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

           
            return View(orders);
        }

        //Xem chi tiết đơn hàng
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Nếu không phải admin thì chỉ được xem đơn của chính mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && order.UserId != currentUser.Id)
                return Forbid();


            return View(order);
        }

        //public async Task<IActionResult> Download(int id)
        //{
        //    var userId = _userManager.GetUserId(User); 

        //    var order = await _context.Orders
        //        .Include(o => o.Items)
        //        .ThenInclude(i => i.Product)
        //        .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        //    if (order == null || !order.IsPaid)
        //        return Forbid();

        //    var product = order.Items.First().Product;
        //    if (product == null || string.IsNullOrEmpty(product.BookContentUrl))
        //        return Content("Không tìm thấy file sách để tải.");

        //    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.BookContentUrl.TrimStart('/'));
        //    if (!System.IO.File.Exists(filePath))
        //        return NotFound("File không tồn tại trên máy chủ.");

        //    var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        //    var fileName = Path.GetFileName(filePath);

        //    return File(fileBytes, "application/pdf", fileName);
        //}

    }


}
