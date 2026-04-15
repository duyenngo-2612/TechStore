using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Bắt buộc phải thêm dòng này

namespace TechStore.Models;

public partial class Order
{
    public int OrderId { get; set; }

    // Thêm thẻ Range để bắt buộc UserId phải từ 1 trở lên 
    [Range(1, int.MaxValue, ErrorMessage = "Tài khoản (UserId) không hợp lệ.")]
    public int UserId { get; set; }

    public int? VoucherId { get; set; }

    public DateTime? OrderDate { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Địa chỉ giao hàng không được để trống.")]
    public string ShippingAddress { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public int? Status { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Note { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual User User { get; set; } = null!;

    public virtual Voucher? Voucher { get; set; }

    public virtual Payment? Payment { get; set; }
}