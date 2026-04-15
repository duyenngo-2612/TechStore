using TechStore.Models;

namespace TechStore.Services
{
    public interface ICartService
    {
        // Quản lý số lượng và đồng bộ
        Task<int> GetCartCountAsync(int? userId);
        Task MergeSessionCartToDbAsync(int userId);

        // Các thao tác chính
        Task<List<Cart>> GetCartItemsAsync(int? userId);
        Task AddToCartAsync(int productId, int quantity, int? userId);
        Task UpdateQuantityAsync(int productId, int change, int? userId);
        Task RemoveFromCartAsync(int productId, int? userId);
    }
}