using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
namespace WebBanHang.Data
{
    public class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Đảm bảo DB đã được tạo
            // await db.Database.MigrateAsync();  // Temporarily disabled - migration handled manually
            try
            {
                await db.Database.MigrateAsync();
            }
            catch (System.InvalidOperationException)
            {
                // Ignore pending model changes warning
            }

            // Tạo Role Admin nếu chưa có
            const string adminRole = "Admin";
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRole));
            }

           
            const string adminEmail = "admin@3tbooks.com";
            const string adminPassword = "Admin@123"; 

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Administrator"
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, adminRole);
                    Console.WriteLine("✅ Đã tạo tài khoản Admin mặc định.");
                }
                else
                {
                    Console.WriteLine("⚠️ Không thể tạo Admin:");
                    foreach (var error in result.Errors)
                        Console.WriteLine($" - {error.Description}");
                }
            }
            else
            {
                // Gán role nếu user tồn tại nhưng chưa có quyền
                if (!await userManager.IsInRoleAsync(existingAdmin, adminRole))
                {
                    await userManager.AddToRoleAsync(existingAdmin, adminRole);
                    Console.WriteLine("✅ Đã gán role Admin cho tài khoản hiện có.");
                }
            }
        }    
    }
}
