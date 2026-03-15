using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;

namespace TechStore.Controllers
{
    public class CartController : Controller
    {
        private readonly TechStoreContext _context;
        public CartController(TechStoreContext context) { _context = context; }

        private async Task<int> GetCartCount(int userId)
        {
            var count = await _context.Carts.Where(c => c.UserId == userId).SumAsync(c => (int?)c.Quantity) ?? 0;
            HttpContext.Session.SetString("CartCount", count.ToString());
            return count;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");
            int userId = int.Parse(userIdStr);
            return View(await _context.Carts.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int change)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdStr);

            var item = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
            if (item != null)
            {
                item.Quantity += change;
                if (item.Quantity <= 0) _context.Carts.Remove(item);
                else _context.Update(item);
                await _context.SaveChangesAsync();
            }

            int newCount = await GetCartCount(userId);
            return Json(new { success = true, newCount = newCount });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdStr);

            var item = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
            if (item != null)
            {
                _context.Carts.Remove(item);
                await _context.SaveChangesAsync();
            }

            int newCount = await GetCartCount(userId);
            return Json(new { success = true, newCount = newCount });
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                if (isAjax) return Json(new { success = false, message = "Vui lòng đăng nhập" });
                return RedirectToAction("Login", "Account");
            }

            int userId = int.Parse(userIdStr);
            var item = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (item != null)
            {
                item.Quantity += quantity;
                _context.Update(item);
            }
            else
            {
                _context.Add(new Cart { UserId = userId, ProductId = productId, Quantity = quantity });
            }

            await _context.SaveChangesAsync();
            int newCount = await GetCartCount(userId);

            if (isAjax)
            {
                return Json(new { success = true, newCount = newCount });
            }
            else
            {
                TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer);
                }
                return RedirectToAction("Index", "Home");
            }
        }

        // ================= PHẦN MỚI THÊM: XỬ LÝ THANH TOÁN ================= //

        // 1. GET: Hiển thị form điền thông tin thanh toán
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            // Lấy giỏ hàng của user
            var cartItems = await _context.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index");
            }

            // Lấy thông tin User để tự động điền vào form thanh toán (nếu có)
            var user = await _context.Users.FindAsync(userId);
            ViewBag.User = user;

            return View(cartItems);
        }

        // 2. POST: Nhận thông tin từ form và lưu vào bảng Order, OrderDetail
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string shippingAddress, string paymentMethod, string note)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            // Lấy lại giỏ hàng
            var cartItems = await _context.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index");

            // Tính tổng tiền
            decimal totalAmount = cartItems.Sum(x => x.Quantity * x.Product.Price);

            // Tạo đơn hàng mới
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                ShippingAddress = shippingAddress ?? "Chưa cung cấp",
                PaymentMethod = paymentMethod ?? "COD",
                Status = 0, // 0: Chờ xác nhận
                TotalAmount = totalAmount,
                Note = note
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Lưu để lấy OrderId

            // Lưu chi tiết đơn hàng (OrderDetails)
            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                };
                _context.OrderDetails.Add(orderDetail);
            }

            // Xóa giỏ hàng sau khi đặt thành công
            _context.Carts.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            // Cập nhật lại số lượng giỏ hàng trên Session
            await GetCartCount(userId);

            // Chuyển hướng tới trang thông báo thành công
            TempData["SuccessMessage"] = "Đặt hàng thành công! Mã đơn hàng của bạn là #" + order.OrderId;
            return RedirectToAction("OrderSuccess");
        }

        // 3. GET: Trang thông báo đặt hàng thành công
        public IActionResult OrderSuccess()
        {
            return View();
        }
    }
}