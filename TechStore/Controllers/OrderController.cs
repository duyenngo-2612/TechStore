using Microsoft.AspNetCore.Mvc;
using TechStore.Services;
using TechStore.DTOs;

namespace TechStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private int? GetUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return string.IsNullOrEmpty(userIdStr) ? null : int.Parse(userIdStr);
        }

        // ==========================================
        // 1. DÀNH CHO KHÁCH HÀNG (USER)
        // ==========================================

        public async Task<IActionResult> Index(int? status)
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var orders = await _orderService.GetCustomerOrdersAsync(userId.Value, status);
            ViewBag.CurrentStatus = status;
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = await _orderService.GetOrderDetailsAsync(id, userId.Value);
            return order == null ? NotFound() : View(order);
        }

        public async Task<IActionResult> Checkout(string voucherCode)
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var checkoutData = await _orderService.GetCheckoutDataAsync(userId.Value, voucherCode);
            if (!checkoutData.CartItems.Any()) return RedirectToAction("Index", "Cart");

            // Đổ dữ liệu từ ViewModel ra ViewBag (để tương thích với View .cshtml hiện tại của bạn)
            ViewBag.SuggestVouchers = checkoutData.SuggestedVouchers;
            ViewBag.VoucherId = checkoutData.AppliedVoucherId;
            ViewBag.VoucherMessage = checkoutData.VoucherMessage;
            ViewBag.VoucherError = checkoutData.VoucherError;
            ViewBag.Total = checkoutData.Total;
            ViewBag.Discount = checkoutData.Discount;
            ViewBag.FinalTotal = checkoutData.FinalTotal;

            return View(checkoutData.CartItems);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string address, string paymentMethod, int? voucherId)
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var result = await _orderService.PlaceOrderAsync(userId.Value, address, paymentMethod, voucherId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("Index", "Cart");
            }

            HttpContext.Session.SetString("CartCount", "0"); // Reset Session đếm giỏ hàng

            if (paymentMethod == "EWallet")
            {
                return RedirectToAction("CreatePaymentUrl", "Payment", new { orderId = result.OrderId });
            }

            TempData["Success"] = "Đặt hàng thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            bool success = await _orderService.CancelOrderAsync(orderId, userId.Value);
            if (success) TempData["Success"] = "Đã hủy đơn hàng thành công và hoàn trả tồn kho.";
            else TempData["Error"] = "Không thể hủy đơn hàng này.";

            return RedirectToAction(nameof(Index), new { status = 4 });
        }

        // ==========================================
        // 2. DÀNH CHO QUẢN TRỊ VIÊN (ADMIN)
        // ==========================================

        public async Task<IActionResult> AdminIndex()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Index", "Home");
            var orders = await _orderService.GetAllOrdersForAdminAsync();
            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            bool success = await _orderService.ConfirmOrderAsync(id);
            if (success) TempData["Success"] = $"Đã duyệt đơn hàng #{id}";
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}