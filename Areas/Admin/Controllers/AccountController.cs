using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechStore.Models;

namespace TechStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly TechStoreContext _context;

        public AccountController(TechStoreContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                // Tự động sửa lỗi băm mật khẩu
                if (!string.IsNullOrEmpty(user.PasswordHash) && !user.PasswordHash.StartsWith("$"))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    await _context.SaveChangesAsync();
                }

                bool isPasswordValid = false;
                try { isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash); } catch { }

                if (isPasswordValid && user.Role.RoleName == "Admin")
                {
                    // 1. Cấp thẻ Cookie
                    var claims = new List<Claim> {
                        new Claim(ClaimTypes.Name, user.FullName ?? "Admin"),
                        new Claim(ClaimTypes.Role, "Admin")
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                    await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

                    // 2. Lưu Session để hiện lên Sidebar
                    HttpContext.Session.SetString("AdminName", user.FullName);
                    HttpContext.Session.SetString("AdminEmail", user.Email);

                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
            }
            ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
            return View();
        }

        // NÚT ĐĂNG XUẤT SẼ GỌI VÀO ĐÂY
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }
    }
}