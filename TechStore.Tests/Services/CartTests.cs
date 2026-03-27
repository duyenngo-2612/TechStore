using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using TechStore.Models;
using TechStore.Tests.MockData;

namespace TechStore.Tests.Services
{
    public class CartTests
    {
        [Fact]
        public void TC01_Should_Calculate_Total_Using_LINQ()
        {
            // Arrange
            var products = MockProducts.GetProducts();

            var carts = new List<Cart>
            {
                new Cart { Product = products[0], Quantity = 1 }, // Laptop: 1000
                new Cart { Product = products[1], Quantity = 2 }, // Mouse: 50 * 2 = 100
                new Cart { Product = products[2], Quantity = 3 }  // Keyboard: 100 * 3 = 300
            };

            // Act (LINQ giống Controller)
            var total = carts.Sum(x => x.Product.Price * x.Quantity);

            // Assert
            var expected = 1400;

            Assert.Equal(expected, total);
        }
        [Fact]
        public void TC02_Should_Remove_Product_When_Quantity_Less_Than_Or_Equal_Zero()
        {
            // Arrange
            var products = MockProducts.GetProducts();

            var carts = new List<Cart>
    {
        new Cart { Product = products[0], Quantity = 1 } // Laptop
    };

            // Act: giảm số lượng = -2 → tổng = -1
            var item = carts.First();

            item.Quantity += -2;

            // Nếu <= 0 thì remove khỏi giỏ
            if (item.Quantity <= 0)
            {
                carts.Remove(item);
            }

            // Assert: kiểm tra giỏ hàng không còn sản phẩm
            Assert.Empty(carts);
        }
    }
}