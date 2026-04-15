using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Models 
{
    [Table("Cart")]
    public class Cart
    {
        [Key, Column(Order = 0)]
        public int UserId { get; set; }

        [Key, Column(Order = 1)]
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        // Navigation properties (Giúp lấy thông tin Sản phẩm và Người dùng)
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}