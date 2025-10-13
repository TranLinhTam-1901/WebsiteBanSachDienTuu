using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required, StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        //[Required, StringLength(300)]
       // public string? Address { get; set; }

        [Required, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ShippingFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsPaid { get; set; } = false;
        public string PaymentMethod { get; set; } = "Fake";
        
        public string Status { get; set; } = "Pending";
        public string? TransactionId { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}


