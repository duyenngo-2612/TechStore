using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Xunit;

namespace TechStore.Tests.IntegrationTests
{
    // 1. Class giả lập Authentication, giúp Client có sẵn trạng thái "Đã đăng nhập"
    // Nó sẽ bỏ qua bước check DB và trực tiếp cấp quyền "CookieAuth" cho trình duyệt ảo.
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, "TestUserId_123"),
                new Claim(ClaimTypes.Name, "AnNhienTestUser")
            };

            // Tên Scheme "CookieAuth" khớp chính xác với thiết lập trong Program.cs của TechStore
            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "CookieAuth");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    // 2. Class Test chính (Đã đổi thành public)
    public class CartFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public CartFlowIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;

            // Khởi tạo client mặc định cho các test case không cần đăng nhập (như TC07)
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false // Tắt auto redirect để bắt được mã 302
            });
        }

        [Fact]
        public async Task TC07_PlaceOrder_WithoutLogin_RedirectsToLogin()
        {
            // Arrange
            var formData = new Dictionary<string, string>
            {
                { "paymentMethod", "COD" }
            };
            var content = new FormUrlEncodedContent(formData);

            // Act: Gửi request khi chưa xác thực
            var response = await _client.PostAsync("/Cart/PlaceOrder", content);

            // Assert: Kiểm tra bị chặn và đẩy về trang Login
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task TC08_PlaceOrder_COD_WithValidSession_RedirectsToOrderSuccess()
        {
            // Arrange: Tạo một client ĐÃ ĐƯỢC XÁC THỰC thông qua TestAuthHandler
            var authenticatedClient = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Ép hệ thống dùng TestAuthHandler thay vì logic Cookie thật
                    services.AddAuthentication("CookieAuth")
                            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("CookieAuth", options => { });
                });
            }).CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var formData = new Dictionary<string, string>
            {
                { "paymentMethod", "COD" }
            };
            var content = new FormUrlEncodedContent(formData);

            // Act: Gửi request chốt đơn
            var response = await authenticatedClient.PostAsync("/Cart/PlaceOrder", content);

            // Assert: Thành công và được chuyển hướng tới trang OrderSuccess
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Cart/OrderSuccess", response.Headers.Location?.OriginalString);
        }
    }
}