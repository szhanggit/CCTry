# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Essential .NET Code Specifications

### 1. Dependency Injection (DI) Mandatory

All dependencies must be injected via constructor injection. Register all services in `Program.cs` or `Startup.cs` using the built-in DI container. Follow the Explicit Dependencies Principle — no hidden `new()` operators inside classes.

Use appropriate lifetimes: `AddScoped()` for DbContext, `AddTransient()` for stateless services, `AddSingleton()` for configuration/caching.

```csharp
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository orderRepository, ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }
}
```

### 2. Testability Requirements

- All class methods must be unit testable — avoid static methods, sealed classes, and hardcoded dependencies
- Every public method should return deterministic results based solely on inputs + injected dependencies
- Use interfaces for all external dependencies (database, HTTP clients, file system, time)
- Use `virtual` keyword for methods needing mocking (or better, mock interfaces directly)

```csharp
// Good - testable
public interface IDateTimeProvider
{
    DateTime Now { get; }
}

public class DiscountCalculator
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public decimal Calculate(decimal amount, IDateTimeProvider dateTimeProvider)
    {
        return dateTimeProvider.Now.DayOfWeek == DayOfWeek.Sunday ? amount * 0.9m : amount;
    }
}

// Bad - untestable (hardcoded DateTime.Now)
public decimal Calculate(decimal amount)
{
    return DateTime.Now.DayOfWeek == DayOfWeek.Sunday ? amount * 0.9m : amount;
}
```

### 3. Unit Test Project Structure

- Every production project must have a matching test project named `[ProjectName].Tests`
- Use xUnit as the primary testing framework (NUnit or MSTest as alternatives)
- Test naming convention: `MethodName_Scenario_ExpectedBehavior`
- Triple-A pattern: Arrange (setup), Act (execute), Assert (verify)
- Minimum 80% code coverage target, enforced in CI pipeline

```csharp
[Fact]
public void CalculateDiscount_OnSunday_ReturnsTenPercentDiscount()
{
    // Arrange
    var mockDateTime = new Mock<IDateTimeProvider>();
    mockDateTime.Setup(x => x.Now).Returns(new DateTime(2024, 1, 7)); // Sunday
    var calculator = new DiscountCalculator(mockDateTime.Object);

    // Act
    var result = calculator.Calculate(100m);

    // Assert
    Assert.Equal(90m, result);
}
```

### 4. Docker Container Requirements

Every service must include a production-ready Dockerfile using multi-stage builds. Use a non-root user for security and configure a healthcheck endpoint.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyProject.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
USER app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyProject.dll"]
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
```

### 5. Docker Compose for Local Development

Include `docker-compose.yml` for multi-container scenarios and `docker-compose.override.yml` for dev-specific overrides.

```yaml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "5000:8080"
    environment:
      - ConnectionStrings__Default=Server=db;Database=MyDb;User=sa;Password=Your_password123
    depends_on:
      - db
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=Your_password123
    volumes:
      - sql_data:/var/opt/mssql

volumes:
  sql_data:
```

### 6. Additional Best Practices

**Null Safety**
- Enable nullable reference types: `<Nullable>enable</Nullable>` in the `.csproj`
- Never return `null` from methods returning collections; use `Enumerable.Empty<T>()` instead

**Async/Await**
- Use `async`/`await` for all I/O operations — never `.Result` or `.Wait()`
- Name async methods with the `Async` suffix

```csharp
public async Task<Order> GetOrderAsync(int id)
{
    return await _context.Orders.FindAsync(id);
}
```

**Error Handling**
- Use result objects instead of throwing exceptions for expected failures
- Implement global exception handling middleware for APIs

**Configuration**
- Use `IOptions<T>` pattern for strongly-typed configuration
- Never hardcode connection strings or secrets

### 7. CI/CD (GitHub Actions)

Two-job pipeline triggered on push:

```yaml
name: Build, Test, and Containerize

on: [push]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
      - run: dotnet publish -c Release -o ./publish

  docker-build:
    needs: build-and-test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - run: docker build -t myapp:latest .
      - run: docker run --rm myapp:latest dotnet test
```

### 8. General Principles

Prefer readability over cleverness. Good code is easy to read, debug, maintain, and predict.

```csharp
// Bad
var r = l.Where(x => x.A && !x.B).Select(x => x.C).ToList();

// Good
var activeCustomers = customers
    .Where(customer => customer.IsActive && !customer.IsDeleted)
    .Select(customer => customer.Email)
    .ToList();
```

### 9. Naming Conventions

Follow standard Microsoft naming conventions:

| Item           | Convention     | Example            |
| -------------- | -------------- | ------------------ |
| Class          | PascalCase     | `OrderService`     |
| Method         | PascalCase     | `CalculatePrice()` |
| Property       | PascalCase     | `FirstName`        |
| Private field  | `_camelCase`   | `_logger`          |
| Local variable | camelCase      | `totalPrice`       |
| Interface      | Prefix `I`     | `IUserRepository`  |
| Async method   | Suffix `Async` | `GetUserAsync()`   |
| Constant       | PascalCase     | `MaxRetryCount`    |

### 10. Async/Await — Additional Rules

**Always pass `CancellationToken`** through the call chain:

```csharp
public async Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
{
    return await _context.Users
        .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
}
```

**Never use `async void`** — exceptions cannot be caught and the caller cannot await it. The only valid use is UI event handlers.

```csharp
// Bad
public async void SaveData() { }

// Good
public async Task SaveDataAsync() { }
```

### 11. Exception Handling — Additional Rules

**Never swallow exceptions silently:**

```csharp
// Bad
try { await SaveAsync(); } catch { }

// Good
try
{
    await SaveAsync();
}
catch (SqlException ex)
{
    _logger.LogError(ex, "Database save failed");
    throw;
}
```

**Use specific exception types, not the base `Exception`:**

```csharp
// Bad
throw new Exception("Invalid data");

// Good
throw new ValidationException("Email address is invalid");
```

### 12. Logging

Use structured logging — never string interpolation in log calls.

```csharp
// Good — queryable structured fields
_logger.LogInformation(
    "User {UserId} created order {OrderId}",
    userId, orderId);

// Bad — plain string, no structured fields
_logger.LogInformation($"User {userId} created order {orderId}");
```

Recommended sinks: Serilog, Seq, Datadog, Elastic.

### 13. Clean Architecture

```text
API
 ↓
Application
 ↓
Domain
 ↓
Infrastructure
```

- **Domain** — entities, value objects, business rules
- **Application** — use cases, DTOs, service interfaces
- **Infrastructure** — EF Core, external APIs, file system, email

Keep controllers thin — delegate all logic to the Application layer:

```csharp
// Bad — business logic in controller
[HttpPost]
public async Task<IActionResult> Create(OrderRequest request)
{
    // 200 lines of business logic
}

// Good — controller as thin dispatcher
[HttpPost]
public async Task<IActionResult> Create(
    CreateOrderCommand command,
    CancellationToken cancellationToken)
{
    var result = await _mediator.Send(command, cancellationToken);
    return Ok(result);
}
```

### 14. Entity Framework Core

**Use `AsNoTracking()` for read-only queries:**

```csharp
var users = await _context.Users
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

**Avoid N+1 — use `Include()` instead of querying inside loops:**

```csharp
// Bad
foreach (var order in orders)
{
    var items = await _context.OrderItems
        .Where(x => x.OrderId == order.Id).ToListAsync();
}

// Good
var orders = await _context.Orders
    .Include(x => x.Items)
    .ToListAsync();
```

**Use projections to avoid loading full entities:**

```csharp
var users = await _context.Users
    .Select(x => new UserDto { Id = x.Id, Name = x.Name })
    .ToListAsync();
```

### 15. API Design

**Use correct HTTP status codes:**

| Status | Meaning      |
| ------ | ------------ |
| 200    | OK           |
| 201    | Created      |
| 400    | Bad Request  |
| 401    | Unauthorized |
| 403    | Forbidden    |
| 404    | Not Found    |
| 409    | Conflict     |
| 500    | Server Error |

**Never expose EF entities directly — always use DTOs:**

```csharp
// Bad
return Ok(userEntity);

// Good
return Ok(new UserResponse { Id = user.Id, Name = user.Name });
```

**Validate input with FluentValidation:**

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress();
```

### 16. Minimal APIs

Appropriate for small services, internal APIs, and prototypes. For large enterprise systems, prefer controllers + clean architecture.

```csharp
app.MapGet("/users/{id}", async (
    Guid id,
    IUserService service,
    CancellationToken cancellationToken) =>
{
    var user = await service.GetByIdAsync(id, cancellationToken);
    return user is null ? Results.NotFound() : Results.Ok(user);
});
```

### 17. Security

Enable HTTPS redirection:

```csharp
app.UseHttpsRedirection();
```

**Password hashing** — use ASP.NET Identity, PBKDF2, bcrypt, or Argon2. Never MD5, SHA1, or plain SHA256.

Never store secrets in code — use environment variables, Azure Key Vault, or AWS Secrets Manager.

### 18. Mock Libraries

Use **Moq** or **NSubstitute** for mocking dependencies in unit tests. Mock interfaces, not concrete classes.

### 19. Folder Structure

```text
src/
 ├── Api/
 ├── Application/
 ├── Domain/
 ├── Infrastructure/
 └── Shared/

tests/
 ├── UnitTests/
 └── IntegrationTests/
```

### 20. Recommended Stack

| Purpose    | Technology       |
| ---------- | ---------------- |
| Framework  | .NET 8 / .NET 9  |
| API        | ASP.NET Core     |
| ORM        | EF Core          |
| Validation | FluentValidation |
| Logging    | Serilog          |
| Messaging  | MassTransit      |
| Cache      | Redis            |
| Database   | PostgreSQL       |
| Testing    | xUnit            |
| Container  | Docker           |
| CI/CD      | GitHub Actions   |

### 21. Code Style

**File-scoped namespaces** (reduces indentation):

```csharp
namespace MyApp.Services;

public class UserService { }
```

**Records for immutable DTOs:**

```csharp
public record UserDto(Guid Id, string Name);
```

**Always mark injected fields `readonly`:**

```csharp
private readonly IUserRepository _repository;
```

Keep methods small with a single responsibility.

### 22. Performance

- Reuse `HttpClient` via `builder.Services.AddHttpClient()` — never `new HttpClient()` per request
- Use in-memory or distributed Redis cache where appropriate
- Measure before optimizing: use BenchmarkDotNet, JetBrains dotTrace, or built-in tracing

### 23. Linters and Analyzers

Enable in `.csproj`:

```xml
<AnalysisLevel>latest</AnalysisLevel>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
```

Recommended: built-in Roslyn analyzers, StyleCop, SonarQube.

### 24. Learning Resources

- [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
- [EF Core Performance](https://learn.microsoft.com/en-us/ef/core/performance/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com)

### 25. Enterprise-Level Advice

Senior .NET developers are evaluated on architecture quality, maintainability, observability, scalability, testing, security, and operational reliability — not on clever syntax, complex LINQ, or excessive abstraction.

The best enterprise C# code is boring, consistent, predictable, observable, and testable.
