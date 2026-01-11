// HomeBuddy_API/Repositories/InventoryTransactionRepository.cs
using HomeBuddy_API.Data; // adjust as needed
using HomeBuddy_API.Interfaces.InventoryInterfaces;
using HomeBuddy_API.Models;
using System.Threading;
using System.Threading.Tasks;

namespace HomeBuddy_API.Repositories
{
    public class InventoryTransactionRepository : IInventoryTransactionRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public InventoryTransactionRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(InventoryTransaction tx, CancellationToken ct = default)
        {
            await _dbContext.InventoryTransactions.AddAsync(tx, ct);
        }
    }
}
