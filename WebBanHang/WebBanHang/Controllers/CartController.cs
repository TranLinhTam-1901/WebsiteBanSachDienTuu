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
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        private async Task<Cart> GetOrCreateCart()
        {
            var user = await _userManager.GetUserAsync(User);
            var cart = await _db.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null)
            {
                cart = new Cart { UserId = user.Id };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<IActionResult> Index()
        {
            var cart = await GetOrCreateCart();
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            if (quantity < 1) quantity = 1;

            var user = await _userManager.GetUserAsync(User);
            var cart = await GetOrCreateCart();
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return NotFound();

            bool alreadyOwned = await _db.Orders
                .AnyAsync(o => o.UserId == user.Id && o.IsPaid &&
                               o.Items.Any(i => i.ProductId == productId));
            if (alreadyOwned)
            {
                TempData["Error"] = "📚 Bạn đã sở hữu sách này trong thư viện của mình.";
                return RedirectToAction("Index", "Library");
            }

            // 🔍 Kiểm tra sách đã có trong giỏ hàng chưa
            var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (existing != null)
            {
                // Không cộng dồn nữa, giữ nguyên
                TempData["Info"] = "⚠️ Sách này đã có trong giỏ hàng.";
            }
            else
            {
                // ✅ Chỉ thêm 1 bản duy nhất
                cart.Items.Add(new CartItem
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = product.Price
                });
                TempData["Success"] = $"✅ Đã thêm '{product.Name}' vào giỏ hàng.";
            }


            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> UpdateQuantityAjax(int itemId, int quantity)
        {
            var cart = await GetOrCreateCart();
            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return NotFound();
            if (quantity <= 0)
            {
                _db.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = 1; // ép về 1
            }

            await _db.SaveChangesAsync();

            var subtotal = cart.Items.Sum(ci => ci.UnitPrice * ci.Quantity);
            var total = subtotal;
            var lineSubtotal = item.UnitPrice;

            return Json(new { ok = true, itemId, quantity = 1, lineSubtotal, subtotal, total });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Remove(int itemId)
        {
            var cart = await GetOrCreateCart();
            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return NotFound();

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Clear()
        {
            var cart = await GetOrCreateCart();
            _db.CartItems.RemoveRange(cart.Items);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
