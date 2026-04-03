using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;

namespace TechStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly TechStoreContext _context;

        public OrderController(TechStoreContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách đơn hàng
        public async Task<IActionResult> Index()
        {
            // Lấy đơn hàng kèm thông tin Khách hàng, sắp xếp mới nhất lên đầu
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Xử lý cập nhật trạng thái
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, int newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = newStatus;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái cho đơn hàng #{orderId}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}