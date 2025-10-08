using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var cart = await _db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }
            ViewBag.Subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
            ViewBag.Total = (decimal)ViewBag.Subtotal;
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string fullName, string phone)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ thông tin.");
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            var cart = await _db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
            var total = subtotal ;

            var order = new Order
            {
                UserId = user.Id,
                FullName = fullName,                
                Phone = phone,
                Subtotal = subtotal,
                Status = "Pending",
                Total = total
            };

            foreach (var ci in cart.Items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice
                });
            }

            _db.Orders.Add(order);
            _db.CartItems.RemoveRange(cart.Items);
            await _db.SaveChangesAsync();

            return RedirectToAction("FakePay", "Checkout", new { orderId = order.Id });

        }


        public async Task<IActionResult> FakePay(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            if (order.Status != "Confirmed")
            {
                TempData["Error"] = "Đơn hàng của bạn chưa được admin xác nhận.";
                return RedirectToAction("Index", "Order");
            }
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmFakePay(int orderId)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (order.Status != "Confirmed")
            {
                TempData["Error"] = "Đơn hàng chưa được xác nhận, không thể thanh toán.";
                return RedirectToAction("Index", "Order");
            }


            order.IsPaid = true;
            order.Status = "Confirmed";
            order.TransactionId = Guid.NewGuid().ToString("N").Substring(0, 12);
          
            _db.Payments.Add(new Payment
            {
                OrderId = order.Id,
                Amount = order.Total,
                Method = "Fake",
                Status = "Success",
                TransactionId = order.TransactionId
            });

            await _db.SaveChangesAsync();          
            return RedirectToAction("Success", new { orderId });
        }

        public async Task<IActionResult> Success(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return View(order);
        }
       
    }
}


