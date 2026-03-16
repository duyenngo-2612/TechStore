using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;
using TechStore.Extensions; // Khai báo thư viện vừa tạo ở trên

namespace TechStore.Controllers
{
    public class CartController : Controller
    {
        private readonly TechStoreContext _context;
        public CartController(TechStoreContext context) { _context = context; }

        // =================================================================
        // HÀM HỖ TRỢ: ĐẾM SỐ LƯỢNG VÀ GỘP GIỎ HÀNG
        // =================================================================

        private async Task<int> GetCartCount()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            int count = 0;

            if (!string.IsNullOrEmpty(userIdStr)) // Đã đăng nhập
            {
                int userId = int.Parse(userIdStr);
                count = await _context.Carts.Where(c => c.UserId == userId).SumAsync(c => (int?)c.Quantity) ?? 0;
            }
            else // Chưa đăng nhập (khách vãng lai)
            {
                var sessionCart = HttpContext.Session.GetObjectFromJson<List<CartItemSession>>("GuestCart");
                if (sessionCart != null) count = sessionCart.Sum(c => c.Quantity);
            }

            HttpContext.Session.SetString("CartCount", count.ToString());
            return count;
        }

        // Hàm này cực kỳ quan trọng: Gộp đồ khách vãng lai đã chọn vào Database sau khi họ đăng nhập
        private async Task MergeSessionCartToDb(int userId)
        {
            var sessionCart = HttpContext.Session.GetObjectFromJson<List<CartItemSession>>("GuestCart");
            if (sessionCart != null && sessionCart.Any())
            {
                foreach (var item in sessionCart)
                {
                    var dbItem = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == item.ProductId);
                    if (dbItem != null)
                    {
                        dbItem.Quantity += item.Quantity; // Cộng dồn nếu đã có
                    }
                    else
                    {
                        _context.Carts.Add(new Cart { UserId = userId, ProductId = item.ProductId, Quantity = item.Quantity });
                    }
                }
                await _context.SaveChangesAsync();
                HttpContext.Session.Remove("GuestCart"); // Gộp xong thì xóa Session tạm đi
            }
        }

        // =================================================================
        // CÁC CHỨC NĂNG GIỎ HÀNG (Cho phép cả khách và user)
        // =================================================================

        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdStr)) // NẾU ĐÃ ĐĂNG NHẬP
            {
                int userId = int.Parse(userIdStr);
                await MergeSessionCartToDb(userId); // Gộp giỏ hàng nếu có
                var dbCart = await _context.Carts.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();
                return View(dbCart);
            }
            else // NẾU CHƯA ĐĂNG NHẬP
            {
                var sessionCart = HttpContext.Session.GetObjectFromJson<List<CartItemSession>>("GuestCart") ?? new List<CartItemSession>();
                var viewCart = new List<Cart>();

                // Chuyển từ Session sang Model Cart để View hiển thị bình thường không bị lỗi
                foreach (var item in sessionCart)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        viewCart.Add(new Cart { ProductId = item.ProductId, Quantity = item.Quantity, Product = product });
                    }
                }
                return View(viewCart);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdStr)) // NẾU ĐÃ ĐĂNG NHẬP (Lưu DB)
            {
                int userId = int.Parse(userIdStr);
                var item = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
                if (item != null) item.Quantity += quantity;
                else _context.Add(new Cart { UserId = userId, ProductId = productId, Quantity = quantity });

                await _context.SaveChangesAsync();
            }
            else // NẾU CHƯA ĐĂNG NHẬP (Lưu Session)
            {
                var sessionCart = HttpContext.Session.GetObjectFromJson<List<CartItemSession>>("GuestCart") ?? new List<CartItemSession>();
                var item = sessionCart.FirstOrDefault(c => c.ProductId == productId);

                if (item != null) item.Quantity += quantity;
                else sessionCart.Add(new CartItemSession { ProductId = productId, Quantity = quantity });

                HttpContext.Session.SetObjectAsJson("GuestCart", sessionCart);
            }

            int newCount = await GetCartCount();

            if (isAjax) return Json(new { success = true, newCount = newCount });

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";
            var referer = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int change)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdStr)) // Cập nhật DB
            {
                int userId = int.Parse(userIdStr);
                var item = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
                if (item != null)
                {
                    item.Quantity += change;
                    if (item.Quantity <= 0) _context.Carts.Remove(item);
                    await _context.SaveChangesAsync();
                }
            }
            else // Cập nhật Session
            {
                var sessionCart = HttpContext.Session.GetObjectFromJson<List<CartItemSession>>("GuestCart");
                if (sessionCart != null)
                {
                    var item = sessionCart.FirstOrDefault(c => c.ProductId == productId);
                    if (item != null)
                    {
                        item.Quantity += change;
                        if (item.Quantity <= 0) sessionCart.Remove(item);
                        HttpContext.Session.SetObjectAsJson("GuestCart", sessionCart);
                    }
                }
            }

            return Json(new { success = true, newCount = await GetCartCount() });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdStr)) // Xóa DB
            {
                int userId = int.Parse(userIdStr);
                var item = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
                if (item != null)
                {
                    _context.Carts.Remove(item);
                    await _context.SaveChangesAsync();
                }
            }
            else // Xóa Session
            {
                var sessionCart = HttpContext.Session.GetObjectFromJson<List<CartItemSession>>("GuestCart");
                if (sessionCart != null)
                {
                    var item = sessionCart.FirstOrDefault(c => c.ProductId == productId);
                    if (item != null)
                    {
                        sessionCart.Remove(item);
                        HttpContext.Session.SetObjectAsJson("GuestCart", sessionCart);
                    }
                }
            }

            return Json(new { success = true, newCount = await GetCartCount() });
        }

        // =================================================================
        // THANH TOÁN (Yêu cầu phải đăng nhập)
        // =================================================================

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            // LUỒNG CHUẨN UX: Bấm thanh toán mà chưa đăng nhập thì đẩy ra trang Login
            if (string.IsNullOrEmpty(userIdStr))
            {
                TempData["Message"] = "Vui lòng đăng nhập tài khoản để tiến hành đặt hàng.";
                return RedirectToAction("Login", "Account");
            }

            int userId = int.Parse(userIdStr);

            // Gộp giỏ hàng (Trường hợp khách vãng lai đi nhặt đồ, bị ép login, login xong quay lại đây thì đồ vẫn còn)
            await MergeSessionCartToDb(userId);

            var cartItems = await _context.Carts.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();
            if (!cartItems.Any()) return RedirectToAction("Index");

            ViewBag.User = await _context.Users.FindAsync(userId);
            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string shippingAddress, string paymentMethod, string note)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);
            var cartItems = await _context.Carts.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index");

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                ShippingAddress = shippingAddress ?? "Chưa cung cấp",
                PaymentMethod = paymentMethod ?? "COD",
                Status = 0,
                TotalAmount = cartItems.Sum(x => x.Quantity * x.Product.Price),
                Note = note
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                });
            }

            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            await GetCartCount();

            TempData["SuccessMessage"] = "Đặt hàng thành công! Mã đơn hàng: #" + order.OrderId;
            return RedirectToAction("OrderSuccess");
        }

        public IActionResult OrderSuccess() => View();
    }
}