using Microsoft.AspNetCore.Mvc;
using TechStore.Services;
using TechStore.Models;
using Microsoft.EntityFrameworkCore; // Vẫn tạm giữ để phục vụ hàm PlaceOrder

namespace TechStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly TechStoreContext _context; // Tạm giữ lại cho hàm PlaceOrder (Sẽ rút nốt ở bước sau)

        // Inject cả Service và DbContext
        public CartController(ICartService cartService, TechStoreContext context)
        {
            _cartService = cartService;
            _context = context;
        }

        // =================================================================
        // HÀM HỖ TRỢ DÙNG CHUNG TRONG CONTROLLER
        // =================================================================
        private int? GetUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            // Hỗ trợ trường hợp test Auth ảo mà bạn đã viết trước đó
            if (string.IsNullOrEmpty(userIdStr) && User.Identity != null && User.Identity.IsAuthenticated)
            {
                userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }

            return string.IsNullOrEmpty(userIdStr) ? null : int.Parse(userIdStr);
        }

        // =================================================================
        // CÁC CHỨC NĂNG GIỎ HÀNG (Đã được làm mỏng tối đa)
        // =================================================================

        public async Task<IActionResult> Index()
        {
            // Chỉ 1 dòng code: Gọi service lấy danh sách item (đã bao gồm tự động gộp Session nếu có)
            var cartItems = await _cartService.GetCartItemsAsync(GetUserId());
            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            // Nhờ Service xử lý logic thêm vào DB hoặc Session
            await _cartService.AddToCartAsync(productId, quantity, GetUserId());
            int newCount = await _cartService.GetCartCountAsync(GetUserId());

            if (isAjax) return Json(new { success = true, newCount = newCount });

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";
            var referer = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int change)
        {
            await _cartService.UpdateQuantityAsync(productId, change, GetUserId());
            int newCount = await _cartService.GetCartCountAsync(GetUserId());
            return Json(new { success = true, newCount = newCount });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            await _cartService.RemoveFromCartAsync(productId, GetUserId());
            int newCount = await _cartService.GetCartCountAsync(GetUserId());
            return Json(new { success = true, newCount = newCount });
        }

        // =================================================================
        // THANH TOÁN
        // =================================================================

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                TempData["Message"] = "Vui lòng đăng nhập tài khoản để tiến hành đặt hàng.";
                return RedirectToAction("Login", "Account");
            }

            // Gọi service lấy giỏ hàng ra (nó đã tự động Merge session ở bên trong)
            var cartItems = await _cartService.GetCartItemsAsync(userId);
            if (!cartItems.Any()) return RedirectToAction("Index");

            ViewBag.User = await _context.Users.FindAsync(userId.Value);
            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string shippingAddress, string paymentMethod, string note)
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var cartItems = await _context.Carts.Include(c => c.Product).Where(c => c.UserId == userId.Value).ToListAsync();
            if (!cartItems.Any()) return RedirectToAction("Index");

            // ⚠️ CHÚ Ý: Toàn bộ khối code tạo đơn, lưu OrderDetails, và trừ tồn kho dưới đây 
            // hiện tại mình vẫn giữ nguyên trong Controller để đảm bảo code của bạn chạy được ngay.
            // Mục tiêu tiếp theo là cắt toàn bộ khối này mang sang `OrderService`.

            var order = new Order
            {
                UserId = userId.Value,
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

            // Cập nhật lại số lượng giỏ hàng trên Header sau khi mua xong
            await _cartService.GetCartCountAsync(userId);

            if (paymentMethod == "EWallet")
            {
                return RedirectToAction("CreatePaymentUrl", "Payment", new { orderId = order.OrderId });
            }

            TempData["SuccessMessage"] = "Đặt hàng thành công! Mã đơn hàng: #" + order.OrderId;
            return RedirectToAction("OrderSuccess");
        }

        public IActionResult OrderSuccess() => View();
    }
}