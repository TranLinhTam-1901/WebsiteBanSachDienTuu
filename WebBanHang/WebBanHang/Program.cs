using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Repositories;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



//setup Indetity 
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                 .AddDefaultTokenProviders()
                 .AddDefaultUI()
                 .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Hết hạn sau 30 phút
    options.SlidingExpiration = true;
    options.Cookie.IsEssential = true;

    // Quan trọng: không set ExpireTimeSpan cố định -> nó thành session cookie
    options.Cookie.Expiration = null;
});

builder.Services.AddRazorPages();
// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Đặt sau app.UseRouting()

app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages(); // rất quan trọng để Identity chạy


app.MapAreaControllerRoute(
    name: "Admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
