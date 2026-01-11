// HomeBuddy_API/Interfaces/InventoryInterfaces/IInventoryService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using HomeBuddy_API.Models;

namespace HomeBuddy_API.Interfaces.InventoryInterfaces
{
    public interface IInventoryService
    {
        /// Gets total quantity across all SKUs in a product group.
        Task<int> GetGroupQuantityAsync(Guid productGroupId, CancellationToken ct = default);

        /// Gets available units for a specific SKU.
        Task<int> GetSkuQuantityAsync(string sku, CancellationToken ct = default);

        /// Adjusts inventory for a SKU and logs a transaction.
        /// <param name="sku">The SKU to adjust (variant-level).</param>
        /// <param name="delta">Positive to add stock, negative to subtract.</param>
        /// <param name="type">Reason for the adjustment (Restock, Sale, Adjustment).</param>
        /// <param name="referenceId">Optional reference (order number, shipment ID, etc.).</param>
        Task AdjustInventoryAsync(string sku, int delta, InventoryTransactionType type, string? referenceId = null, CancellationToken ct = default);

        /// Adjusts inventory by VariantId and logs a transaction.
        /// Prefer this overload when you already have the VariantId to avoid extra SKU lookups.
        Task AdjustInventoryAsync(Guid variantId, int delta, InventoryTransactionType type, string? referenceId = null, CancellationToken ct = default);
    }
}
