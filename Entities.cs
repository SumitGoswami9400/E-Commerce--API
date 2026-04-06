namespace ECommerceAPI.Models;

// ──────────────────────────────────────────────
//  Base Entity
// ──────────────────────────────────────────────
public abstract class BaseEntity
{
    public int    Id        { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool   IsDeleted { get; set; } = false; // Soft delete
}

// ──────────────────────────────────────────────
//  Product
// ──────────────────────────────────────────────
public class Product : BaseEntity
{
    public string  Name        { get; set; } = string.Empty;
    public string  Description { get; set; } = string.Empty;
    public decimal Price       { get; set; }
    public int     Stock       { get; set; }
    public string  Category    { get; set; } = string.Empty;
    public string  ImageUrl    { get; set; } = string.Empty;

    // Navigation
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

// ──────────────────────────────────────────────
//  Order
// ──────────────────────────────────────────────
public class Order : BaseEntity
{
    public string     CustomerName  { get; set; } = string.Empty;
    public string     CustomerEmail { get; set; } = string.Empty;
    public string     ShippingAddress { get; set; } = string.Empty;
    public OrderStatus Status       { get; set; } = OrderStatus.Pending;
    public decimal    TotalAmount   { get; set; }
    public string?    Notes         { get; set; }

    // Navigation
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public enum OrderStatus
{
    Pending    = 0,
    Confirmed  = 1,
    Processing = 2,
    Shipped    = 3,
    Delivered  = 4,
    Cancelled  = 5,
    Refunded   = 6
}

// ──────────────────────────────────────────────
//  OrderItem (Many-to-Many bridge)
// ──────────────────────────────────────────────
public class OrderItem : BaseEntity
{
    public int     OrderId   { get; set; }
    public int     ProductId { get; set; }
    public int     Quantity  { get; set; }
    public decimal UnitPrice { get; set; }  // Price at time of order (snapshot)

    public decimal SubTotal => UnitPrice * Quantity;

    // Navigation
    public Order?   Order   { get; set; }
    public Product? Product { get; set; }
}

// ──────────────────────────────────────────────
//  User (for Auth)
// ──────────────────────────────────────────────
public class User : BaseEntity
{
    public string FullName     { get; set; } = string.Empty;
    public string Email        { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role         { get; set; } = "User"; // "User" or "Admin"
}
