// HomeBuddy_API/Services/InventoryService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using HomeBuddy_API.Data;
using HomeBuddy_API.Models;
using HomeBuddy_API.Interfaces.InventoryInterfaces;
using HomeBuddy_API.Exceptions;

namespace HomeBuddy_API.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _db;
        private const int MaxConcurrencyRetries = 3;

        public InventoryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AdjustInventoryAsync(string sku, int delta, InventoryTransactionType type, string? referenceId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("SKU is required.", nameof(sku));

            var skuNormalized = sku.Trim().ToUpperInvariant();

            var variant = await _db.Variants
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(v => v.Sku == skuNormalized, ct);

            if (variant == null)
                throw new NotFoundException("Variant", skuNormalized);

            await AdjustInventoryAsync(variant.Id, delta, type, referenceId, ct);
        }

        public async Task AdjustInventoryAsync(Guid variantId, int delta, InventoryTransactionType type, string? referenceId = null, CancellationToken ct = default)
        {
            if (variantId == Guid.Empty)
                throw new ArgumentException("variantId is required.", nameof(variantId));

            if (delta < 0)
            {
                IDbContextTransaction? localTx = null;
                try
                {
                    // Only open a local transaction if there is no ambient one
                    if (_db.Database.CurrentTransaction == null)
                        localTx = await _db.Database.BeginTransactionAsync(ct);

                    // Use EF Core ExecuteUpdateAsync to perform an atomic conditional update in a provider-agnostic way.
                    var rowsAffected = await _db.Inventories
                        .Where(i => i.VariantId == variantId && (i.Quantity + delta) >= 0)
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(i => i.Quantity, i => i.Quantity + delta),
                            ct);

                    if (rowsAffected == 0)
                    {
                        if (localTx != null)
                            await localTx.RollbackAsync(ct);

                        var inv = await _db.Inventories
                                           .AsNoTracking()
                                           .FirstOrDefaultAsync(i => i.VariantId == variantId, ct);

                        if (inv == null)
                            throw new NotFoundException("Inventory", variantId.ToString());

                        // Throw typed exception with details
                        throw new InsufficientStockException(variantId.ToString(), Math.Abs(delta), inv.Quantity);
                    }

                    // Read resulting quantity (within same transaction/connection)
                    var inventory = await _db.Inventories
                                             .AsNoTracking()
                                             .FirstOrDefaultAsync(i => i.VariantId == variantId, ct);

                    if (inventory == null)
                    {
                        if (localTx != null)
                            await localTx.RollbackAsync(ct);
                        throw new NotFoundException("Inventory", variantId.ToString());
                    }

                    var invTx = new InventoryTransaction
                    {
                        Id = Guid.NewGuid(),
                        InventoryId = inventory.Id,
                        TransactionType = type,
                        QuantityChange = delta,
                        ResultingQuantity = inventory.Quantity,
                        ReferenceId = referenceId,
                        Timestamp = DateTimeOffset.UtcNow
                    };

                    _db.InventoryTransactions.Add(invTx);
                    await _db.SaveChangesAsync(ct);

                    if (localTx != null)
                        await localTx.CommitAsync(ct);

                    return;
                }
                finally
                {
                    if (localTx != null)
                        await localTx.DisposeAsync();
                }
            }

            // Positive adjustments: tracked + optimistic retries
            for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
            {
                var inventory = await _db.Inventories
                                         .FirstOrDefaultAsync(i => i.VariantId == variantId, ct);

                if (inventory == null)
                    throw new NotFoundException("Inventory", variantId.ToString());

                // Defensive check for negative result (shouldn't be hit for positive-delta path)
                if (delta < 0 && inventory.Quantity + delta < 0)
                    throw new InsufficientStockException(variantId.ToString(), Math.Abs(delta), inventory.Quantity);

                inventory.Quantity += delta;

                var txEntity = new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    InventoryId = inventory.Id,
                    TransactionType = type,
                    QuantityChange = delta,
                    ResultingQuantity = inventory.Quantity,
                    ReferenceId = referenceId,
                    Timestamp = DateTimeOffset.UtcNow
                };

                _db.InventoryTransactions.Add(txEntity);

                try
                {
                    await _db.SaveChangesAsync(ct);
                    return;
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (attempt == MaxConcurrencyRetries - 1)
                        throw;

                    var entry = _db.Entry(inventory);
                    if (entry != null)
                        await entry.ReloadAsync(ct);

                    foreach (var e in _db.ChangeTracker.Entries<InventoryTransaction>())
                    {
                        if (e.Entity.InventoryId == inventory.Id && e.Entity.ReferenceId == referenceId)
                            e.State = EntityState.Detached;
                    }
                }
            }
        }

        public async Task<int> GetGroupQuantityAsync(Guid productGroupId, CancellationToken ct = default)
        {
            var total = await _db.Inventories
                .Where(i => _db.Variants.Any(v => v.Id == i.VariantId && v.ProductGroupId == productGroupId))
                .SumAsync(i => i.Quantity, ct);

            return total;
        }

        public async Task<int> GetSkuQuantityAsync(string sku, CancellationToken ct = default)
        {
            var skuNormalized = sku.Trim().ToUpperInvariant();
            var variant = await _db.Variants
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Sku == skuNormalized, ct);

            if (variant == null)
                throw new NotFoundException("Variant", skuNormalized);

            var inventory = await _db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.VariantId == variant.Id, ct);

            if (inventory == null)
                throw new NotFoundException("Inventory", skuNormalized);

            return inventory.Quantity;
        }

        /// <summary>
        /// Decrement inventory for an order (creates an InventoryTransaction referencing the order).
        /// Throws NotFoundException or InsufficientStockException.
        /// Uses RowVersion to detect concurrent updates.
        /// </summary>
        public async Task DecrementForOrderAsync(Guid inventoryId, int quantity, Guid orderId)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be positive", nameof(quantity));

            // Load current inventory row (includes RowVersion due to model)
            var inventory = await _db.Inventories
                .SingleOrDefaultAsync(i => i.Id == inventoryId);

            if (inventory == null)
                throw new NotFoundException("Inventory", inventoryId.ToString());

            if (inventory.Quantity < quantity)
                throw new InsufficientStockException(inventoryId.ToString(), inventory.Quantity, quantity);

            // Apply decrement
            inventory.Quantity -= quantity;

            // Create transaction trace. ReferenceId is the order id (string) so it matches migration type.
            var itx = new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                InventoryId = inventoryId,
                TransactionType = InventoryTransactionType.Order, // adjust enum/value to your model
                QuantityChange = -quantity,
                ResultingQuantity = inventory.Quantity,
                ReferenceId = orderId.ToString(),
                Timestamp = DateTimeOffset.UtcNow
            };
            _db.InventoryTransactions.Add(itx);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Someone else modified the inventory concurrently. Re-check available quantity or surface as insufficient.
                throw new InsufficientStockException(inventoryId.ToString(), quantity, inventory.Quantity);
            }
        }
    }
}









