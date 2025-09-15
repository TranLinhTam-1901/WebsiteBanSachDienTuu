using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebBanHang.Models;
using WebBanHang.Repositories;

namespace WebBanHang.Areas.Admin.Controllers
{
   
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository; 
        private readonly ICategoryRepository _categoryRepository;
        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }       
        
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }
        //// Xử lý thêm sản phẩm mới
        //[HttpPost]
        //public async Task<IActionResult> Add(Product product, IFormFile imageFile)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        if (imageFile != null)
        //        {
        //            product.ImageUrl = await SaveImage(imageFile);
        //        }

        //        await _productRepository.AddAsync(product);
        //        return RedirectToAction(nameof(Index));
        //    }

        //    var categories = await _categoryRepository.GetAllAsync();
        //    ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
        //    return View(product);
        //}

        //// Xử lý thêm sản phẩm mới
        //[HttpPost]
        //public async Task<IActionResult> Add(Product product, IFormFile imageFile)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        if (imageFile != null)
        //        {
        //            product.ImageUrl = await SaveImage(imageFile);
        //        }

        //        await _productRepository.AddAsync(product);
        //        return RedirectToAction(nameof(Index));
        //    }

        //    var categories = await _categoryRepository.GetAllAsync();
        //    ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
        //    return View(product);
        //}

        //// Lưu ảnh và trả về đường dẫn
        //private async Task<string> SaveImage(IFormFile image)
        //{
        //    if (!Directory.Exists(_uploadDir))
        //        Directory.CreateDirectory(_uploadDir);

        //    var fileName = Path.GetFileName(image.FileName);
        //    var savePath = Path.Combine(_uploadDir, fileName);

        //    using (var fileStream = new FileStream(savePath, FileMode.Create))
        //    {
        //        await image.CopyToAsync(fileStream);
        //    }

        //    return "/images/" + fileName; // trả về link để hiển thị
        //}

        // Chi tiết sản phẩm
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

        // Hiển thị form cập nhật
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        //// Xử lý cập nhật sản phẩm
        //[HttpPost]
        //public async Task<IActionResult> Update(int id, Product product, IFormFile imageFile)
        //{
        //    if (id != product.Id) return NotFound();

        //    if (ModelState.IsValid)
        //    {
        //        var existingProduct = await _productRepository.GetByIdAsync(id);
        //        if (existingProduct == null) return NotFound();

        //        existingProduct.Name = product.Name;
        //        existingProduct.Price = product.Price;
        //        existingProduct.Description = product.Description;
        //        existingProduct.CategoryId = product.CategoryId;

        //        if (imageFile != null)
        //        {
        //            existingProduct.ImageUrl = await SaveImage(imageFile);
        //        }

        //        await _productRepository.UpdateAsync(existingProduct);
        //        return RedirectToAction(nameof(Index));
        //    }

        //    var categories = await _categoryRepository.GetAllAsync();
        //    ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
        //    return View(product);
        //}

        // Xóa sản phẩm
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var product = await _productRepository.GetByIdAsync(id);
        //    if (product == null) return NotFound();

        //    return View(product);
        //}

        //[HttpPost, ActionName("Delete")]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    await _productRepository.DeleteAsync(id);
        //    return RedirectToAction(nameof(Index));
        //}
    }
}
