using Microsoft.EntityFrameworkCore;
using TechStore.Models;
using TechStore.DTOs;

namespace TechStore.Services
{
    public class OrderService : IOrderService
    {
        private readonly TechStoreContext _context;

        public OrderService(TechStoreContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetCustomerOrdersAsync(int userId, int? status)
        {
            var query = _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .Where(o => o.UserId == userId);

            if (status.HasValue) query = query.Where(o => o.Status == status.Value);

            return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<Order> GetOrderDetailsAsync(int orderId, int userId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
        }

        public async Task<CheckoutDataDto> GetCheckoutDataAsync(int userId, string voucherCode)
        {
            var model = new CheckoutDataDto();
            model.CartItems = await _context.Carts.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();

            if (!model.CartItems.Any()) return model;

            model.SuggestedVouchers = await _context.Vouchers
                .Where(v => v.StartDate <= DateTime.Now && v.EndDate >= DateTime.Now && (v.UsageLimit ?? 0) > 0)
                .Take(3).ToListAsync();

            model.Total = model.CartItems.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);

            if (!string.IsNullOrEmpty(voucherCode))
            {
                var v = await _context.Vouchers.FirstOrDefaultAsync(x => x.Code == voucherCode &&
                    x.StartDate <= DateTime.Now && x.EndDate >= DateTime.Now && (x.UsageLimit ?? 0) > 0);

                if (v != null && model.Total >= (v.MinOrderValue ?? 0))
                {
                    model.Discount = Math.Min(model.Total * (v.DiscountPercent ?? 0) / 100, v.MaxDiscountAmount ?? 0);
                    model.AppliedVoucherId = v.VoucherId;
                    model.VoucherMessage = $"Áp dụng mã {voucherCode} thành công!";
                }
                else
                {
                    model.VoucherError = "Mã giảm giá không hợp lệ hoặc không đủ điều kiện.";
                }
            }
            return model;
        }

        public async Task<PlaceOrderResultDto> PlaceOrderAsync(int userId, string address, string paymentMethod, int? voucherId)
        {
            var cartItems = await _context.Carts.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();
            if (!cartItems.Any()) return new PlaceOrderResultDto { Success = false, ErrorMessage = "Giỏ hàng trống." };

            // 1. Kiểm tra tồn kho
            foreach (var item in cartItems)
            {
                if (item.Product.StockQuantity < item.Quantity)
                    return new PlaceOrderResultDto { Success = false, ErrorMessage = $"Sản phẩm '{item.Product.ProductName}' không đủ hàng (Hiện còn: {item.Product.StockQuantity})" };
            }

            decimal total = cartItems.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);
            decimal discount = 0;

            // 2. Tính Voucher & Trừ số lượt dùng
            if (voucherId.HasValue)
            {
                var v = await _context.Vouchers.FindAsync(voucherId);
                if (v != null)
                {
                    discount = Math.Min(total * (v.DiscountPercent ?? 0) / 100, (v.MaxDiscountAmount ?? 0));
                    v.UsageLimit -= 1;
                }
            }

            // 3. Tạo đơn hàng
            var order = new Order
            {
                UserId = userId,
                VoucherId = voucherId,
                OrderDate = DateTime.Now,
                ShippingAddress = address,
                PaymentMethod = paymentMethod,
                Status = 0,
                TotalAmount = total - discount
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Lưu để lấy OrderId

            // 4. Lưu chi tiết, Trừ kho, Xóa giỏ hàng
            foreach (var item in cartItems)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product?.Price ?? 0
                });
                item.Product.StockQuantity -= item.Quantity; // Trừ kho
            }

            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return new PlaceOrderResultDto { Success = true, OrderId = order.OrderId };
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
            if (order == null || order.Status != 0) return false;

            order.Status = 4; // Hủy
            var details = await _context.OrderDetails.Where(d => d.OrderId == orderId).ToListAsync();
            foreach (var item in details)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null) product.StockQuantity += item.Quantity; // Hoàn kho
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Order>> GetAllOrdersForAdminAsync()
        {
            return await _context.Orders.Include(o => o.User).OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<bool> ConfirmOrderAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null && order.Status == 0)
            {
                order.Status = 1;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}