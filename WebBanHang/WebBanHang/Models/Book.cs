using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    [Table("Book")]   // ánh xạ đến bảng "Book" trong DB
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
       
        }


}
