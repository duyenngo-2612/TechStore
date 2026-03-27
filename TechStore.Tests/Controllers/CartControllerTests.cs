using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechStore.Controllers;
using TechStore.Models;
using Xunit;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks; // Thêm dòng này để dùng Task

namespace TechStore.Tests.Controllers
{
    public class CartControllerTests
    {
        private readonly Mock<ISession> _mockSession;
        private readonly CartController _controller;

        public CartControllerTests()
        {
            _mockSession = new Mock<ISession>();
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(s => s.Session).Returns(_mockSession.Object);

            // Truyền tham số context nếu cần, ở đây giả định dùng null! cho đơn giản
            _controller = new CartController(null!)
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
            // Arrange
            byte[]? outValue = null;
            _mockSession.Setup(s => s.TryGetValue("UserId", out outValue)).Returns(false);

            // Act: Thêm await ở phía trước để đợi hàm chạy xong
            var result = await _controller.PlaceOrder("COD", "Giao nhanh", "TP.HCM") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Account", result.ControllerName);
        }
    }
}