using System.Text.Json;

namespace TechStore.Extensions
{
    // Class hỗ trợ lưu Object vào Session
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }

    // Class đại diện cho 1 món hàng lưu tạm trong Session khi chưa đăng nhập
    public class CartItemSession
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}