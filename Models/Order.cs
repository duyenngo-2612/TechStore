using System;
using System.Collections.Generic;

namespace TechStore.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int? VoucherId { get; set; }

    public DateTime? OrderDate { get; set; }

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
