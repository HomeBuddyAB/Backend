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
        Task CreateOrderAsync(OrderCreateDto dto);
        Task UpdateOrderAsync(int id, OrderUpdateDto dto);
        Task DeleteOrderAsync(int id);
    }
}
