using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models
{
    public class Inventory
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Must match Variant.Id (Guid)
        public Guid VariantId { get; set; }
        public Variant? Variant { get; set; }

        // On-hand quantity
        public int Quantity { get; set; }

        public int LowStockThreshold { get; set; }
        public DateTimeOffset? LastRestockDate { get; set; }

        // Concurrency token for optimistic concurrency
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation collection — required for explicit mapping
        public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    }
}
