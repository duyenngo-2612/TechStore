using TechStore.Models;

namespace TechStore.DTOs
{
    public class CheckoutDataDto
    {
        public List<Cart> CartItems { get; set; } = new();
        public List<Voucher> SuggestedVouchers { get; set; } = new();
        public decimal Total { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalTotal => Total - Discount;
        public int? AppliedVoucherId { get; set; }
        public string VoucherMessage { get; set; }
        public string VoucherError { get; set; }
    }
}