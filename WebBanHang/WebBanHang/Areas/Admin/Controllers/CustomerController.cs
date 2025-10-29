using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CustomerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public CustomerController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy toàn bộ user trừ Admin
            var users = await _userManager.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var list = new List<CustomerAccountViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                list.Add(new CustomerAccountViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    CreatedAt = user.CreatedAt, // nếu bạn có cột CreatedAt
                    Roles = string.Join(", ", roles)
                });
            }

            return View(list);
        }

        // Tuỳ chọn: Xoá tài khoản
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy user!";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra nếu user là Admin hiện tại đang đăng nhập
            var currentUser = await _userManager.GetUserAsync(User);
            if (user.Id == currentUser?.Id)
            {
                TempData["Error"] = "Không thể xóa tài khoản của chính mình!";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra nếu user là Admin khác
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(SD.Role_Admin))
            {
                TempData["Error"] = "Không thể xóa tài khoản Admin!";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Xóa Reviews của user
                var reviews = _context.Reviews.Where(r => r.UserId == id);
                _context.Reviews.RemoveRange(reviews);

                // Xóa ReadingProgress của user
                var readingProgresses = _context.ReadingProgresses.Where(r => r.UserId == id);
                _context.ReadingProgresses.RemoveRange(readingProgresses);

                // Xóa Conversations và Messages liên quan
                var conversations = _context.Conversations
                    .Include(c => c.Messages)
                    .Where(c => c.UserId == id)
                    .ToList();

                foreach (var conversation in conversations)
                {
                    _context.Messages.RemoveRange(conversation.Messages);
                    _context.Conversations.Remove(conversation);
                }

                // Lưu thay đổi về conversations, reviews và reading progress
                await _context.SaveChangesAsync();

                // Xóa user (Orders và Cart sẽ được xóa tự động do Cascade Delete)
                var result = await _userManager.DeleteAsync(user);
                
                if (result.Succeeded)
                {
                    TempData["Success"] = "Đã xóa tài khoản thành công!";
                }
                else
                {
                    TempData["Error"] = "Không thể xóa tài khoản. Lỗi: " + string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xóa tài khoản: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ToggleRole(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            // Nếu user hiện đang là Admin -> đổi về User
            if (roles.Contains(SD.Role_Admin))
            {
                await _userManager.RemoveFromRoleAsync(user, SD.Role_Admin);
                await _userManager.AddToRoleAsync(user, SD.Role_User);
            }
            else
            {
                // Nếu user hiện là User -> đổi thành Admin
                await _userManager.RemoveFromRoleAsync(user, SD.Role_User);
                await _userManager.AddToRoleAsync(user, SD.Role_Admin);
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }
    }

    public class CustomerAccountViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string Roles { get; set; }
    }
}
