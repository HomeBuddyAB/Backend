using System;
using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models 
{ 
    public class InventoryTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // FK must be Guid to match Inventory.Id
        public Guid InventoryId { get; set; }

        // Navigation (nullable allowed to ease materialization in some cases)
        public Inventory? Inventory { get; set; }

        public InventoryTransactionType TransactionType { get; set; }
        public int QuantityChange { get; set; }
        public int? ResultingQuantity { get; set; }
        public string? ReferenceId { get; set; }

        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}