using Microsoft.AspNetCore.Mvc;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Bảng điều khiển Admin";
            return View();
        }
    }
}
