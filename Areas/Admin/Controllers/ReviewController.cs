using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;

namespace TechStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReviewController : Controller
    {
        private readonly TechStoreContext _context;

        public ReviewController(TechStoreContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy danh sách đánh giá kèm thông tin Sản phẩm và Người dùng
            var reviews = await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            return View(reviews);
        }

        // Hàm Ẩn/Hiện đánh giá (nếu khách comment bậy bạ thì Admin có thể ẩn đi)
        [HttpPost]
        public async Task<IActionResult> ToggleVisibility(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review != null)
            {
                review.IsVisible = !(review.IsVisible ?? true); // Đảo trạng thái
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}