using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;

public class ImportDetailsController : Controller
{
    private readonly TechStoreContext _context;

    public ImportDetailsController(TechStoreContext context)
    {
        _context = context;
    }

    // Xem danh sách các sản phẩm trong 1 Phiếu nhập cụ thể
    public async Task<IActionResult> Index(int? receiptId)
    {
        if (receiptId == null) return NotFound();

        var details = await _context.ImportDetails
            .Include(i => i.Product)
            .Where(m => m.ReceiptId == receiptId)
            .ToListAsync();

        ViewBag.ReceiptId = receiptId;
        return View(details);
    }
}