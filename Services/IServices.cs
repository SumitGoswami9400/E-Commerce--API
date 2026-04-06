using ECommerceAPI.DTOs;
using ECommerceAPI.Models;

namespace ECommerceAPI.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<OrderResponseDto>> CreateOrderAsync(CreateOrderDto dto);
        Task<ApiResponse<OrderResponseDto>> GetOrderByIdAsync(int id);
        Task<ApiResponse<PaginatedResult<OrderResponseDto>>> GetAllOrdersAsync(int page, int pageSize, OrderStatus? status = null);
        Task<ApiResponse<IEnumerable<OrderResponseDto>>> GetOrdersByEmailAsync(string email);
        Task<ApiResponse<OrderResponseDto>> UpdateStatusAsync(int id, UpdateOrderStatusDto dto);
        Task<ApiResponse<bool>> CancelOrderAsync(int id);
        Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync();
    }

    public interface IProductService
    {
        Task<ApiResponse<ProductResponseDto>> CreateProductAsync(CreateProductDto dto);
        Task<ApiResponse<ProductResponseDto>> GetProductByIdAsync(int id);
        Task<ApiResponse<PaginatedResult<ProductResponseDto>>> GetAllProductsAsync(int page, int pageSize, string? category = null, string? search = null);
        Task<ApiResponse<ProductResponseDto>> UpdateProductAsync(int id, UpdateProductDto dto);
        Task<ApiResponse<bool>> DeleteProductAsync(int id);
    }

    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto);
    }
}
