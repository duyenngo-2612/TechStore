using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;
using TechStore.Models;

namespace TechStore.Tests
{
    public class OrderTest
    {
        [Fact]
        public void Order_Invalid_When_Missing_Required_Fields()
        {
            // 1. Arrange: Tạo Order thiếu dữ liệu (UserId = 0, Địa chỉ trống)
            var order = new Order()
            {
                UserId = 0,
                ShippingAddress = ""
            };

            var context = new ValidationContext(order);
            var results = new List<ValidationResult>();

            // 2. Act: Ép hệ thống kiểm tra tính hợp lệ
            bool isValid = Validator.TryValidateObject(order, context, results, true);

            // 3. Assert: Phải trả về FALSE (vì dữ liệu đang sai)
            Assert.False(isValid);
        }
    }

}