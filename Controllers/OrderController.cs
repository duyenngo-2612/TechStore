using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;

namespace TechStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly TechStoreContext _context;

        public OrderController(TechStoreContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DÀNH CHO KHÁCH HÀNG (USER)
        // ==========================================

        // Danh sách đơn hàng cá nhân (ĐÃ CẬP NHẬT: LỌC THEO TAB SHOPEE STYLE)
        public async Task<IActionResult> Index(int? status)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            // Lấy danh sách đơn hàng kèm chi tiết sản phẩm để hiển thị ra View
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Where(o => o.UserId == userId);

            // Lọc theo trạng thái nếu người dùng bấm vào các Tab
            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            // Lưu trạng thái hiện tại để đổi màu Tab đang chọn
            ViewBag.CurrentStatus = status;

            return View(orders);
        }

        // Xem chi tiết một đơn hàng
        public async Task<IActionResult> Details(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == int.Parse(userIdStr));

            if (order == null) return NotFound();

            return View(order);
        }

        // Giao diện Thanh toán (Checkout) - CÓ GỢI Ý VOUCHER
        public async Task<IActionResult> Checkout(string voucherCode)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);
            var cartItems = await _context.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

            // 1. Lấy danh sách Voucher còn hạn để gợi ý cho khách
            ViewBag.SuggestVouchers = await _context.Vouchers
                .Where(v => v.StartDate <= DateTime.Now && v.EndDate >= DateTime.Now && (v.UsageLimit ?? 0) > 0)
                .Take(3)
                .ToListAsync();

            decimal total = cartItems.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);
            decimal discount = 0;

            // 2. Kiểm tra Voucher nếu khách nhập/chọn mã
            if (!string.IsNullOrEmpty(voucherCode))
            {
                var v = await _context.Vouchers.FirstOrDefaultAsync(x =>
                    x.Code == voucherCode &&
                    x.StartDate <= DateTime.Now &&
                    x.EndDate >= DateTime.Now &&
                    (x.UsageLimit ?? 0) > 0);

                if (v != null && total >= (v.MinOrderValue ?? 0))
                {
                    discount = total * (v.DiscountPercent ?? 0) / 100;
                    if (discount > (v.MaxDiscountAmount ?? 0)) discount = v.MaxDiscountAmount ?? 0;

                    ViewBag.VoucherId = v.VoucherId;
                    ViewBag.VoucherMessage = $"Áp dụng mã {voucherCode} thành công!";
                }
                else
                {
                    ViewBag.VoucherError = "Mã giảm giá không hợp lệ hoặc không đủ điều kiện.";
                }
            }

            ViewBag.Total = total;
            ViewBag.Discount = discount;
            ViewBag.FinalTotal = total - discount;

            return View(cartItems);
        }

        // Xử lý nút ĐẶT HÀNG - CÓ KIỂM TRA KHO
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string address, string paymentMethod, int? voucherId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdStr);

            var cartItems = await _context.Carts.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();

            // 1. Kiểm tra kho trước khi lưu (Chặn lỗi âm kho)
            foreach (var item in cartItems)
            {
                if (item.Product.StockQuantity < item.Quantity)
                {
                    TempData["Error"] = $"Sản phẩm '{item.Product.ProductName}' không đủ hàng (Hiện còn: {item.Product.StockQuantity})";
                    return RedirectToAction("Index", "Cart");
                }
            }

            decimal total = cartItems.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);
            decimal discount = 0;

            if (voucherId.HasValue)
            {
                var v = await _context.Vouchers.FindAsync(voucherId);
                if (v != null)
                {
                    discount = Math.Min(total * (v.DiscountPercent ?? 0) / 100, (v.MaxDiscountAmount ?? 0));
                    v.UsageLimit -= 1;
                }
            }

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
            await _context.SaveChangesAsync();

            // Lưu chi tiết & Trừ kho
            foreach (var item in cartItems)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product?.Price ?? 0
                });
                item.Product.StockQuantity -= item.Quantity;
            }

            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            HttpContext.Session.SetString("CartCount", "0");

            TempData["Success"] = "Đặt hàng thành công!";
            return RedirectToAction("Index"); // Tự động về tab Tất cả đơn hàng
        }

        // Hủy đơn hàng - CÓ HOÀN KHO (ĐÃ CẬP NHẬT STATUS = 4)
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == int.Parse(userIdStr));

            if (order != null && order.Status == 0)
            {
                // Sửa thành 4 để khớp với Database Constraint (0,1,2,3,4)
                order.Status = 4;

                // Hoàn lại số lượng vào kho
                var details = await _context.OrderDetails.Where(d => d.OrderId == orderId).ToListAsync();
                foreach (var item in details)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null) product.StockQuantity += item.Quantity;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy đơn hàng thành công và hoàn trả tồn kho.";
            }
            // Hủy xong thì chuyển khách sang Tab Đã hủy (status = 4)
            return RedirectToAction(nameof(Index), new { status = 4 });
        }

        // ==========================================
        // 2. DÀNH CHO QUẢN TRỊ VIÊN (ADMIN)
        // ==========================================

        // Trang danh sách đơn hàng toàn hệ thống
        public async Task<IActionResult> AdminIndex()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin") return RedirectToAction("Index", "Home");

            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Duyệt đơn hàng
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null && order.Status == 0)
            {
                order.Status = 1; // 1: Đã xác nhận/Đang chuẩn bị
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã duyệt đơn hàng #{id}";
            }
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}