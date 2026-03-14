using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;

namespace TechStore.Controllers
{
    public class ProductController : Controller
    {
        private readonly TechStoreContext _context;

        public ProductController(TechStoreContext context)
        {
            _context = context;
        }

        // HÀM KIỂM TRA QUYỀN ADMIN (Dùng nội bộ trong file này)
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        // ==========================================
        // KHÁCH HÀNG XEM ĐƯỢC
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var techStoreContext = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category);
            return View(await techStoreContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // ==========================================
        // CHỈ ADMIN MỚI ĐƯỢC THAO TÁC (CREATE, EDIT, DELETE)
        // ==========================================
        public IActionResult Create()
        {
            if (!IsAdmin()) { TempData["Error"] = "Chỉ Admin mới có quyền truy cập!"; return RedirectToAction(nameof(Index)); }

            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "BrandId");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,CategoryId,BrandId,ProductName,Description,Specifications,Price,StockQuantity,ImageUrl,IsActive,CreatedAt")] Product product)
        {
            if (!IsAdmin()) return RedirectToAction(nameof(Index));

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "BrandId", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin()) { TempData["Error"] = "Chỉ Admin mới có quyền truy cập!"; return RedirectToAction(nameof(Index)); }

            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "BrandId", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,CategoryId,BrandId,ProductName,Description,Specifications,Price,StockQuantity,ImageUrl,IsActive,CreatedAt")] Product product)
        {
            if (!IsAdmin()) return RedirectToAction(nameof(Index));

            if (id != product.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "BrandId", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin()) { TempData["Error"] = "Chỉ Admin mới có quyền truy cập!"; return RedirectToAction(nameof(Index)); }

            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction(nameof(Index));

            var product = await _context.Products.FindAsync(id);
            if (product != null) _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}