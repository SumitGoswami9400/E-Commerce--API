# 🛒 ECommerce Order Management API

A **production-ready** ASP.NET Core 8 Web API demonstrating clean architecture, industry-standard patterns, and real-world best practices.

---

## 🏗️ Architecture & Patterns Used

| Pattern | Purpose |
|---|---|
| **Repository Pattern** | Decouples data access from business logic |
| **Generic Repository** | Reusable CRUD for all entities — no code repetition |
| **Service Layer** | All business logic isolated from controllers |
| **DTO Pattern** | Clean separation between API contracts and domain models |
| **Soft Delete** | Data is never truly deleted — just flagged `IsDeleted = true` |
| **Global Exception Middleware** | Consistent error responses, no stack trace leaks |
| **JWT Authentication** | Stateless, scalable auth with Role-based policies |
| **Pagination** | All list endpoints return paginated results |

---

## 📁 Project Structure

```
ECommerceAPI/
├── Controllers/          # HTTP layer — thin, delegates to services
│   └── Controllers.cs    # Auth, Products, Orders controllers
├── Services/             # Business logic layer
│   └── Services.cs       # OrderService, ProductService, AuthService
├── Repositories/         # Data access layer
│   ├── Interfaces/       # Abstractions (depend on interfaces, not implementations)
│   └── Repositories.cs   # GenericRepository + specialized repos
├── Models/               # Domain entities
│   └── Entities.cs       # Product, Order, OrderItem, User, BaseEntity
├── DTOs/                 # Request/Response contracts
│   └── Dtos.cs           # All DTOs + ApiResponse<T> + PaginatedResult<T>
├── Data/                 # EF Core
│   └── AppDbContext.cs   # DbContext with Fluent API config + Seeder
├── Middleware/           # Custom middleware
│   └── ExceptionHandlingMiddleware.cs
├── appsettings.json      # Config (JWT, DB connection)
└── Program.cs            # App setup, DI registration, middleware pipeline
```

---

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

### Run the project
```bash
# Restore packages
dotnet restore

# Run (auto-creates SQLite DB and seeds data)
dotnet run
```

Open your browser: **http://localhost:5000** → Swagger UI loads automatically!

---

## 🔐 Authentication

### 1. Login with seeded admin account
```
POST /api/auth/login
{
  "email": "admin@ecommerce.com",
  "password": "Admin@123"
}
```

### 2. Copy the `token` from response

### 3. Click **Authorize** in Swagger → Enter: `Bearer <your-token>`

---

## 📖 API Endpoints

### 🔓 Public (No Auth Required)
| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login → get JWT token |
| GET | `/api/products` | Get all products (paginated, filterable) |
| GET | `/api/products/{id}` | Get single product |

### 🔒 User + Admin
| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/orders` | Place a new order |
| GET | `/api/orders/{id}` | Get order by ID |
| GET | `/api/orders/by-email/{email}` | Get all orders by customer email |
| DELETE | `/api/orders/{id}/cancel` | Cancel order (restores stock) |

### 👑 Admin Only
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/orders` | All orders (paginated + status filter) |
| PATCH | `/api/orders/{id}/status` | Update order status |
| GET | `/api/orders/dashboard/stats` | Revenue, top products, analytics |
| POST | `/api/products` | Create product |
| PUT | `/api/products/{id}` | Update product |
| DELETE | `/api/products/{id}` | Soft-delete product |

---

## 💡 Key Design Decisions (Interview Points)

**Q: Why Generic Repository?**
> Avoids duplicating `GetAll`, `GetById`, `Add`, `Update`, `Delete` for every entity. Specialized repos extend it only for entity-specific queries.

**Q: Why Soft Delete?**
> Real applications almost never hard-delete data. Soft delete preserves audit trails and allows data recovery.

**Q: Why price snapshot in OrderItem.UnitPrice?**
> Product prices change over time. Storing the price *at time of order* ensures old orders display correct historical amounts.

**Q: Why SQLite instead of SQL Server?**
> Zero setup — runs anywhere without installing a database server. The same EF Core code works with SQL Server by changing one line in `Program.cs`.

**Q: Why Middleware for exceptions?**
> Prevents stack traces from leaking to clients. Returns consistent `ApiResponse<T>` format for all errors.

---

## 🗃️ Database Schema

```
Users          Products
─────────      ────────────
Id             Id
FullName       Name
Email          Description
PasswordHash   Price
Role           Stock
               Category
Orders         ImageUrl
──────
Id             OrderItems
CustomerName   ──────────
CustomerEmail  Id
ShippingAddr   OrderId  ──→ Orders
TotalAmount    ProductId ──→ Products
Status         Quantity
               UnitPrice (price snapshot)
```

---

## 🧪 Test the Order Flow

1. **Login** → get token
2. **GET /api/products** → note a product ID and check its stock
3. **POST /api/orders** → place order with that product
4. **GET /api/products/{id}** → stock is reduced ✅
5. **DELETE /api/orders/{id}/cancel** → stock is restored ✅
6. **GET /api/orders/dashboard/stats** → see analytics ✅

---

*Built with ASP.NET Core 8 · Entity Framework Core · SQLite · JWT · BCrypt*
