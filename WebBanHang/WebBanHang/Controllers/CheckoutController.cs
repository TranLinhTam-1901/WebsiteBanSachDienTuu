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

        private string GenerateOrderCode()
        {
            var random = new Random();
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmm"); // Bỏ giây để ngắn hơn
            var randomSuffix = random.Next(100, 999); // 3 số thay vì 4
            var orderCode = $"ORD{timestamp}{randomSuffix}";
            
            // Đảm bảo OrderCode là duy nhất và không quá 20 ký tự
            while (_db.Orders.Any(o => o.OrderCode == orderCode))
            {
                randomSuffix = random.Next(100, 999);
                orderCode = $"ORD{timestamp}{randomSuffix}";
            }
            
            return orderCode;
        }

        // ✅ Mua ngay - đưa trực tiếp vào trang thanh toán
        [HttpPost]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index", "Product");
            }

            // Kiểm tra xem người dùng đã sở hữu sách này chưa
            bool alreadyOwned = await _db.Orders
                .AnyAsync(o => o.UserId == user.Id && o.IsPaid &&
                               o.Items.Any(i => i.ProductId == productId));
            
            if (alreadyOwned)
            {
                TempData["Error"] = "Bạn đã sở hữu sách này trong thư viện của mình.";
                return RedirectToAction("Index", "Library");
            }

            // Tạo một CartItem tạm thời để hiển thị trong checkout
            var tempCartItem = new CartItem
            {
                Id = -1, // ID tạm thời
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price,
                Product = product
            };

            var tempCart = new Cart
            {
                UserId = user.Id,
                Items = new List<CartItem> { tempCartItem }
            };

            ViewBag.Subtotal = product.Price * quantity;
            ViewBag.Total = product.Price * quantity;
            ViewBag.IsBuyNow = true; // Đánh dấu đây là mua ngay

            return View("Index", tempCart);
        }

        // Hiển thị trang thanh toán
        // ✅ Khi người dùng nhấn "Đặt hàng" từ giỏ hàng (chỉ chọn 1 vài sản phẩm)
        [HttpPost]
        public async Task<IActionResult> Index(List<int> selectedItems)
        {
            if (selectedItems == null || !selectedItems.Any())
            {
                TempData["ErrorMessage"] = " Bạn cần chọn sản phẩm để thanh toán";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);
            var cart = await _db.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy giỏ hàng.";
                return RedirectToAction("Index", "Cart");
            }

            var selectedCartItems = cart.Items
                .Where(i => selectedItems.Contains(i.Id))
                .ToList();

            if (!selectedCartItems.Any())
            {
                TempData["ErrorMessage"] = " Bạn cần chọn sản phẩm để thanh toán ";
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.Subtotal = selectedCartItems.Sum(i => i.UnitPrice * i.Quantity);
            ViewBag.Total = (decimal)ViewBag.Subtotal;

            var selectedCart = new Cart
            {
                UserId = user.Id,
                Items = selectedCartItems
            };

            return View(selectedCart);
        }


        // Xử lý khi người dùng đặt hàng
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string fullName, string phone, List<int> selectedItems, int? buyNowProductId = null)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ thông tin.");
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            List<CartItem> itemsToOrder = new List<CartItem>();

            // Xử lý trường hợp "Mua ngay"
            if (buyNowProductId.HasValue)
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == buyNowProductId.Value);
                if (product == null)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction("Index", "Product");
                }

                // Kiểm tra xem người dùng đã sở hữu sách này chưa
                bool alreadyOwned = await _db.Orders
                    .AnyAsync(o => o.UserId == user.Id && o.IsPaid &&
                                   o.Items.Any(i => i.ProductId == buyNowProductId.Value));
                
                if (alreadyOwned)
                {
                    TempData["Error"] = "Bạn đã sở hữu sách này trong thư viện của mình.";
                    return RedirectToAction("Index", "Library");
                }

                // Tạo CartItem tạm thời cho mua ngay
                itemsToOrder.Add(new CartItem
                {
                    ProductId = buyNowProductId.Value,
                    Quantity = 1,
                    UnitPrice = product.Price,
                    Product = product
                });
            }
            // Xử lý trường hợp từ giỏ hàng
            else
            {
                if (selectedItems == null || !selectedItems.Any())
                {
                    TempData["Error"] = "Bạn cần chọn sản phẩm để thanh toán.";
                    return RedirectToAction("Index", "Cart");
                }

                var cart = await _db.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (cart == null || !cart.Items.Any())
                    return RedirectToAction("Index", "Cart");

                // Chỉ lấy những sản phẩm đã chọn
                itemsToOrder = cart.Items
                    .Where(i => selectedItems.Contains(i.Id))
                    .ToList();

                if (!itemsToOrder.Any())
                {
                    TempData["Error"] = "Bạn cần chọn sản phẩm để thanh toán.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // ✅ Không tính phí ship nữa
            var subtotal = itemsToOrder.Sum(i => i.UnitPrice * i.Quantity);
            var total = subtotal;

            var order = new Order
            {
                UserId = user.Id,
                OrderCode = GenerateOrderCode(),
                FullName = fullName,                
                Phone = phone,
                Subtotal = subtotal,
                Status = "Pending",
                Total = total
            };

            // Thêm sản phẩm vào đơn hàng
            foreach (var ci in itemsToOrder)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice
                });
            }

            _db.Orders.Add(order);

            // Nếu là từ giỏ hàng thì xóa sản phẩm đã chọn khỏi giỏ hàng
            if (!buyNowProductId.HasValue)
            {
                var cart = await _db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);
                
                if (cart != null)
                {
                    var itemsToRemove = cart.Items.Where(i => selectedItems.Contains(i.Id)).ToList();
                    _db.CartItems.RemoveRange(itemsToRemove);
                }
            }

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
                // Bỏ thông báo admin chưa xác nhận
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
