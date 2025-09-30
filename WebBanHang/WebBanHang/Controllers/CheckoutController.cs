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
            ViewBag.Shipping = 15000m;
            ViewBag.Total = (decimal)ViewBag.Subtotal + (decimal)ViewBag.Shipping;
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string fullName, string address, string phone)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(phone))
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
            var shipping = 15000m;
            var total = subtotal + shipping;

            var order = new Order
            {
                UserId = user.Id,
                FullName = fullName,
                Address = address,
                Phone = phone,
                Subtotal = subtotal,
                ShippingFee = shipping,
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

            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}


