using System.ComponentModel.DataAnnotations;
using AzureAssignmentApp.Models;

namespace AzureAssignmentApp.DTOs;

public class OrderItemCreateDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}

public class OrderCreateDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string CustomerName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(200)]
    public string? CustomerEmail { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public List<OrderItemCreateDto> Items { get; set; } = new();
}

public class OrderItemResponseDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal => UnitPrice * Quantity;
}

public class OrderResponseDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
}

public class UpdateOrderStatusDto
{
    [Required]
    public OrderStatus Status { get; set; }
}
