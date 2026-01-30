using HomeBuddy_API.DTOs.Requests.OrderDTOs;
using HomeBuddy_API.Interfaces.OrderInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,User")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("{id}")]
    // Only authenticated users can see a specific order
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _orderService.GetOrderAsync(id);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")] // Only Admins can see all orders
    public async Task<IActionResult> GetAllOrders(int page)
    {
        var orders = await _orderService.GetAllOrdersAsync(page);
        if (orders == null) return NotFound("There are no orders registered");
        return Ok(orders);
    }

    [HttpGet("count")]
    [Authorize(Roles = "Admin")] // Only Admins can see order count
    public async Task<IActionResult> GetOrderCount()
    {
        var count = await _orderService.GetOrdersCountAsync();
        return Ok(new { count });
    }

    [HttpGet("by-email/{email}")]
    // Only authenticated users can see their orders by email
    public async Task<IActionResult> GetOrderByEmail(string email)
    {
        var order = await _orderService.GetOrderByOrderEmailAsync(email);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpGet("by-orderNo/{orderNo}")]
    // Only authenticated users can see their orders by order number
    public async Task<IActionResult> GetOrderByOrderNoAsync(string orderNo)
    {
        var order = await _orderService.GetOrderByOrderNoAsync(orderNo);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpPost]
    // Only authenticated users can create orders
    public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.CountryCode))
            return BadRequest(new { error = "CountryCode is required for checkout. Use GET /api/tax/countries for valid European country codes." });

        try
        {
            await _orderService.CreateOrderAsync(dto);
            return Ok("Order created");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Country") || ex.Message.Contains("country"))
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")] // Only Admins can update orders (Status updates)
    public async Task<IActionResult> UpdateOrderStatus(int id, OrderUpdateDto dto)
    {
        await _orderService.UpdateOrderAsync(id, dto);
        return Ok("Updated");
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")] // Only Admins can delete orders
    public async Task<IActionResult> DeleteOrder(int id)
    {
        await _orderService.DeleteOrderAsync(id);
        return Ok("Deleted");
    }
}
