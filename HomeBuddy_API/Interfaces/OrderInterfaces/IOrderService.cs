using HomeBuddy_API.DTOs.Requests.OrderDTOs;
using HomeBuddy_API.Models;

namespace HomeBuddy_API.Interfaces.OrderInterfaces
{
    public interface IOrderService
    {
        Task<Order?> GetOrderAsync(int id);
        Task<IEnumerable<Order>?> GetOrderByOrderEmailAsync(string email);
        Task<Order?> GetOrderByOrderNoAsync(string orderNr);
        Task<IEnumerable<Order>> GetAllOrdersAsync(int page);
        Task<int> GetOrdersCountAsync();
        Task<string> CreateOrderAsync(OrderCreateDto dto, int? userId = null);
        /// <summary>Link an existing order (by order number) to the current user. Order email must match user email.</summary>
        Task ClaimOrderAsync(string orderNo, int userId, string userEmail);
        Task UpdateOrderAsync(int id, OrderUpdateDto dto);
        Task DeleteOrderAsync(int id);
    }
}
