using TechStore.Models;
using TechStore.DTOs;

namespace TechStore.Services
{
    public interface IOrderService
    {
        // Cho Khách hàng
        Task<List<Order>> GetCustomerOrdersAsync(int userId, int? status);
        Task<Order> GetOrderDetailsAsync(int orderId, int userId);
        Task<CheckoutDataDto> GetCheckoutDataAsync(int userId, string voucherCode);
        Task<PlaceOrderResultDto> PlaceOrderAsync(int userId, string address, string paymentMethod, int? voucherId);
        Task<bool> CancelOrderAsync(int orderId, int userId);

        // Cho Admin
        Task<List<Order>> GetAllOrdersForAdminAsync();
        Task<bool> ConfirmOrderAsync(int orderId);
    }
}