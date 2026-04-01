using HomeBuddy_API.DTOs.Requests.OrderDTOs;
using HomeBuddy_API.Exceptions;
using HomeBuddy_API.Interfaces;
using HomeBuddy_API.Interfaces.EmailInterfaces;
using HomeBuddy_API.Interfaces.InventoryInterfaces;
using HomeBuddy_API.Interfaces.OrderInterfaces;
using HomeBuddy_API.Interfaces.ProductInterfaces;
using HomeBuddy_API.Interfaces.TaxInterfaces;
using HomeBuddy_API.Models;
using Microsoft.Extensions.Configuration;

namespace HomeBuddy_API.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IInventoryService _inventoryService;
        private readonly IVariantRepository _variantRepository;
        private readonly ITaxBracketService _taxService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<OrderService> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public OrderService(
            IOrderRepository orderRepo,
            IInventoryService inventoryService,
            IVariantRepository variantRepository,
            ITaxBracketService taxService,
            IUnitOfWork uow,
            ILogger<OrderService> logger,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _orderRepo = orderRepo;
            _inventoryService = inventoryService;
            _variantRepository = variantRepository;
            _taxService = taxService;
            _uow = uow;
            _logger = logger;
            _emailSender = emailSender;
            _configuration = configuration;
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

        public async Task<string> CreateOrderAsync(OrderCreateDto dto, int? userId = null)
        {
            var countryCode = (dto.CountryCode ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(countryCode))
                throw new InvalidOperationException("CountryCode is required for checkout.");

            var vatRate = _taxService.GetVatRate(countryCode);
            if (vatRate == null)
                throw new InvalidOperationException($"Country '{countryCode}' is not supported. Use GET /api/tax/countries for valid European country codes.");

            // Generate a unique order number and trim to 45 characters.
            var orderNo = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
            orderNo = orderNo[..Math.Min(45, orderNo.Length)];

            decimal subtotal = 0m;
            var orderItems = new List<OrderItem>();
            var lineSummaries = new List<(string Sku, int Quantity, decimal UnitPrice, decimal LineTotal)>();

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
                    var lineTotal = unitPrice * item.Quantity;
                    subtotal += lineTotal;
                    lineSummaries.Add((skuNormalized, item.Quantity, unitPrice, lineTotal));

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

                var taxAmount = Math.Round(subtotal * (vatRate.Value / 100m), 2, MidpointRounding.AwayFromZero);
                var total = subtotal + taxAmount;

                var order = new Order
                {
                    OrderNo = orderNo,
                    UserId = userId,
                    Email = dto.Email,
                    CountryCode = countryCode,
                    Subtotal = subtotal,
                    TaxRate = vatRate.Value,
                    TaxAmount = taxAmount,
                    Total = total,
                    Status = "Pending",
                    CreatedUtc = DateTime.UtcNow,
                    Items = orderItems
                };

                await _orderRepo.CreateAsync(order);

                // Single commit for both inventory + order
                await _uow.SaveChangesAsync(ct);
            });

            var computedTax = Math.Round(subtotal * (vatRate.Value / 100m), 2, MidpointRounding.AwayFromZero);
            var computedTotal = subtotal + computedTax;

            _logger.LogInformation(
                "Order {OrderNo} created for {Email} (UserId: {UserId}) with subtotal {Subtotal} and total {Total} in country {CountryCode}",
                orderNo,
                dto.Email,
                userId,
                subtotal,
                computedTotal,
                countryCode);

            await TrySendOrderConfirmationEmailAsync(
                dto.Email.Trim(),
                orderNo,
                countryCode,
                lineSummaries,
                subtotal,
                computedTax,
                vatRate.Value,
                computedTotal);

            return orderNo;
        }

        private async Task TrySendOrderConfirmationEmailAsync(
            string toEmail,
            string orderNo,
            string countryCode,
            List<(string Sku, int Quantity, decimal UnitPrice, decimal LineTotal)> lines,
            decimal subtotal,
            decimal taxAmount,
            decimal vatPercent,
            decimal total)
        {
            try
            {
                var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
                var subject = $"Order confirmation — {orderNo}";
                var rows = string.Join("", lines.Select(line => $@"
    <tr>
      <td style=""padding:8px;border-bottom:1px solid #eee"">{line.Sku}</td>
      <td style=""padding:8px;border-bottom:1px solid #eee;text-align:right"">{line.Quantity}</td>
      <td style=""padding:8px;border-bottom:1px solid #eee;text-align:right"">{line.UnitPrice:F2} €</td>
      <td style=""padding:8px;border-bottom:1px solid #eee;text-align:right"">{line.LineTotal:F2} €</td>
    </tr>"));
                var body = $@"
<div style=""font-family:Arial,sans-serif;line-height:1.5;color:#222"">
  <h2 style=""color:#8B4545"">Thank you for your order</h2>
  <p>This confirms we have received your order. Please keep your order number for your records.</p>
  <p><strong>Order number:</strong> {orderNo}</p>
  <p><strong>Country (VAT):</strong> {countryCode}</p>
  <table style=""width:100%;border-collapse:collapse;margin:16px 0"">
    <thead>
      <tr style=""background:#f5f5f5"">
        <th style=""text-align:left;padding:8px"">SKU</th>
        <th style=""text-align:right;padding:8px"">Qty</th>
        <th style=""text-align:right;padding:8px"">Unit</th>
        <th style=""text-align:right;padding:8px"">Line</th>
      </tr>
    </thead>
    <tbody>{rows}
    </tbody>
  </table>
  <p>Subtotal: <strong>{subtotal:F2} €</strong><br/>
  VAT ({vatPercent:F2}%): <strong>{taxAmount:F2} €</strong><br/>
  <strong>Total: {total:F2} €</strong></p>
  <p style=""margin-top:24px""><a href=""{baseUrl}"" style=""color:#8B4545"">Return to the shop</a></p>
  <p style=""color:#666;font-size:12px"">This message was sent to the email address used at checkout.</p>
</div>";
                await _emailSender.SendAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email for order {OrderNo} to {Email}", orderNo, toEmail);
            }
        }

        public async Task ClaimOrderAsync(string orderNo, int userId, string userEmail)
        {
            if (string.IsNullOrWhiteSpace(orderNo))
                throw new InvalidOperationException("Order number is required.");

            var order = await _orderRepo.GetOrderByOrderNoAsync(orderNo.Trim());
            if (order == null)
            {
                _logger.LogWarning("Claim order failed: order not found. OrderNo={OrderNo}, UserId={UserId}", orderNo, userId);
                throw new KeyNotFoundException("Order not found.");
            }

            if (order.UserId != null)
            {
                _logger.LogWarning("Claim order failed: order already linked to a user. OrderNo={OrderNo}, UserId={ExistingUserId}, RequestUserId={UserId}",
                    orderNo,
                    order.UserId,
                    userId);
                throw new InvalidOperationException("Order is already linked to an account.");
            }

            if (!string.Equals(order.Email?.Trim(), userEmail?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Claim order failed: email mismatch for OrderNo={OrderNo}. OrderEmail={OrderEmail}, RequestEmail={RequestEmail}",
                    orderNo,
                    order.Email,
                    userEmail);
                throw new InvalidOperationException("Order can only be linked to an account with the same email address.");
            }

            order.UserId = userId;
            await _orderRepo.UpdateAsync(order);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Order {OrderNo} successfully linked to user {UserId}", orderNo, userId);
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

            _logger.LogInformation("Order {OrderId} updated. New status={Status}, New total={Total}",
                id,
                dto.Status ?? "(unchanged)",
                dto.Total ?? 0m);
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

            _logger.LogInformation("Order {OrderId} deleted", id);
        }
    }
}

