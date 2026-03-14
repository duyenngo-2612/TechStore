using Microsoft.EntityFrameworkCore;
using TechStore.Models;

namespace TechStore.Models
{
    public class TechStoreContext : DbContext
    {
        public TechStoreContext(DbContextOptions<TechStoreContext> options) : base(options)
        {
        }

        // Khai báo 14 bảng dữ liệu (DbSet)
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Cart> Carts { get; set; } // Trong code gọi là Carts, nhưng SQL sẽ là Cart
        public DbSet<Category> Categories { get; set; }
        public DbSet<ImportDetail> ImportDetails { get; set; }
        public DbSet<ImportReceipt> ImportReceipts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Gọi base trước để EF áp dụng các cấu hình mặc định
            base.OnModelCreating(modelBuilder);

            // 1. Cấu hình Khóa chính phức hợp (Composite Key) cho các bảng trung gian

            // Bảng Cart (Giỏ hàng)
            modelBuilder.Entity<Cart>()
                .HasKey(c => new { c.UserId, c.ProductId });
            modelBuilder.Entity<Cart>().ToTable("Cart"); // Ép tên bảng SQL là "Cart" (số ít)

            // Bảng Chi tiết nhập hàng
            modelBuilder.Entity<ImportDetail>()
                .HasKey(id => new { id.ReceiptId, id.ProductId });

            // Bảng Chi tiết đơn hàng
            modelBuilder.Entity<OrderDetail>()
                .HasKey(od => new { od.OrderId, od.ProductId });

            // 2. Cấu hình quan hệ đệ quy cho Category (Danh mục cha - con)
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.InverseParent)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // Tránh xóa cha xóa luôn con ngoài ý muốn

            // 3. Cấu hình độ chính xác cho kiểu decimal (Tránh cảnh báo khi Build/Migration)
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

                foreach (var property in properties)
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            }
        }
    }
}