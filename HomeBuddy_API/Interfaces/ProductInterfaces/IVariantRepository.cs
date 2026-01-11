// HomeBuddy_API/Interfaces/ProductInterfaces/IVariantRepository.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HomeBuddy_API.Models;

namespace HomeBuddy_API.Interfaces.ProductInterfaces
{
    public interface IVariantRepository
    {
        Task<Variant?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Variant?> GetBySkuAsync(string sku, CancellationToken ct = default);
        Task<List<Variant>> ListByGroupAsync(Guid productGroupId, CancellationToken ct = default);

        // (Optional helpers if you need them elsewhere)
        Task<Variant?> GetBySkuIncludingDeletedAsync(string sku, CancellationToken ct = default);
        Task<List<Variant>> ListByGroupIncludingDeletedAsync(Guid productGroupId, CancellationToken ct = default);
    }
}
