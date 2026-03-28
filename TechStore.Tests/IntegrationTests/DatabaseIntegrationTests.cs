using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TechStore.Models;
using TechStore.Tests.IntegrationSetup;
using Xunit;

namespace TechStore.Tests
{
    public class DatabaseIntegrationTests
    {
        private readonly TechStoreContext _context;

        public DatabaseIntegrationTests()
        {
            // Tạo context dùng InMemory (giống môi trường test)
            var options = new DbContextOptionsBuilder<TechStoreContext>()
                .UseInMemoryDatabase("TechStoreTestDb")
                .Options;

            _context = new TechStoreContext(options);
        }

        [Fact]
        public async Task TC06_Should_Save_Order_To_Database()
        {
            // Arrange
            var order = new Order
            {

                // Nếu model của bạn có các field khác thì thêm vào đây
                OrderDate = System.DateTime.Now,
                OrderDetails = new System.Collections.Generic.List<OrderDetail>
                {
                    new OrderDetail
                    {
                        ProductId = 1,
                        Quantity = 2,
                        UnitPrice = 100000
                    }
                },
                ShippingAddress = "123 Đường ABC, TP.HCM"
            };

            // Act
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // Assert
            var count = _context.Orders.Count();
            Assert.Equal(1, count);
        }
    }
}