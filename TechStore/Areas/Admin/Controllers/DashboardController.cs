using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;

namespace TechStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly TechStoreContext _context;

        public DashboardController(TechStoreContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Thống kê số liệu cho 4 thẻ Top Card
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalReviews = await _context.Reviews.CountAsync();

            // Doanh thu từ đơn hàng thành công (Lưu ý: Status = 3 là Hoàn thành)
            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.Status == 3)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // THÊM: Lấy 5 đơn hàng mới nhất truyền ra View
            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // Lấy 5 sản phẩm đắt nhất/mới nhất để hiện bên phải Prototype
            var topProducts = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Price)
                .Take(5)
                .ToListAsync();

            return View(topProducts);
        }
    }
}