using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public class ApplicationUser : IdentityUser
    {
        
        public String FullName { get; set; }
        public String? Address { get; set; }
        public String? Age { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
    }
}
