using System;
using System.Collections.Generic;

namespace TechStore.Models;

public partial class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? TinhTp { get; set; }

    public string? QuanHuyen { get; set; }

    public string? PhuongXa { get; set; }

    public string? DiaChiCuThe { get; set; }

    public bool? IsEmailVerified { get; set; }

    public bool? Status { get; set; }

    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<ImportReceipt> ImportReceipts { get; set; } = new List<ImportReceipt>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Role Role { get; set; } = null!;
}
