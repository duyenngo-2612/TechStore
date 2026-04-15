using Microsoft.EntityFrameworkCore;
using TechStore.Models;
using TechStore.Extensions;
using Microsoft.AspNetCore.Http;

namespace TechStore.Services
{
    public class CartService : ICartService
    {
        private readonly TechStoreContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(TechStoreContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext.Session;

        public async Task<int> GetCartCountAsync(int? userId)
        {
            int count = 0;
            if (userId.HasValue) // Đã đăng nhập
            {
                count = await _context.Carts.Where(c => c.UserId == userId.Value).SumAsync(c => (int?)c.Quantity) ?? 0;
            }
            else // Chưa đăng nhập
            {
                var sessionCart = Session.GetObjectFromJson<List<CartItemSession>>("GuestCart");
                if (sessionCart != null) count = sessionCart.Sum(c => c.Quantity);
            }

            Session.SetString("CartCount", count.ToString());
            return count;
        }

        public async Task MergeSessionCartToDbAsync(int userId)
        {
            var sessionCart = Session.GetObjectFromJson<List<CartItemSession>>("GuestCart");
            if (sessionCart != null && sessionCart.Any())
            {
                foreach (var item in sessionCart)
                {
                    var dbItem = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == item.ProductId);
                    if (dbItem != null)
                    {
                        dbItem.Quantity += item.Quantity;
                    }
                    else
                    {
                        _context.Carts.Add(new Cart { UserId = userId, ProductId = item.ProductId, Quantity = item.Quantity });
                    }
                }
                await _context.SaveChangesAsync();
                Session.Remove("GuestCart");
            }
        }

        public async Task<List<Cart>> GetCartItemsAsync(int? userId)
        {
            if (userId.HasValue)
            {
                await MergeSessionCartToDbAsync(userId.Value);
                return await _context.Carts.Include(c => c.Product).Where(c => c.UserId == userId.Value).ToListAsync();
            }
            else
            {
                var sessionCart = Session.GetObjectFromJson<List<CartItemSession>>("GuestCart") ?? new List<CartItemSession>();
                var viewCart = new List<Cart>();
                foreach (var item in sessionCart)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        viewCart.Add(new Cart { ProductId = item.ProductId, Quantity = item.Quantity, Product = product });
                    }
                }
                return viewCart;
            }
        }

        public async Task AddToCartAsync(int productId, int quantity, int? userId)
        {
            if (userId.HasValue)
            {
                var item = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == productId);
                if (item != null) item.Quantity += quantity;
                else _context.Carts.Add(new Cart { UserId = userId.Value, ProductId = productId, Quantity = quantity });

                await _context.SaveChangesAsync();
            }
            else
            {
                var sessionCart = Session.GetObjectFromJson<List<CartItemSession>>("GuestCart") ?? new List<CartItemSession>();
                var item = sessionCart.FirstOrDefault(c => c.ProductId == productId);

                if (item != null) item.Quantity += quantity;
                else sessionCart.Add(new CartItemSession { ProductId = productId, Quantity = quantity });

                Session.SetObjectAsJson("GuestCart", sessionCart);
            }
        }

        // Tương tự, cắt code UpdateQuantity và Remove từ Controller mang sang đây
        public async Task UpdateQuantityAsync(int productId, int change, int? userId) { /* Code UpdateQuantity */ }
        public async Task RemoveFromCartAsync(int productId, int? userId) { /* Code Remove */ }
    }
}