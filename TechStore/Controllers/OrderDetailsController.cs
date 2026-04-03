using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;

public class OrderDetailsController : Controller
{
    private readonly TechStoreContext _context;

    public OrderDetailsController(TechStoreContext context)
    {
        _context = context;
    }

    // Xem danh sách món hàng trong 1 đơn hàng cụ thể
    public async Task<IActionResult> Index(int? orderId)
    {
        if (orderId == null) return NotFound();

        var details = await _context.OrderDetails
            .Include(o => o.Product)
            .Where(m => m.OrderId == orderId) // Lọc theo đơn hàng
            .ToListAsync();

        ViewBag.OrderId = orderId;
        return View(details);
    }

    // Xóa một món hàng khỏi đơn hàng (Cần cả OrderId và ProductId)
    [HttpPost]
    public async Task<IActionResult> Delete(int orderId, int productId)
    {
        var detail = await _context.OrderDetails
            .FirstOrDefaultAsync(m => m.OrderId == orderId && m.ProductId == productId);

        if (detail != null)
        {
            _context.OrderDetails.Remove(detail);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), new { orderId = orderId });
    }
}