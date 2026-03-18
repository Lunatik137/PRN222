# CloneEbay Project Detailed Documentation

## 1. Overview

This repository contains an ASP.NET Core MVC web application (`Project_Group3`) for a marketplace-style platform (CloneEbay) with:

- MVC pages for login, admin dashboard, and order management.
- API endpoints for user management.
- Entity Framework Core with SQL Server.
- Session-based authentication checks for admin pages.

## 2. Solution Structure

- `Project_Group3.sln`: Visual Studio solution.
- `Project_Group3/Project_Group3.csproj`: Main ASP.NET Core web project (`net9.0`).
- `Project_Group3/Program.cs`: App startup and middleware pipeline.
- `Project_Group3/DependencyInjection.cs`: DbContext and repository registration via convention.
- `Project_Group3/Models/*`: Entity and view-model classes.
- `Project_Group3/Repository/*`: Data-access repositories and interfaces.
- `Project_Group3/Controllers/*`: MVC and API controllers.
- `Project_Group3/Views/*`: Razor views.
- `Project_Group3/wwwroot/*`: Static assets (CSS/JS/libs).
- `Project_Group3/appsettings*.json`: Configuration and connection strings.

## 3. Technology Stack

- .NET: `net9.0`
- ASP.NET Core MVC
- Entity Framework Core (`8.0.8`)
- SQL Server provider for EF Core
- Swagger (`Swashbuckle.AspNetCore 6.6.2`) in Development environment
- Bootstrap + jQuery (frontend assets)

## 4. Configuration

### 4.1 Connection String

Configured in `Project_Group3/appsettings.json`:

`ConnectionStrings:DefaultConnectionStringDB`

Current value points to local SQL Server:

`Server=localhost;Database=CloneEbayDB;User Id=sa;Password=123;TrustServerCertificate=True`

### 4.2 Launch Profiles

`Project_Group3/Properties/launchSettings.json` defines:

- HTTP: `http://localhost:5287`
- HTTPS: `https://localhost:7179` and `http://localhost:5287`

## 5. Startup and Middleware Pipeline

Defined in `Project_Group3/Program.cs`:

1. Register infrastructure services:
   - `AddInfrastructure(builder.Configuration)`
2. Register MVC:
   - `AddControllersWithViews()`
3. Register session:
   - `AddSession()`
4. Register Swagger:
   - Enabled only when environment is Development.
5. Middleware order:
   - `UseHttpsRedirection()`
   - `UseRouting()`
   - `UseSession()`
   - `UseAuthorization()`
6. Endpoint mapping:
   - Default route: `{controller=Home}/{action=Index}/{id?}`

## 6. Dependency Injection Pattern

`DependencyInjection.cs` contains:

- `AddDbContext<CloneEbayDbContext>(UseSqlServer(...))`
- Automatic repository scanning:
  - Finds classes ending with `Repository`.
  - Maps them to interfaces by naming convention:
    - `UserRepository` -> `IUserRepository`
    - `OrderRepository` -> `IOrderRepository`

## 7. Data Layer

## 7.1 DbContext

`CloneEbayDbContext` maps marketplace-related entities:

- User, Address, Product, Category
- OrderTable, OrderItem
- Payment, ShippingInfo
- ReturnRequest, Review, Feedback
- Bid, Coupon, Inventory
- Message, Dispute
- Store, StoreUpgradeRequest
- RiskAssessment

It also defines entity configurations (table names, keys, column types, relationships, indexes).

## 7.2 Important Domain Models

- `User`: Account profile, approval/lock status, 2FA fields, risk fields, login metadata.
- `Product`: Product listing, pricing, optional auction fields.
- `OrderTable`: Order header (buyer, address, date, status, total).
- `OrderItem`: Product-level line items for each order.
- `RiskAssessment`: Fraud/risk scoring history linked to a user.

Note:
- Most entities are DB-first style with nullable properties and lowercase naming to match the existing database schema.

## 8. Repository Layer

## 8.1 User Repository (`IUserRepository` / `UserRepository`)

Provides async operations:

- Get all users / get by id / email / username
- Credential lookup for login
- Create/update user
- 2FA enable/disable and recovery code updates
- Filter by last login IP
- Paged filtering (`keyword`, `isApproved`, `isLocked`, `page`, `pageSize`)
- Approve user
- Lock/unlock user

## 8.2 Order Repository (`IOrderRepository` / `OrderRepository`)

Provides synchronous operations:

- Get all orders (descending by date)
- Get one order by id
- Get order items by order id
- Update order status

## 9. Controllers and Application Flows

## 9.1 MVC Controllers

### `HomeController`

- `Index()`: Home page.
- `Privacy()`: Privacy page.
- `Error()`: Error page with request id.

### `AccountController`

- `GET /Account/Login`: Login form.
- `POST /Account/Login`: Validates username/password through `IUserRepository`.
- `POST /Account/Logout`: Clears session.

Session keys used:

- `UserId`
- `Username`

### `AdminController`

Protects admin pages by checking `Session["UserId"]`:

- `Dashboard()`
- `UserManagement()`
- `ProductModeration()`
- `OrderManagement()`
- `ReviewsFeedback()`
- `ComplaintsDisputes()`
- `Analytics()`
- `SystemSettings()`

Most section actions render a common placeholder page (`Views/Admin/Section.cshtml`) with dynamic title.

### `AdminOrderController`

- `Index()`: Order list view.
- `Details(int id)`: Order detail + line items.
- `UpdateStatus(int id, string status)`: Updates order status and redirects to details.

## 9.2 API Controller

### `UserController` (base route: `api/user`)

Endpoints:

- `GET /api/user/{id}`
- `GET /api/user/all`
- `POST /api/user/login`
- `GET /api/user/by-email?email=...`
- `GET /api/user/by-username?username=...`
- `GET /api/user/paged?...`
- `POST /api/user/approve/{id}`
- `POST /api/user/lock/{id}`
- `POST /api/user/unlock/{id}`
- `POST /api/user/2fa/enable/{id}`
- `POST /api/user/2fa/disable/{id}`
- `POST /api/user/2fa/recovery-codes/{id}`
- `GET /api/user/by-last-login-ip?ipAddress=...`
- `POST /api/user/create`
- `PUT /api/user/update`

## 10. Views and Frontend

- Shared layout: `Views/Shared/_Layout.cshtml`
- Login view: `Views/Account/Login.cshtml`
- Admin UI:
  - `Views/Admin/Dashboard.cshtml`
  - `Views/Admin/Section.cshtml`
  - `Views/Admin/_AdminSidebar.cshtml`
- Admin order UI:
  - `Views/AdminOrder/Index.cshtml`
  - `Views/AdminOrder/Details.cshtml`
- Styling:
  - `wwwroot/css/site.css` contains dashboard and general styling.
  - `Views/Shared/_Layout.cshtml.css` contains layout defaults.

## 11. Authentication and Authorization Notes

- Current protection for admin pages is session-based (`UserId` in session), not role-policy authorization.
- `UseAuthorization()` is enabled, but no configured authentication scheme is present in startup.
- API endpoints in `UserController` are publicly accessible unless protected elsewhere.

## 12. Risks and Improvement Areas

1. Credential handling:
   - Login checks plain text password equality.
   - Password hashing/salting is not implemented.
2. Hard-coded DB credentials in source (`appsettings.json`).
3. Limited authorization:
   - Session existence check only.
   - No role-based/claim-based policy.
4. Mixed async and sync repository patterns.
5. Missing robust validation in some API endpoints (request shape, null handling, status value validation).
6. Minimal error handling around repository/database failures.
7. No automated tests present in the repository.

## 13. How to Run

1. Ensure SQL Server is running and the `CloneEbayDB` schema exists.
2. Update connection string if needed in `appsettings.json`.
3. Restore and run:

```powershell
dotnet restore
dotnet run --project .\Project_Group3\Project_Group3.csproj
```

4. Open:
   - App: `https://localhost:7179` or `http://localhost:5287`
   - Swagger (Development): `/swagger`

## 14. Suggested Next Documentation Files

- `API_REFERENCE.md`: Request/response samples and status codes for `api/user`.
- `DATABASE_SCHEMA.md`: Table-level schema and relationship diagram.
- `SECURITY_NOTES.md`: Auth, password storage, secrets, and hardening checklist.

