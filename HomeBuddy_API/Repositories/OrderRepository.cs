using HomeBuddy_API.Data;
using HomeBuddy_API.Extensions;
using HomeBuddy_API.Interfaces.OrderInterfaces;
using HomeBuddy_API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task<IEnumerable<Order>> GetAllAsync(int page)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .Paginate(page)
                .ToListAsync();
        }

        public async Task<int> GetOrdersCountAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order?> GetOrderByOrderNoAsync(string orderNr)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderNo == orderNr);
        }

        public async Task<IEnumerable<Order>?> GetOrderByEmailAsync(string email)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Email == email)
                .ToListAsync();
        }

        public async Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            await _context.Orders
                .Where(o => o.Id == id)
                .ExecuteDeleteAsync();
        }
    }
}
