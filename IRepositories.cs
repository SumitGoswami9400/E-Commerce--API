using System.Linq.Expressions;
using ECommerceAPI.Models;

namespace ECommerceAPI.Repositories.Interfaces;

/// <summary>
/// Generic Repository Pattern — works for ANY entity that extends BaseEntity.
/// This avoids code duplication for basic CRUD across all repositories.
/// </summary>
public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?>             GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T>              AddAsync(T entity);
    void                 Update(T entity);
    void                 SoftDelete(T entity);   // Sets IsDeleted = true
    Task<bool>           SaveChangesAsync();
    Task<int>            CountAsync(Expression<Func<T, bool>>? predicate = null);
}

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?>                GetOrderWithItemsAsync(int id);
    Task<IEnumerable<Order>>    GetOrdersByEmailAsync(string email);
    Task<IEnumerable<Order>>    GetOrdersByStatusAsync(OrderStatus status);
    Task<(List<Order> Items, int Total)> GetPagedAsync(int page, int pageSize, OrderStatus? statusFilter = null);
}

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(string category);
    Task<IEnumerable<Product>> GetLowStockAsync(int threshold = 10);
    Task<(List<Product> Items, int Total)> GetPagedAsync(int page, int pageSize, string? category = null, string? search = null);
}
