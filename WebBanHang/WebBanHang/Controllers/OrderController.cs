using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;
using System.IO;
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

            // Cập nhật OrderCode cho các đơn hàng chưa có mã
            var ordersWithoutCode = await _context.Orders
                .Where(o => string.IsNullOrEmpty(o.OrderCode))
                .ToListAsync();

            foreach (var order in ordersWithoutCode)
            {
                order.OrderCode = GenerateOrderCode();
            }

            if (ordersWithoutCode.Any())
            {
                await _context.SaveChangesAsync();
            }

            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

           
            return View(orders);
        }

        private string GenerateOrderCode()
        {
            var random = new Random();
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmm"); // Bỏ giây để ngắn hơn
            var randomSuffix = random.Next(100, 999); // 3 số thay vì 4
            var orderCode = $"ORD{timestamp}{randomSuffix}";
            
            // Đảm bảo OrderCode là duy nhất và không quá 20 ký tự
            while (_context.Orders.Any(o => o.OrderCode == orderCode))
            {
                randomSuffix = random.Next(100, 999);
                orderCode = $"ORD{timestamp}{randomSuffix}";
            }
            
            return orderCode;
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

        public async Task<IActionResult> Download(int id)
        {
            var userId = _userManager.GetUserId(User); 

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null || !order.IsPaid)
                return Forbid();

            var product = order.Items.First().Product;
            if (product == null || string.IsNullOrEmpty(product.BookContentUrl))
                return Content("Không tìm thấy file sách để tải.");

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.BookContentUrl.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound("File không tồn tại trên máy chủ.");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);

            return File(fileBytes, "application/pdf", fileName);
        }

        // Xóa đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
                return NotFound();

            // Cho phép xóa tất cả đơn hàng (không cần kiểm tra trạng thái thanh toán)
            // Chỉ cảnh báo nếu đơn hàng đã thanh toán
            if (order.IsPaid)
            {
                // Vẫn cho phép xóa nhưng cảnh báo
                TempData["Warning"] = $"Đã xóa đơn hàng {order.OrderCode ?? $"#{order.Id}"} (đơn hàng đã thanh toán).";
            }
            else
            {
                TempData["Success"] = $"Đã xóa đơn hàng {order.OrderCode ?? $"#{order.Id}"} thành công.";
            }

            // Xóa các OrderItem trước
            _context.OrderItems.RemoveRange(order.Items);
            
            // Xóa Payment nếu có
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }

            // Xóa Order
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }


}
