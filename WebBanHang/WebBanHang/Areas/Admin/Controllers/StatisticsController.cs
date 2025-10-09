using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public StatisticsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(int? month, int? year)
        {
            var now = DateTime.Now;
            int m = month ?? now.Month;
            int y = year ?? now.Year;

            // Lấy đơn hàng trong tháng
            var ordersInMonth = await _db.Orders
                .Where(o => o.CreatedAt.Month == m && o.CreatedAt.Year == y)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .ToListAsync();

            // Tổng doanh thu tháng (tổng tất cả đơn)
            var totalRevenueMonth = ordersInMonth.Sum(o => o.Total);

            // Gom doanh thu theo ngày trong tháng
            var dailyRevenue = ordersInMonth
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Day = g.Key.ToString("dd/MM"),
                    Total = g.Sum(o => o.Total)
                })
                .OrderBy(x => x.Day)
                .ToList();

            // Top 5 sách bán chạy
            var topBooks = ordersInMonth
                .SelectMany(o => o.Items)
                .GroupBy(i => i.Product.Name)
                .Select(g => new
                {
                    BookName = g.Key,
                    Quantity = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.UnitPrice * i.Quantity),
                    Image = g.FirstOrDefault().Product.ImageUrl
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToList();

            ViewBag.Month = m;
            ViewBag.Year = y;
            ViewBag.TotalRevenueMonth = totalRevenueMonth;
            ViewBag.DailyRevenue = dailyRevenue;
            ViewBag.TopBooks = topBooks;

            return View();
        }
    }
}
