using System;

namespace HomeBuddy_API.Exceptions
{
    public class InsufficientStockException : Exception
    {
        public string? SkuOrVariantId { get; }
        public int Requested { get; }
        public int Available { get; }

        public InsufficientStockException(string message) : base(message) { }

        public InsufficientStockException(string skuOrVariantId, int requested, int available)
            : base($"Insufficient stock for '{skuOrVariantId}'. Requested: {requested}, Available: {available}")
        {
            SkuOrVariantId = skuOrVariantId;
            Requested = requested;
            Available = available;
        }
    }
}