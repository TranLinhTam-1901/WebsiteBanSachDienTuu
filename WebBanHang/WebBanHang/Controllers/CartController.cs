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

        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { count = 0 });
                }

                var cart = await _db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                var count = cart?.Items?.Sum(i => i.Quantity) ?? 0;
                return Json(new { count });
            }
            catch
            {
                return Json(new { count = 0 });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            try
            {
                if (quantity < 1) quantity = 1;

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { 
                        success = false,
                        message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
                    });
                }

                var cart = await GetOrCreateCart();
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
                if (product == null) 
                {
                    return Json(new { 
                        success = false,
                        message = "Không tìm thấy sản phẩm."
                    });
                }

                bool alreadyOwned = await _db.Orders
                    .AnyAsync(o => o.UserId == user.Id && o.IsPaid &&
                                   o.Items.Any(i => i.ProductId == productId));
                if (alreadyOwned)
                {
                    return Json(new { 
                        success = false,
                        message = "Bạn đã sở hữu sách này trong thư viện của mình."
                    });
                }

                // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
                var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                bool isNewItem = existingItem == null;
                
                // Thêm sản phẩm vào giỏ nếu chưa có
                if (existingItem == null)
                {
                    cart.Items.Add(new CartItem
                    {
                        ProductId = productId,
                        Quantity = 1,
                        UnitPrice = product.Price
                    });
                    await _db.SaveChangesAsync();
                }

                // Cập nhật lại cart để lấy count mới nhất
                var updatedCart = await _db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);
                    
                var cartCount = updatedCart?.Items?.Sum(i => i.Quantity) ?? 0;
                
                // Luôn trả về success=true để hiển thị notification đúng cách
                // Chỉ khác nhau ở message
                return Json(new { 
                    success = true,
                    isNewItem = isNewItem,
                    message = isNewItem 
                        ? $"Đã thêm '{product.Name}' vào giỏ hàng." 
                        : $"Sản phẩm '{product.Name}' đã có trong giỏ hàng.",
                    cartCount = cartCount,
                    productName = product.Name
                });
            }
            catch (Exception ex)
            {
                // Trả về JSON lỗi thay vì redirect
                return Json(new { 
                    success = false,
                    message = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng."
                });
            }
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
            
            // Cập nhật localStorage
            var cartCount = cart.Items.Count - 1;
            TempData["CartCount"] = cartCount;
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Clear()
        {
            var cart = await GetOrCreateCart();
            _db.CartItems.RemoveRange(cart.Items);
            await _db.SaveChangesAsync();
            
            // Clear localStorage
            TempData["CartCount"] = 0;
            
            return RedirectToAction(nameof(Index));
        }
    }
}
