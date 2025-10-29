using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class BookEditorController : Controller
    {
        public BookEditorController()
        {
        }

        // Trang quản lý viết sách
        public IActionResult Index()
        {
            return View();
        }

        // API để lưu draft (tùy chọn - có thể lưu vào localStorage)
        [HttpPost]
        public IActionResult SaveDraft([FromBody] BookDraftModel model)
        {
            // Có thể lưu vào database nếu cần
            // Hiện tại sẽ dùng localStorage ở client
            return Json(new { success = true, message = "Lưu nháp thành công" });
        }

        // API để load draft
        [HttpGet]
        public IActionResult LoadDraft()
        {
            // Load từ database hoặc trả về empty
            return Json(new { success = true, chapters = new List<object>() });
        }
    }

    public class BookDraftModel
    {
        public string Title { get; set; } = string.Empty;
        public List<ChapterModel> Chapters { get; set; } = new List<ChapterModel>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    public class ChapterModel
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}

