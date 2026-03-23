using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AzureAssignmentApp.Data;
using AzureAssignmentApp.DTOs;
using AzureAssignmentApp.Models;

namespace AzureAssignmentApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrderController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrderController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Gets all orders with their items.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => ToResponseDto(o))
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>Gets a single order by ID with items.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound(new { message = $"Order with ID {id} was not found." });

        return Ok(ToResponseDto(order));
    }

    /// <summary>Creates a new order. Validates products, locks in prices, and calculates total.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validate all product IDs in one query
        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Any())
            return NotFound(new { message = $"Products not found with IDs: {string.Join(", ", missing)}" });

        // Check stock availability
        foreach (var item in dto.Items)
        {
            var product = products[item.ProductId];
            if (product.StockQuantity < item.Quantity)
                return BadRequest(new
                {
                    message = $"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}, Requested: {item.Quantity}"
                });
        }

        // Build order
        var order = new Order
        {
            CustomerName = dto.CustomerName,
            CustomerEmail = dto.CustomerEmail,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Build items, lock in unit prices, and deduct stock
        foreach (var item in dto.Items)
        {
            var product = products[item.ProductId];
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price  // Snapshot price at time of order
            });
            product.StockQuantity -= item.Quantity;
        }

        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Reload with product names for response
        await _context.Entry(order)
            .Collection(o => o.Items)
            .Query()
            .Include(i => i.Product)
            .LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToResponseDto(order));
    }

    /// <summary>Updates the status of an order.</summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound(new { message = $"Order with ID {id} was not found." });

        order.Status = dto.Status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ToResponseDto(order));
    }

    /// <summary>Cancels an order (sets status to Cancelled and restores stock).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound(new { message = $"Order with ID {id} was not found." });

        if (order.Status == OrderStatus.Delivered)
            return Conflict(new { message = "Delivered orders cannot be cancelled." });

        if (order.Status == OrderStatus.Cancelled)
            return Conflict(new { message = "Order is already cancelled." });

        // Restore stock
        foreach (var item in order.Items)
        {
            if (item.Product is not null)
                item.Product.StockQuantity += item.Quantity;
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ToResponseDto(order));
    }

    private static OrderResponseDto ToResponseDto(Order o) => new()
    {
        Id = o.Id,
        CustomerName = o.CustomerName,
        CustomerEmail = o.CustomerEmail,
        Status = o.Status.ToString(),
        TotalAmount = o.TotalAmount,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        Items = o.Items.Select(i => new OrderItemResponseDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? "Unknown",
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };
}