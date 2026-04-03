using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechStore.Models;

public partial class ImportReceipt
{

    [Key]
    public int ReceiptId { get; set; }

    public int SupplierId { get; set; }

    public int UserId { get; set; }

    public DateTime? ImportDate { get; set; }

    public decimal? TotalCost { get; set; }

    public string? Note { get; set; }

    public virtual ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
