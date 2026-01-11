using HomeBuddy_API.DTOs.Requests.OrderDTOs;
using HomeBuddy_API.Exceptions;
using HomeBuddy_API.Interfaces;
using HomeBuddy_API.Interfaces.InventoryInterfaces;
using HomeBuddy_API.Interfaces.OrderInterfaces;
using HomeBuddy_API.Interfaces.ProductInterfaces;
using HomeBuddy_API.Models;
using HomeBuddy_API.Services;
using Moq;
using Xunit;

namespace HomeBuddy_API.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _orderRepoMock;
        private readonly Mock<IInventoryService> _inventoryServiceMock;
        private readonly Mock<IVariantRepository> _variantRepoMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly OrderService _service;

        public OrderServiceTests()
        {
            _orderRepoMock = new Mock<IOrderRepository>();
            _inventoryServiceMock = new Mock<IInventoryService>();
            _variantRepoMock = new Mock<IVariantRepository>();
            _uowMock = new Mock<IUnitOfWork>();

            // Setup UnitOfWork to execute the action immediately
            _uowMock.Setup(u => u.ExecuteInTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

            _service = new OrderService(
                _orderRepoMock.Object,
                _inventoryServiceMock.Object,
                _variantRepoMock.Object,
                _uowMock.Object);
        }

        [Fact]
        // Tests that GetAllOrdersAsync returns a list of orders
        public async Task GetAllOrdersAsync_ShouldReturnOrders()
        {
            var orders = new List<Order>
            {
                new Order { Id = 1, Email = "test@example.com", Total = 299.99m, Status = "Pending" }
            };
            _orderRepoMock.Setup(r => r.GetAllAsync(It.IsAny<int>())).ReturnsAsync(orders);

            var result = await _service.GetAllOrdersAsync(1);

            Assert.Single(result);
            Assert.Equal(299.99m, ((List<Order>)result)[0].Total);
            Assert.Equal("Pending", ((List<Order>)result)[0].Status);
        }

        [Fact]
        // Tests that GetOrdersCountAsync returns the correct count
        public async Task GetOrdersCountAsync_ShouldReturnCount()
        {
            _orderRepoMock.Setup(r => r.GetOrdersCountAsync()).ReturnsAsync(523);

            var result = await _service.GetOrdersCountAsync();

            Assert.Equal(523, result);
        }

        [Fact]
        // Tests that GetOrderAsync returns an order if it exists
        public async Task GetOrderAsync_ShouldReturnOrder_WhenExists()
        {
            var order = new Order
            {
                Id = 1,
                Email = "test@example.com",
                Total = 499.99m,
                Status = "Shipped"
            };
            _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

            var result = await _service.GetOrderAsync(1);

            Assert.NotNull(result);
            Assert.Equal(499.99m, result.Total);
            Assert.Equal("Shipped", result.Status);
        }

        [Fact]
        // Tests that GetOrderAsync returns null when order doesn't exist
        public async Task GetOrderAsync_ShouldReturnNull_WhenNotExists()
        {
            _orderRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Order?)null);

            var result = await _service.GetOrderAsync(999);

            Assert.Null(result);
        }

        [Fact]
        // Tests that GetOrderByOrderEmailAsync returns correct orders
        public async Task GetOrderByOrderEmailAsync_ShouldReturnOrders()
        {
            var email = "customer@example.com";
            var orders = new List<Order>
            {
                new Order { Id = 1, Email = email, Total = 100m },
                new Order { Id = 2, Email = email, Total = 200m }
            };
            _orderRepoMock.Setup(r => r.GetOrderByEmailAsync(email)).ReturnsAsync(orders);

            var result = await _service.GetOrderByOrderEmailAsync(email);

            Assert.Equal(2, result!.Count());
            Assert.All(result, o => Assert.Equal(email, o.Email));
        }

        [Fact]
        // Tests that GetOrderByOrderNoAsync returns correct order
        public async Task GetOrderByOrderNoAsync_ShouldReturnOrder()
        {
            var orderNo = "ORD-20241128-ABC123";
            var order = new Order { Id = 1, OrderNo = orderNo, Total = 100m };
            _orderRepoMock.Setup(r => r.GetOrderByOrderNoAsync(orderNo)).ReturnsAsync(order);

            var result = await _service.GetOrderByOrderNoAsync(orderNo);

            Assert.NotNull(result);
            Assert.Equal(orderNo, result.OrderNo);
        }

        [Fact]
        // Tests that CreateOrderAsync generates order number and creates order
        public async Task CreateOrderAsync_ShouldCreateOrderWithGeneratedOrderNo()
        {
            var variantId = Guid.NewGuid();
            var dto = new OrderCreateDto
            {
                Email = "test@example.com",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { Sku = "TEST-SKU-001", Quantity = 2 }
                }
            };

            _variantRepoMock.Setup(r => r.GetBySkuAsync("TEST-SKU-001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Variant { Id = variantId, Sku = "TEST-SKU-001", Price = 50m });

            await _service.CreateOrderAsync(dto);

            _orderRepoMock.Verify(r => r.CreateAsync(It.Is<Order>(o =>
                o.Email == "test@example.com" &&
                o.Status == "Pending" &&
                o.Total == 100m &&
                o.OrderNo.StartsWith("ORD-") &&
                o.Items.Count == 1
            )), Times.Once);

            _inventoryServiceMock.Verify(i => i.AdjustInventoryAsync(
                variantId,
                -2,
                InventoryTransactionType.Sale,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        // Tests that CreateOrderAsync throws when variant not found
        public async Task CreateOrderAsync_ShouldThrow_WhenVariantNotFound()
        {
            var dto = new OrderCreateDto
            {
                Email = "test@example.com",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { Sku = "INVALID-SKU", Quantity = 1 }
                }
            };

            _variantRepoMock.Setup(r => r.GetBySkuAsync("INVALID-SKU", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Variant?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateOrderAsync(dto));
        }

        [Fact]
        // Tests that UpdateOrderAsync correctly updates status
        public async Task UpdateOrderAsync_ShouldUpdateStatus_WhenExists()
        {
            var existingOrder = new Order
            {
                Id = 1,
                Email = "test@example.com",
                Status = "Pending",
                Total = 100m,
                Items = new List<OrderItem>()
            };
            _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingOrder);

            var dto = new OrderUpdateDto { Status = "Shipped" };
            await _service.UpdateOrderAsync(1, dto);

            _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o =>
                o.Id == 1 &&
                o.Status == "Shipped"
            )), Times.Once);
        }

        [Fact]
        // Tests that UpdateOrderAsync restocks items when cancelling
        public async Task UpdateOrderAsync_ShouldRestockItems_WhenCancelling()
        {
            var variantId = Guid.NewGuid();
            var existingOrder = new Order
            {
                Id = 1,
                Email = "test@example.com",
                Status = "Pending",
                Total = 100m,
                OrderNo = "ORD-TEST-123",
                Items = new List<OrderItem>
                {
                    new OrderItem { VariantId = variantId, Quantity = 3, UnitPrice = 50m }
                }
            };
            _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingOrder);

            var dto = new OrderUpdateDto { Status = "Cancelled" };
            await _service.UpdateOrderAsync(1, dto);

            _inventoryServiceMock.Verify(i => i.AdjustInventoryAsync(
                variantId,
                3,
                InventoryTransactionType.Adjustment,
                "ORD-TEST-123",
                It.IsAny<CancellationToken>()
            ), Times.Once);

            _orderRepoMock.Verify(r => r.UpdateAsync(It.Is<Order>(o =>
                o.Id == 1 &&
                o.Status == "Cancelled"
            )), Times.Once);
        }

        [Fact]
        // Tests that UpdateOrderAsync throws if order does not exist
        public async Task UpdateOrderAsync_ShouldThrow_WhenOrderNotFound()
        {
            _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Order?)null);
            var dto = new OrderUpdateDto { Status = "Shipped" };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateOrderAsync(1, dto));
        }

        [Fact]
        // Tests that DeleteOrderAsync calls repository delete method if order exists
        public async Task DeleteOrderAsync_ShouldCallRepoDelete_WhenOrderExists()
        {
            var existingOrder = new Order
            {
                Id = 1,
                Email = "test@example.com",
                Status = "Pending",
                Total = 100m,
                Items = new List<OrderItem>()
            };
            _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingOrder);

            await _service.DeleteOrderAsync(1);

            _orderRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        // Tests that DeleteOrderAsync restocks items when deleting pending order
        public async Task DeleteOrderAsync_ShouldRestockItems_WhenDeletingPendingOrder()
        {
            var variantId = Guid.NewGuid();
            var existingOrder = new Order
            {
                Id = 1,
                Email = "test@example.com",
                Status = "Pending",
                Total = 150m,
                OrderNo = "ORD-DELETE-TEST",
                Items = new List<OrderItem>
                {
                    new OrderItem { VariantId = variantId, Quantity = 5, UnitPrice = 30m }
                }
            };
            _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingOrder);

            await _service.DeleteOrderAsync(1);

            _inventoryServiceMock.Verify(i => i.AdjustInventoryAsync(
                variantId,
                5,
                InventoryTransactionType.Adjustment,
                "ORD-DELETE-TEST",
                It.IsAny<CancellationToken>()
            ), Times.Once);

            _orderRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        // Tests that DeleteOrderAsync throws if order does not exist
        public async Task DeleteOrderAsync_ShouldThrow_WhenOrderNotFound()
        {
            _orderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Order?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteOrderAsync(1));
        }
    }
}
