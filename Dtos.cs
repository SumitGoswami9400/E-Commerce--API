using System.ComponentModel.DataAnnotations;
using ECommerceAPI.Models;

namespace ECommerceAPI.DTOs;

// ──────────────────────────────────────────────
//  Common
// ──────────────────────────────────────────────
public class ApiResponse<T>
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T?     Data    { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new() };
}

public class PaginatedResult<T>
{
    public List<T> Items       { get; set; } = new();
    public int     TotalCount  { get; set; }
    public int     PageNumber  { get; set; }
    public int     PageSize    { get; set; }
    public int     TotalPages  => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool    HasNextPage => PageNumber < TotalPages;
    public bool    HasPrevPage => PageNumber > 1;
}

// ──────────────────────────────────────────────
//  Product DTOs
// ──────────────────────────────────────────────
public class CreateProductDto
{
    [Required] [MaxLength(200)]
    public string  Name        { get; set; } = string.Empty;
    public string  Description { get; set; } = string.Empty;
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price       { get; set; }
    [Range(0, int.MaxValue)]
    public int     Stock       { get; set; }
    [Required]
    public string  Category    { get; set; } = string.Empty;
    public string  ImageUrl    { get; set; } = string.Empty;
}

public class UpdateProductDto : CreateProductDto { }

public class ProductResponseDto
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string  Description { get; set; } = string.Empty;
    public decimal Price       { get; set; }
    public int     Stock       { get; set; }
    public string  Category    { get; set; } = string.Empty;
    public string  ImageUrl    { get; set; } = string.Empty;
    public bool    InStock     => Stock > 0;
    public DateTime CreatedAt  { get; set; }
}

// ──────────────────────────────────────────────
//  Order DTOs
// ──────────────────────────────────────────────
public class CreateOrderDto
{
    [Required] public string CustomerName    { get; set; } = string.Empty;
    [Required] [EmailAddress]
    public string CustomerEmail              { get; set; } = string.Empty;
    [Required] public string ShippingAddress { get; set; } = string.Empty;
    public string? Notes                     { get; set; }

    [Required] [MinLength(1, ErrorMessage = "Order must have at least 1 item")]
    public List<OrderItemDto> Items          { get; set; } = new();
}

public class OrderItemDto
{
    [Required] public int ProductId { get; set; }
    [Range(1, 100)] public int Quantity { get; set; }
}

public class UpdateOrderStatusDto
{
    [Required] public OrderStatus Status { get; set; }
}

public class OrderResponseDto
{
    public int         Id              { get; set; }
    public string      CustomerName    { get; set; } = string.Empty;
    public string      CustomerEmail   { get; set; } = string.Empty;
    public string      ShippingAddress { get; set; } = string.Empty;
    public string      Status          { get; set; } = string.Empty;
    public decimal     TotalAmount     { get; set; }
    public string?     Notes           { get; set; }
    public DateTime    CreatedAt       { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
}

public class OrderItemResponseDto
{
    public int     ProductId   { get; set; }
    public string  ProductName { get; set; } = string.Empty;
    public int     Quantity    { get; set; }
    public decimal UnitPrice   { get; set; }
    public decimal SubTotal    { get; set; }
}

// ──────────────────────────────────────────────
//  Auth DTOs
// ──────────────────────────────────────────────
public class RegisterDto
{
    [Required] public string FullName { get; set; } = string.Empty;
    [Required] [EmailAddress]
    public string Email               { get; set; } = string.Empty;
    [Required] [MinLength(6)]
    public string Password            { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required] [EmailAddress]
    public string Email    { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token     { get; set; } = string.Empty;
    public string FullName  { get; set; } = string.Empty;
    public string Email     { get; set; } = string.Empty;
    public string Role      { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
}

// ──────────────────────────────────────────────
//  Dashboard / Analytics DTO
// ──────────────────────────────────────────────
public class DashboardStatsDto
{
    public int     TotalOrders      { get; set; }
    public int     PendingOrders    { get; set; }
    public int     DeliveredOrders  { get; set; }
    public decimal TotalRevenue     { get; set; }
    public int     TotalProducts    { get; set; }
    public int     LowStockProducts { get; set; }
    public List<TopProductDto> TopProducts { get; set; } = new();
}

public class TopProductDto
{
    public string Name       { get; set; } = string.Empty;
    public int    TotalSold  { get; set; }
    public decimal Revenue   { get; set; }
}
