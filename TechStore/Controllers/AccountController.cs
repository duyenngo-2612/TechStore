using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;
using BCrypt.Net;
using TechStore.Services.Interface;
using TechStore.ViewModel;

namespace TechStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly TechStoreContext _context;
        private readonly IEmailService _emailService;

        public AccountController(TechStoreContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ==========================================
        // 1. ĐĂNG KÝ & GỬI MAIL OTP
        // ==========================================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password)
        {
            var checkEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (checkEmail != null)
            {
                ViewBag.Error = "Email này đã được đăng ký! Vui lòng dùng email khác.";
                return View();
            }

            // Tạo OTP ngẫu nhiên (sử dụng string interpolation để ép kiểu an toàn)
            string otp = $"{new Random().Next(100000, 999999)}";

            // Mã hóa mật khẩu trước khi lưu tạm vào Session
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            HttpContext.Session.SetString("RegEmail", email);
            HttpContext.Session.SetString("RegName", fullName);
            HttpContext.Session.SetString("RegPass", passwordHash);
            HttpContext.Session.SetString("OTP", otp);

            string subject = "Mã xác nhận đăng ký tài khoản TechStore";
            string body = $@"<h3>Chào {fullName},</h3>
                             <p>Cảm ơn bạn đã đăng ký tài khoản. Mã xác nhận (OTP) của bạn là: <strong style='color:red; font-size: 20px;'>{otp}</strong></p>
                             <p>Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>";

            try
            {
                await _emailService.SendEmailAsync(email, subject, body);
                return RedirectToAction("VerifyOTP");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi hệ thống: Không thể gửi email xác nhận. Chi tiết: " + ex.Message;
                return View();
            }
        }

        // ==========================================
        // 2. XÁC NHẬN OTP & LƯU DATABASE
        // ==========================================
        [HttpGet]
        public IActionResult VerifyOTP()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOTP(string otp)
        {
            var sessionOtp = HttpContext.Session.GetString("OTP");

            if (string.IsNullOrEmpty(sessionOtp) || sessionOtp != otp)
            {
                ViewBag.Error = "Mã OTP không chính xác hoặc đã hết hạn!";
                return View();
            }

            var email = HttpContext.Session.GetString("RegEmail");
            var name = HttpContext.Session.GetString("RegName");
            var passHash = HttpContext.Session.GetString("RegPass");

            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
            int roleId = customerRole != null ? customerRole.RoleId : 2;

            var newUser = new User
            {
                FullName = name ?? "Người dùng mới",
                Email = email ?? "",
                PasswordHash = passHash ?? "",
                RoleId = roleId,
                Status = true,
                CreatedAt = DateTime.Now
            };

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                HttpContext.Session.Remove("OTP");
                HttpContext.Session.Remove("RegEmail");
                HttpContext.Session.Remove("RegName");
                HttpContext.Session.Remove("RegPass");

                // Đăng nhập tự động
                HttpContext.Session.SetString("UserEmail", newUser.Email);
                HttpContext.Session.SetString("UserName", newUser.FullName);
                HttpContext.Session.SetString("UserId", $"{newUser.UserId}");
                HttpContext.Session.SetString("UserRole", "Customer");

                TempData["Success"] = "Đăng ký thành công! Chào mừng bạn đến với TechStore.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ViewBag.Error = "Lỗi lưu Database: " + errorMsg;
                return View();
            }
        }

        // ==========================================
        // 3. ĐĂNG NHẬP
        // ==========================================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                // KHÓA CHẶT: Chặn Admin không cho đăng nhập ở trang của Khách hàng
                if (user.Role.RoleName == "Admin")
                {
                    ViewBag.Error = "Đây là trang của Khách hàng. Quản trị viên vui lòng đăng nhập tại đường dẫn /Admin/Account/Login !";
                    return View();
                }

                // VÁ LỖI BCRYPT: Tự động sửa lại mật khẩu mẫu trong database
                if (!string.IsNullOrEmpty(user.PasswordHash) && !user.PasswordHash.StartsWith("$"))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    await _context.SaveChangesAsync();
                }

                // KIỂM TRA MẬT KHẨU AN TOÀN (Tránh bị văng lỗi SaltParseException)
                bool isPasswordValid = false;
                try
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                }
                catch
                {
                    isPasswordValid = false;
                }

                if (isPasswordValid)
                {
                    if (user.Status != true)
                    {
                        ViewBag.Error = "Tài khoản của bạn đã bị khóa hoặc chưa kích hoạt!";
                        return View();
                    }

                    HttpContext.Session.SetString("UserId", $"{user.UserId}");
                    HttpContext.Session.SetString("UserName", user.FullName);
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserRole", user.Role.RoleName);

                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Error = "Email hoặc mật khẩu không chính xác!";
            return View();
        }

        // ==========================================
        // 4. ĐĂNG XUẤT
        // ==========================================
        [HttpGet]
        public IActionResult Logout()
        {
            // Xóa toàn bộ phiên làm việc
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
        // ==========================================
        // 5. QUẢN LÝ HỒ SƠ CÁ NHÂN (PROFILE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            int userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(User model)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            int userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);

            if (user != null)
            {
                // Cập nhật các thông tin từ form gửi lên
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Email = model.Email;
                user.TinhTp = model.TinhTp;
                user.QuanHuyen = model.QuanHuyen;
                user.PhuongXa = model.PhuongXa;
                user.DiaChiCuThe = model.DiaChiCuThe;
                user.BankName = model.BankName;
                user.BankAccountNumber = model.BankAccountNumber;

                _context.Update(user);
                await _context.SaveChangesAsync();

                // Cập nhật lại tên hiển thị trên Header
                HttpContext.Session.SetString("UserName", user.FullName);
                TempData["Success"] = "Đã lưu thay đổi thông tin cá nhân!";
            }
            return RedirectToAction("Profile");
        }
        // ==========================================
        // 6. ĐỔI MẬT KHẨU
        // ==========================================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            if (!ModelState.IsValid) return View(model);

            int userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // Kiểm tra mật khẩu cũ
            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(model.OldPassword, user.PasswordHash);
            }
            catch
            {
                // Fallback: Trong trường hợp dữ liệu mẫu (DML) của bạn đang là mật khẩu chưa băm (chuỗi thường)
                if (user.PasswordHash == model.OldPassword) isPasswordValid = true;
            }

            if (!isPasswordValid)
            {
                ViewBag.Error = "Mật khẩu hiện tại không chính xác!";
                return View(model);
            }

            // Mã hóa mật khẩu mới và lưu vào DB
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công! Hãy sử dụng mật khẩu mới cho lần đăng nhập sau.";
            return RedirectToAction("Profile");
        }
    }
}