using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechStore.Models;

public partial class ImportDetail
{
    public int ReceiptId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal ImportPrice { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ImportReceipt Receipt { get; set; } = null!;
}
