using Microsoft.EntityFrameworkCore;
using TechStore.Models;
using TechStore.Services;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// PHẦN 1: ĐĂNG KÝ DỊCH VỤ (SERVICES)
// Bắt buộc phải nằm TRƯỚC lệnh builder.Build()
// ==========================================

// 1. Đăng ký DbContext
builder.Services.AddDbContext<TechStoreContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Thêm các dịch vụ MVC
builder.Services.AddControllersWithViews();

// 3. Đăng ký Session và HttpContextAccessor
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

// 4. Đăng ký Email Service
builder.Services.AddScoped<IEmailService, EmailSender>();

// 5. Đăng ký Authentication (Xác thực đăng nhập)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "CookieAuth";
    options.DefaultSignInScheme = "CookieAuth";
    options.DefaultChallengeScheme = "CookieAuth";
})
.AddCookie("CookieAuth", options =>
{
    options.LoginPath = "/Account/Login"; // Chuyển hướng khi chưa đăng nhập
    options.AccessDeniedPath = "/Account/AccessDenied"; // Chuyển hướng khi không đủ quyền
});

// Khởi tạo ứng dụng sau khi đã đăng ký xong tất cả Services
var app = builder.Build();

// ==========================================
// PHẦN 2: CẤU HÌNH PIPELINE (MIDDLEWARE)
// Bắt buộc nằm SAU lệnh builder.Build() - Thứ tự rất quan trọng
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 1. Routing phải chạy trước để biết request đi đâu
app.UseRouting();

// 2. Kích hoạt Session
app.UseSession();

// 3. Xác thực (Hệ thống hỏi: Bạn là ai?) - Phải nằm TRƯỚC Authorization
app.UseAuthentication();

// 4. Phân quyền (Hệ thống hỏi: Bạn có quyền vào đây không?)
app.UseAuthorization();

// 5. Cấu hình Routing cho Area Admin (BẮT BUỘC đặt trên route default)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// 6. Cấu hình Routing mặc định cho khu vực khách hàng
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Chạy ứng dụng
app.Run();
public partial class Program { }