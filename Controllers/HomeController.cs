using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;

namespace TechStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly TechStoreContext _context;
        public HomeController(TechStoreContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            var products = await _context.Products.Take(8).ToListAsync();
            return View(products);
        }

        // Tích hợp cả lọc theo Danh mục và Tìm kiếm theo tên
        public async Task<IActionResult> GetProductsByCategory(int? categoryId, string? search)
        {
            var products = _context.Products.AsQueryable();

            if (categoryId.HasValue && categoryId > 0)
                products = products.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrEmpty(search))
                products = products.Where(p => p.ProductName.Contains(search));

            return PartialView("_ProductListPartial", await products.ToListAsync());
        }
    }
}