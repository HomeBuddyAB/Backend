// HomeBuddy_API/Interfaces/InventoryInterfaces/IInventoryTransactionRepository.cs
using System.Threading;
using System.Threading.Tasks;
using HomeBuddy_API.Models;

namespace HomeBuddy_API.Interfaces.InventoryInterfaces
{
    public interface IInventoryTransactionRepository
    {
        Task AddAsync(InventoryTransaction tx, CancellationToken ct = default);
    }
}
