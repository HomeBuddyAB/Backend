using HomeBuddy_API.Models;

namespace HomeBuddy_API.Interfaces.OrderInterfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int id);
        Task<IEnumerable<Order>?> GetOrderByEmailAsync(string email);
        Task<Order?> GetOrderByOrderNoAsync(string orderNr);
        Task<IEnumerable<Order>> GetAllAsync(int page);
        Task<int> GetOrdersCountAsync();
        Task CreateAsync(Order order);
        Task UpdateAsync(Order order);
        Task DeleteAsync(int id);
    }
}
