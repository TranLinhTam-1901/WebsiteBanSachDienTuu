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
            return View(new Product());
        }

        [HttpPost]
        public async Task<IActionResult> Add(Product product, IFormFile? ImageFile, IFormFile? PdfFile)
        {
            if (ModelState.IsValid)
            {
                // Upload ảnh bìa
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    if (!IsImage(ImageFile))
                    {
                        ModelState.AddModelError("ImageUrl", "Chỉ được upload file ảnh (jpg, jpeg, png).");
                        return await ReloadAddView(product);
                    }
                    product.ImageUrl = await SaveFile(ImageFile, "images/products");
                }

                // Upload file sách (PDF)
                if (PdfFile != null && PdfFile.Length > 0)
                {
                    if (!IsPdf(PdfFile))
                    {
                        ModelState.AddModelError("BookContentUrl", "Chỉ được upload file PDF.");
                        return await ReloadAddView(product);
                    }
                    product.BookContentUrl = await SaveFile(PdfFile, "books");
                }

                await _productRepository.AddAsync(product);
                return RedirectToAction(nameof(Index));
            }

            return await ReloadAddView(product);
        }
        [HttpGet]
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _productRepository.GetByIdAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Update(Product product, IFormFile? ImageFile, IFormFile? PdfFile)
        {
            if (ModelState.IsValid)
            {
                // Xử lý file ảnh mới
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    if (!IsImage(ImageFile))
                    {
                        ModelState.AddModelError("ImageUrl", "Chỉ được upload file ảnh (jpg, jpeg, png).");
                        return await ReloadUpdateView(product);
                    }
                    product.ImageUrl = await SaveFile(ImageFile, "images/products");
                }

                // Xử lý file PDF mới
                if (PdfFile != null && PdfFile.Length > 0)
                {
                    if (!IsPdf(PdfFile))
                    {
                        ModelState.AddModelError("BookContentUrl", "Chỉ được upload file PDF.");
                        return await ReloadUpdateView(product);
                    }
                    product.BookContentUrl = await SaveFile(PdfFile, "books");
                }

                await _productRepository.UpdateAsync(product);
                return RedirectToAction(nameof(Index));
            }

            return await ReloadUpdateView(product);
        }

     

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _productRepository.GetByIdAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product != null)
            {
                await _productRepository.DeleteAsync(id);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> ReloadUpdateView(Product product)
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View("Update", product);
        }


        private async Task<IActionResult> ReloadAddView(Product product)
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View("Add", product);
        }

        private async Task<string> SaveFile(IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/{folder}/{fileName}";
        }

        private bool IsImage(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png";
        }

        private bool IsPdf(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLower();
            return ext == ".pdf";
        }
}
}
