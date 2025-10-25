using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanHang.Models;
using WebBanHang.Repositories;
using ClosedXML.Excel;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ImportController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ImportController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file Excel hợp lệ!";
                return RedirectToAction(nameof(Index));
            }

            int addedCount = 0, skippedCount = 0;

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Bỏ dòng tiêu đề

                    foreach (var row in rows)
                    {
                        try
                        {
                            string name = row.Cell(1).GetString().Trim();
                            string author = row.Cell(2).GetString().Trim();
                            string priceText = row.Cell(3).GetString().Trim();
                            string desc = row.Cell(4).GetString().Trim();
                            string categoryName = row.Cell(5).GetString().Trim();
                            string imageFileName = row.Cell(6).GetString().Trim();
                            string pdfFileName = row.Cell(7).GetString().Trim();

                            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(priceText))
                            {
                                skippedCount++;
                                continue;
                            }

                            var categories = await _categoryRepository.GetAllAsync();
                            var category = categories.FirstOrDefault(c => c.Name == categoryName);
                            if (category == null && !string.IsNullOrEmpty(categoryName))
                            {
                                category = new Category { Name = categoryName };
                                await _categoryRepository.AddAsync(category);
                            }

                            var product = new Product
                            {
                                Name = name,
                                Author = author,
                                Description = desc,
                                Price = decimal.TryParse(priceText, out decimal p) ? p : 0,
                                CategoryId = category?.Id ?? 0,
                                ImageUrl = string.IsNullOrEmpty(imageFileName) ? null : $"/images/products/{imageFileName}",
                                BookContentUrl = string.IsNullOrEmpty(pdfFileName) ? null : $"/books/{pdfFileName}"
                            };

                            await _productRepository.AddAsync(product);
                            addedCount++;
                        }
                        catch
                        {
                            skippedCount++;
                        }
                    }
                }
            }

            TempData["SuccessMessage"] = $"✅ Import thành công {addedCount} sản phẩm, bỏ qua {skippedCount} dòng lỗi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
