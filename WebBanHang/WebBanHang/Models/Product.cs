using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } =  string.Empty;

        [Required]
        public string Author { get; set; } = string.Empty;  

        public string? BookContentUrl  { get; set; } 


        [Range(0.01, 10000000)]
        [Column(TypeName = "decimal(18,2)")]   // Khai báo rõ precision & scale
        public decimal Price { get; set; }

        public string Description { get; set; }  = string.Empty ;
        public string? ImageUrl { get; set; }

        public List<ProductImage>? Images { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public string? UserId { get; set; }
        
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

       

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

    }

}

