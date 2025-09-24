using Microsoft.AspNetCore.Mvc;
using WebBanHang.Repositories;
using WebBanHang.Models;

public class ProductController : Controller
{
    private readonly IProductRepository _productRepo;

    public ProductController(IProductRepository productRepo)
    {
        _productRepo = productRepo;
    }

    public IActionResult Index()
    {
        var products = _productRepo.GetAllProducts();
        return View(products);
    }

    public IActionResult Details(int id)
    {
        var product = _productRepo.GetProductById(id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }



}
