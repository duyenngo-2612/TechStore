using System.Collections.Generic;
using TechStore.Models;

namespace TechStore.Tests.MockData
{
    public static class MockProducts
    {
        public static List<Product> GetProducts()
        {
            return new List<Product>
            {
                new Product { ProductId = 1, ProductName = "Laptop", Price = 1000, StockQuantity = 10 },
                new Product { ProductId = 2, ProductName = "Mouse", Price = 50, StockQuantity = 20 },
                new Product { ProductId = 3, ProductName = "Keyboard", Price = 100, StockQuantity = 15 }
            };
        }
    }
}