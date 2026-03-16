using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;
using TechStore.Services; // Nhớ đổi namespace nếu file VnPayLibrary bạn để chỗ khác

namespace TechStore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly TechStoreContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(TechStoreContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> CreatePaymentUrl(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            string vnp_Returnurl = Url.Action("PaymentCallback", "Payment", null, Request.Scheme);
            string vnp_Url = _configuration["VnPay:BaseUrl"];
            string vnp_TmnCode = _configuration["VnPay:TmnCode"];
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];

            VnPayLibrary vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);

            // SỬA LỖI 1: Ép kiểu sang long (số nguyên) để đảm bảo không bị dính dấu phẩy thập phân
            vnpay.AddRequestData("vnp_Amount", ((long)(order.TotalAmount * 100)).ToString());

            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");

            // SỬA LỖI 2: Fix cứng IP về IPv4 nội bộ khi chạy Localhost để VNPay không bắt bẻ
            vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");

            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang: " + order.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);

            // SỬA LỖI 3: Cộng thêm thời gian (Ticks) vào OrderId để mỗi lần gửi sang VNPay là 1 mã duy nhất (Tránh lỗi trùng lặp khi test đi test lại)
            string tick = DateTime.Now.Ticks.ToString();
            vnpay.AddRequestData("vnp_TxnRef", order.OrderId.ToString() + "_" + tick);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return Redirect(paymentUrl);
        }

        public async Task<IActionResult> PaymentCallback()
        {
            if (Request.Query.Count > 0)
            {
                string vnp_HashSecret = _configuration["VnPay:HashSecret"];
                var vnpayData = Request.Query;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (string s in vnpayData.Keys)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s, vnpayData[s]);
                    }
                }

                // CẬP NHẬT LẠI LÚC NHẬN VỀ: Tách bỏ phần thời gian (Ticks) ra, chỉ lấy lại mã OrderId gốc
                string txnRef = vnpay.GetResponseData("vnp_TxnRef");
                int orderId = Convert.ToInt32(txnRef.Split('_')[0]);

                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_SecureHash = Request.Query["vnp_SecureHash"];

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00")
                    {
                        var order = await _context.Orders.FindAsync(orderId);
                        if (order != null)
                        {
                            order.Status = 1;
                            await _context.SaveChangesAsync();
                        }

                        TempData["SuccessMessage"] = "Thanh toán thành công! Mã giao dịch: " + orderId;
                        return RedirectToAction("OrderSuccess", "Cart");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Thanh toán thất bại hoặc đã bị hủy. Mã lỗi: " + vnp_ResponseCode;
                        return RedirectToAction("PaymentFailed");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình xử lý. Chữ ký không hợp lệ!";
                    return RedirectToAction("PaymentFailed");
                }
            }
            return RedirectToAction("Index", "Home");
        }

        public IActionResult PaymentFailed()
        {
            return View();
        }
    }
}