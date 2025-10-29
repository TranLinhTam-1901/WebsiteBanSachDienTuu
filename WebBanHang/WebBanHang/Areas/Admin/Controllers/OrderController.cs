using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }
      

       
        public async Task<IActionResult> Manage()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var validStatuses = new[] { "Pending", "Confirmed" };
            if (!validStatuses.Contains(status))
            {
                TempData["Error"] = "Trạng thái không hợp lệ. Chỉ có thể là 'Đang xử lý' hoặc 'Đã xác nhận'.";
                return RedirectToAction("Manage");
            }

            if (order.IsPaid)
            {
                TempData["Error"] = "Đơn hàng đã thanh toán, không thể thay đổi trạng thái.";
                return RedirectToAction("Manage");
            }

            if (order.Status == "Canceled")
            {
                TempData["Error"] = "Không thể cập nhật đơn hàng đã bị hủy.";
                return RedirectToAction("Manage");
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            // Bỏ thông báo admin
            return RedirectToAction("Manage");
        }

       
        [HttpPost]
        public async Task<IActionResult> Confirm(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status == "Canceled")
            {
                TempData["Error"] = "Không thể xác nhận đơn đã bị hủy.";
                return RedirectToAction("Manage");
            }

            if (order.IsPaid)
            {
                TempData["Error"] = "Đơn này đã được thanh toán, không thể xác nhận lại.";
                return RedirectToAction("Manage");
            }

            // Khi admin xác nhận -> đánh dấu đã thanh toán và chuyển trạng thái
            order.Status = "Confirmed";
            order.IsPaid = true;
            order.TransactionId = Guid.NewGuid().ToString("N").Substring(0, 12);
            
            // Tạo payment record
            _context.Payments.Add(new Payment
            {
                OrderId = order.Id,
                Amount = order.Total,
                Method = "Bank Transfer",
                Status = "Success",
                TransactionId = order.TransactionId
            });
            
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Đơn hàng đã được xác nhận thành công!";
            return RedirectToAction("Manage");
        }

        
        [HttpPost]
        public async Task<IActionResult> Complete(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (!order.IsPaid)
            {
                TempData["Error"] = "Đơn hàng chưa được thanh toán, không thể hoàn tất.";
                return RedirectToAction("Manage");
            }

            if (order.Status == "Completed")
            {
                TempData["Error"] = "Đơn này đã hoàn tất rồi.";
                return RedirectToAction("Manage");
            }

            order.Status = "Completed";
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Đơn hàng đã hoàn tất!";
            return RedirectToAction("Manage");
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status == "Canceled")
            {
                TempData["Error"] = "Đơn này đã bị hủy rồi.";
                return RedirectToAction("Manage");
            }

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();
            // Bỏ thông báo admin
            return RedirectToAction("Manage");
        }


      
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return Content("<p class='text-danger'>Không tìm thấy đơn hàng.</p>");

            return PartialView("DetailsPartial", order);
        }
    }
}
