using HomeBuddy_API.DTOs.Requests.OrderDTOs;
using HomeBuddy_API.Exceptions;
using HomeBuddy_API.Interfaces;
using HomeBuddy_API.Interfaces.InventoryInterfaces;
using HomeBuddy_API.Interfaces.OrderInterfaces;
using HomeBuddy_API.Interfaces.ProductInterfaces;
using HomeBuddy_API.Models;

namespace HomeBuddy_API.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IInventoryService _inventoryService;
        private readonly IVariantRepository _variantRepository;
        private readonly IUnitOfWork _uow;

        public OrderService(
            IOrderRepository orderRepo,
            IInventoryService inventoryService,
            IVariantRepository variantRepository,
            IUnitOfWork uow)
        {
            _orderRepo = orderRepo;
            _inventoryService = inventoryService;
            _variantRepository = variantRepository;
            _uow = uow;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync(int page)
        {
            return await _orderRepo.GetAllAsync(page);
        }

        public async Task<int> GetOrdersCountAsync()
        {
            return await _orderRepo.GetOrdersCountAsync();
        }

        public async Task<Order?> GetOrderAsync(int id)
        {
            return await _orderRepo.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Order>?> GetOrderByOrderEmailAsync(string email)
        {
            return await _orderRepo.GetOrderByEmailAsync(email);
        }

        public async Task<Order?> GetOrderByOrderNoAsync(string orderNo)
        {
            return await _orderRepo.GetOrderByOrderNoAsync(orderNo);
        }

        public async Task CreateOrderAsync(OrderCreateDto dto)
        {
            // Generate a unique order number and trim to 45 characters.
            var orderNo = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
            orderNo = orderNo[..Math.Min(45, orderNo.Length)];

            decimal total = 0m;
            var orderItems = new List<OrderItem>();

            await _uow.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var item in dto.Items)
                {
                    // Normalize SKU input to a canonical form before lookup.
                    var skuNormalized = item.Sku?.Trim().ToUpperInvariant()
                        ?? throw new InvalidOperationException("SKU required.");

                    // Look up the variant by normalized SKU (pass the cancellation token).
                    var variant = await _variantRepository.GetBySkuAsync(skuNormalized, ct);
                    if (variant == null)
                        throw new NotFoundException("Variant", skuNormalized); ;

                    // Price
                    var unitPrice = variant.Price;
                    total += unitPrice * item.Quantity;

                    // Decrement inventory and record a sale transaction using VariantId (no redundant SKU lookup).
                    await _inventoryService.AdjustInventoryAsync(
                        variant.Id,
                        -item.Quantity,
                        InventoryTransactionType.Sale,
                        referenceId: orderNo,
                        ct);

                    // Build the order item entity with the resolved price.
                    orderItems.Add(new OrderItem
                    {
                        VariantId = variant.Id,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice
                    });
                }

                var order = new Order
                {
                    OrderNo = orderNo,
                    Email = dto.Email,
                    Status = "Pending",
                    Total = total,
                    CreatedUtc = DateTime.UtcNow,
                    Items = orderItems
                };

                await _orderRepo.CreateAsync(order);

                // Single commit for both inventory + order
                await _uow.SaveChangesAsync(ct);
            });
        }

        public async Task UpdateOrderAsync(int id, OrderUpdateDto dto)
        {
            await _uow.ExecuteInTransactionAsync(async ct =>
            {
                var existingOrder = await _orderRepo.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException("Order not found.");

                // If changing to Cancelled from a non-cancelled state, restock items.
                if (!string.IsNullOrWhiteSpace(dto.Status)
                    && dto.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(existingOrder.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    // Restock all items that were decremented on creation.
                    foreach (var item in existingOrder.Items)
                    {
                        if (item.VariantId.HasValue)
                        {
                            // Use VariantId overload directly to avoid extra SKU lookups.
                            await _inventoryService.AdjustInventoryAsync(
                                item.VariantId.Value,
                                +item.Quantity,
                                InventoryTransactionType.Adjustment,
                                referenceId: existingOrder.OrderNo,
                                ct);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(dto.Status))
                    existingOrder.Status = dto.Status;

                if (dto.Total.HasValue)
                    existingOrder.Total = dto.Total.Value;

                await _orderRepo.UpdateAsync(existingOrder);
                await _uow.SaveChangesAsync(ct);
            });
        }

        public async Task DeleteOrderAsync(int id)
        {
            await _uow.ExecuteInTransactionAsync(async ct =>
            {
                var existingOrder = await _orderRepo.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException("Order not found.");

                // Optional: if you want to restock when deleting a non-shipped order
                if (!string.Equals(existingOrder.Status, "Shipped", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(existingOrder.Status, "Delivered", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var item in existingOrder.Items)
                    {
                        if (item.VariantId is Guid variantId)
                        {
                            // Use VariantId overload directly
                            await _inventoryService.AdjustInventoryAsync(
                                variantId,
                                +item.Quantity,
                                InventoryTransactionType.Adjustment,
                                referenceId: existingOrder.OrderNo,
                                ct);
                        }
                    }
                }

                await _orderRepo.DeleteAsync(id);
                await _uow.SaveChangesAsync(ct);
            });
        }
    }
}

