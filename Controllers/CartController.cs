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

        // ĐÃ SỬA: Hỗ trợ cả AJAX và Form Submit truyền thống
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            // Kiểm tra xem đây có phải là request từ AJAX không
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

            // NẾU LÀ AJAX (Trang chi tiết sản phẩm) -> Trả về JSON
            if (isAjax)
            {
                return Json(new { success = true, newCount = newCount });
            }
            // NẾU LÀ FORM BÌNH THƯỜNG (Trang chủ) -> Quay lại trang vừa bấm
            else
            {
                TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer); // Trở về đúng trang trước đó
                }
                return RedirectToAction("Index", "Home");
            }
        }
    }
}