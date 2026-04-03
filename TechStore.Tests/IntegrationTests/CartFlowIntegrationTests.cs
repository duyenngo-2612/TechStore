using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using TechStore.Models;
using Xunit;

namespace TechStore.Tests.IntegrationTests
{
    // 1. Class giả lập Authentication
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, "1"), // Ép ID = 1
                new Claim(ClaimTypes.Name, "AnNhienTestUser")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestAuth");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    // 2. Class Test chính
    public class CartFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public CartFlowIntegrationTests(WebApplicationFactory<Program> factory)
        {
            // BƯỚC QUYẾT ĐỊNH: Ép hệ thống gỡ SQL Server thật và dùng DB Ảo
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // 1. Tìm và xóa cấu hình SQL Server thật
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TechStoreContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // 2. Thay thế bằng DB Ảo
                    services.AddDbContext<TechStoreContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDB_TechStore");
                    });
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        [Fact]
        public async Task TC07_PlaceOrder_WithoutLogin_RedirectsToLogin()
        {
            var formData = new Dictionary<string, string> { { "paymentMethod", "COD" } };
            var content = new FormUrlEncodedContent(formData);
            var response = await _client.PostAsync("/Cart/PlaceOrder", content);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task TC08_PlaceOrder_COD_WithValidSession_RedirectsToOrderSuccess()
        {
            var authenticatedClient = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication()
                            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestAuth", options => { });
                    services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
                    {
                        options.DefaultAuthenticateScheme = "TestAuth";
                        options.DefaultChallengeScheme = "TestAuth";
                        options.DefaultScheme = "TestAuth";
                    });
                });
            }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            authenticatedClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("TestAuth");

            // NẠP DATA VÀO DATABASE ẢO
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TechStoreContext>();

                db.Database.EnsureDeleted(); // Xóa sạch rác cũ
                db.Database.EnsureCreated(); // Tạo DB ảo mới tinh

                // Lưu ý: Đảm bảo class MockProducts của bạn có namespace đúng
                db.Products.AddRange(TechStore.Tests.MockData.MockProducts.GetProducts());
                db.SaveChanges(); // Lưu Product trước

                db.Carts.Add(new Cart { UserId = 1, ProductId = 1, Quantity = 1 });
                db.SaveChanges(); // Lưu Cart sau
            }

            var formData = new Dictionary<string, string>
            {
                { "shippingAddress", "123 Đường Test, Quận 1" },
                { "paymentMethod", "COD" },
                { "note", "Giao hàng nhanh nhé" }
            };
            var content = new FormUrlEncodedContent(formData);

            var response = await authenticatedClient.PostAsync("/Cart/PlaceOrder", content);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Cart/OrderSuccess", response.Headers.Location?.OriginalString);
        }
    }
}