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

        public CustomerController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
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
