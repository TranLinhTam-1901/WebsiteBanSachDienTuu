using Microsoft.AspNetCore.Mvc;
using WebBanHang.Repositories;

namespace WebBanHang.Controllers
{
    public class AdminProductController : Controller
    {
        private readonly IProductRepository _productRepo;

        public AdminProductController(IProductRepository productRepo)
        {
            _productRepo = productRepo;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Quản lý sản phẩm";
            var products = _productRepo.GetAllProducts();
            return View(products);
        }
    }
}
