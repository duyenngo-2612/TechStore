using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechStore.Controllers;
using TechStore.Models;
using TechStore.Services; 
using Xunit;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TechStore.Tests.Controllers
{
    public class CartControllerTests
    {
        private readonly Mock<ISession> _mockSession;
        private readonly Mock<ICartService> _mockCartService; // [BỔ SUNG] Khai báo Mock Service
        private readonly CartController _controller;

        public CartControllerTests()
        {
            _mockSession = new Mock<ISession>();

            // [BỔ SUNG] Khởi tạo bản sao ảo của Service
            _mockCartService = new Mock<ICartService>();

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(s => s.Session).Returns(_mockSession.Object);

            // [SỬA LỖI Ở ĐÂY] Truyền đủ 2 tham số: mockCartService.Object và null (cho context)
            _controller = new CartController(_mockCartService.Object, null!)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = mockHttpContext.Object
                }
            };
        }

        [Fact]
        public async Task TC03_PlaceOrder_WhenNoSession_ShouldRedirectToLogin()
        {
            // Arrange: Cài đặt Session giả trả về false (không có UserId)
            byte[]? outValue = null;
            _mockSession.Setup(s => s.TryGetValue("UserId", out outValue)).Returns(false);

            // Tạo HttpContext giả và nhét Session giả vào
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            httpContext.Session = _mockSession.Object;

            // Ép Controller phải sử dụng HttpContext giả này
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = httpContext
            };

            // Act: Gửi request chốt đơn
            var result = await _controller.PlaceOrder("COD", "Giao nhanh", "TP.HCM") as Microsoft.AspNetCore.Mvc.RedirectToActionResult;

            // Assert: Kỳ vọng bị đá về trang Login
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Account", result.ControllerName);
        }
    }
}