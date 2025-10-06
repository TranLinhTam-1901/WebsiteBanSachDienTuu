using Microsoft.AspNetCore.Mvc;
using WebBanHang.Models; 
using System.Linq;

public class BooksController : Controller
{
    private readonly ApplicationDbContext _context;

    public BooksController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Action tìm kiếm
    public IActionResult Search(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return View("SearchResults", new List<Book>()); // Trả về view rỗng
        }

        var results = _context.Books
            .Where(b => b.Title.Contains(keyword)
                     || b.Author.Contains(keyword)
                     || b.Category.Contains(keyword))
            .ToList();

        return View("SearchResults", results);
    }
}
