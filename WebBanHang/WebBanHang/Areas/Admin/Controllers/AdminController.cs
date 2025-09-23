using Microsoft.AspNetCore.Mvc;

namespace WebBanHang.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Bảng điều khiển";
            return View();
        }
    }
}
