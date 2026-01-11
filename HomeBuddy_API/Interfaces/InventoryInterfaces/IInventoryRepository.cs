// HomeBuddy_API/Interfaces/InventoryInterfaces/IInventoryRepository.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using HomeBuddy_API.Models;

namespace HomeBuddy_API.Interfaces.InventoryInterfaces 
{ 
	public interface IInventoryRepository
	{
		Task<Inventory?> GetByVariantIdAsync(Guid variantId, CancellationToken ct = default);
		Task AddAsync(Inventory inventory, CancellationToken ct = default);
		void Update(Inventory inventory);
    }
}
