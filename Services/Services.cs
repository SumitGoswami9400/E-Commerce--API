using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using ECommerceAPI.Repositories.Interfaces;
using ECommerceAPI.Services.Interfaces;
using ECommerceAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ECommerceAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IProductRepository _productRepo;

        public OrderService(IOrderRepository orderRepo, IProductRepository productRepo)
        {
            _orderRepo = orderRepo;
            _productRepo = productRepo;
        }

        public async Task<ApiResponse<OrderResponseDto>> CreateOrderAsync(CreateOrderDto dto)
        {
            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = new Dictionary<int, Product>();

            foreach (var pid in productIds)
            {
                var product = await _productRepo.GetByIdAsync(pid);
                if (product == null)
                    return ApiResponse<OrderResponseDto>.Fail($"Product with ID {pid} not found.");
                products[pid] = product;
            }

            var orderItems = new List<OrderItem>();
            decimal total = 0;

            foreach (var item in dto.Items)
            {
                var product = products[item.ProductId];
                if (product.Stock < item.Quantity)
                    return ApiResponse<OrderResponseDto>.Fail(
                        $"Insufficient stock for '{product.Name}'. Available: {product.Stock}");

                product.Stock -= item.Quantity;
                _productRepo.Update(product);

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                });
                total += product.Price * item.Quantity;
            }

            var order = new Order
            {
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                ShippingAddress = dto.ShippingAddress,
                Notes = dto.Notes,
                TotalAmount = total,
                Status = OrderStatus.Confirmed,
                OrderItems = orderItems
            };

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();
            return ApiResponse<OrderResponseDto>.Ok(MapToDto(order), "Order placed successfully!");
        }

        public async Task<ApiResponse<OrderResponseDto>> GetOrderByIdAsync(int id)
        {
            var order = await _orderRepo.GetOrderWithItemsAsync(id);
            if (order == null) return ApiResponse<OrderResponseDto>.Fail("Order not found.");
            return ApiResponse<OrderResponseDto>.Ok(MapToDto(order));
        }

        public async Task<ApiResponse<PaginatedResult<OrderResponseDto>>> GetAllOrdersAsync(
            int page, int pageSize, OrderStatus? status = null)
        {
            var (orders, total) = await _orderRepo.GetPagedAsync(page, pageSize, status);
            var result = new PaginatedResult<OrderResponseDto>
            {
                Items = orders.Select(MapToDto).ToList(),
                TotalCount = total,
                PageNumber = page,
                PageSize = pageSize
            };
            return ApiResponse<PaginatedResult<OrderResponseDto>>.Ok(result);
        }

        public async Task<ApiResponse<IEnumerable<OrderResponseDto>>> GetOrdersByEmailAsync(string email)
        {
            var orders = await _orderRepo.GetOrdersByEmailAsync(email);
            return ApiResponse<IEnumerable<OrderResponseDto>>.Ok(orders.Select(MapToDto));
        }

        public async Task<ApiResponse<OrderResponseDto>> UpdateStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _orderRepo.GetOrderWithItemsAsync(id);
            if (order == null) return ApiResponse<OrderResponseDto>.Fail("Order not found.");

            if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered)
                return ApiResponse<OrderResponseDto>.Fail($"Cannot update a {order.Status} order.");

            order.Status = dto.Status;
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();
            return ApiResponse<OrderResponseDto>.Ok(MapToDto(order), "Order status updated.");
        }

        public async Task<ApiResponse<bool>> CancelOrderAsync(int id)
        {
            var order = await _orderRepo.GetOrderWithItemsAsync(id);
            if (order == null) return ApiResponse<bool>.Fail("Order not found.");

            if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
                return ApiResponse<bool>.Fail("Cannot cancel an order that has been shipped or delivered.");

            foreach (var item in order.OrderItems)
            {
                var product = await _productRepo.GetByIdAsync(item.ProductId);
                if (product != null) { product.Stock += item.Quantity; _productRepo.Update(product); }
            }

            order.Status = OrderStatus.Cancelled;
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Order cancelled. Stock has been restored.");
        }

        public async Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync()
        {
            var allOrders = await _orderRepo.GetAllAsync();
            var allProducts = await _productRepo.GetAllAsync();
            var ordersList = allOrders.ToList();

            var topProducts = ordersList
                .SelectMany(o => o.OrderItems ?? new List<OrderItem>())
                .GroupBy(oi => oi.ProductId)
                .Select(g => new TopProductDto
                {
                    Name = g.First().Product?.Name ?? "Unknown",
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5).ToList();

            var stats = new DashboardStatsDto
            {
                TotalOrders = ordersList.Count,
                PendingOrders = ordersList.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed),
                DeliveredOrders = ordersList.Count(o => o.Status == OrderStatus.Delivered),
                TotalRevenue = ordersList.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                TotalProducts = allProducts.Count(),
                LowStockProducts = allProducts.Count(p => p.Stock <= 10),
                TopProducts = topProducts
            };

            return ApiResponse<DashboardStatsDto>.Ok(stats);
        }

        private static OrderResponseDto MapToDto(Order order) => new()
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            ShippingAddress = order.ShippingAddress,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            Items = order.OrderItems?.Select(oi => new OrderItemResponseDto
            {
                ProductId = oi.ProductId,
                ProductName = oi.Product?.Name ?? "N/A",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                SubTotal = oi.SubTotal
            }).ToList() ?? new()
        };
    }

    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        public ProductService(IProductRepository repo) => _repo = repo;

        public async Task<ApiResponse<ProductResponseDto>> CreateProductAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name, Description = dto.Description,
                Price = dto.Price, Stock = dto.Stock,
                Category = dto.Category, ImageUrl = dto.ImageUrl
            };
            await _repo.AddAsync(product);
            await _repo.SaveChangesAsync();
            return ApiResponse<ProductResponseDto>.Ok(MapToDto(product), "Product created.");
        }

        public async Task<ApiResponse<ProductResponseDto>> GetProductByIdAsync(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            return product == null
                ? ApiResponse<ProductResponseDto>.Fail("Product not found.")
                : ApiResponse<ProductResponseDto>.Ok(MapToDto(product));
        }

        public async Task<ApiResponse<PaginatedResult<ProductResponseDto>>> GetAllProductsAsync(
            int page, int pageSize, string? category = null, string? search = null)
        {
            var (products, total) = await _repo.GetPagedAsync(page, pageSize, category, search);
            var result = new PaginatedResult<ProductResponseDto>
            {
                Items = products.Select(MapToDto).ToList(),
                TotalCount = total, PageNumber = page, PageSize = pageSize
            };
            return ApiResponse<PaginatedResult<ProductResponseDto>>.Ok(result);
        }

        public async Task<ApiResponse<ProductResponseDto>> UpdateProductAsync(int id, UpdateProductDto dto)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null) return ApiResponse<ProductResponseDto>.Fail("Product not found.");

            product.Name = dto.Name; product.Description = dto.Description;
            product.Price = dto.Price; product.Stock = dto.Stock;
            product.Category = dto.Category; product.ImageUrl = dto.ImageUrl;

            _repo.Update(product);
            await _repo.SaveChangesAsync();
            return ApiResponse<ProductResponseDto>.Ok(MapToDto(product), "Product updated.");
        }

        public async Task<ApiResponse<bool>> DeleteProductAsync(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null) return ApiResponse<bool>.Fail("Product not found.");
            _repo.SoftDelete(product);
            await _repo.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Product deleted.");
        }

        private static ProductResponseDto MapToDto(Product p) => new()
        {
            Id = p.Id, Name = p.Name, Description = p.Description,
            Price = p.Price, Stock = p.Stock, Category = p.Category,
            ImageUrl = p.ImageUrl, CreatedAt = p.CreatedAt
        };
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return ApiResponse<AuthResponseDto>.Fail("Email already registered.");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCryptHelper.HashPassword(dto.Password),
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return ApiResponse<AuthResponseDto>.Ok(GenerateToken(user), "Registration successful.");
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCryptHelper.Verify(dto.Password, user.PasswordHash))
                return ApiResponse<AuthResponseDto>.Fail("Invalid email or password.");
            return ApiResponse<AuthResponseDto>.Ok(GenerateToken(user), "Login successful.");
        }

        private AuthResponseDto GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _config["Jwt:Key"] ?? "SuperSecretKeyForECommerceApp2024!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(8);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "ECommerceAPI",
                audience: _config["Jwt:Audience"] ?? "ECommerceClient",
                claims: claims, expires: expires, signingCredentials: creds
            );

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                FullName = user.FullName, Email = user.Email,
                Role = user.Role, Expires = expires
            };
        }
    }
}
