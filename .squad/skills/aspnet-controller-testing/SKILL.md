# ASP.NET Core Controller Testing — xUnit + Moq Patterns

## Overview

This skill covers unit testing ASP.NET Core API controllers using xUnit, Moq, and FluentAssertions without spinning up a full HTTP host. All patterns have been validated against OutdoorsShop .NET 10.

---

## 1. Basic Controller Unit Test Setup

```csharp
public class ProductsControllerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IInventoryRepository> _inventoryRepo = new();
    private readonly ProductsController _sut;

    public ProductsControllerTests()
    {
        _sut = new ProductsController(_productRepo.Object, _inventoryRepo.Object);
    }
}
```

**Key point:** Inject the mocked `.Object` properties directly into the controller constructor. No TestServer, no HTTP client.

---

## 2. Mocking JWT Claims in Controller Unit Tests

When a controller reads `User.FindFirstValue("claim_type")` or `User.IsInRole("Admin")`, construct a `ClaimsPrincipal` and attach it via `ControllerBase.ControllerContext`:

```csharp
private static ControllerContext MakeControllerContext(IEnumerable<Claim> claims)
{
    var identity = new ClaimsIdentity(claims, authenticationType: "Test");
    //                                       ^ "Test" marks identity as authenticated
    var principal = new ClaimsPrincipal(identity);
    return new ControllerContext
    {
        HttpContext = new DefaultHttpContext { User = principal }
    };
}

// Usage:
_sut.ControllerContext = MakeControllerContext([
    new Claim(JwtRegisteredClaimNames.Sub, "user-123"),
    new Claim("customer_id", "42"),
    new Claim(ClaimTypes.Role, "Administrator"),  // for IsInRole checks
]);
```

**Claim type gotchas:**
- `User.IsInRole("Administrator")` checks `ClaimTypes.Role` = `"http://schemas.microsoft.com/ws/2008/06/identity/claims/role"` (the full URI, not `"role"`)
- `User.FindFirstValue("customer_id")` uses the literal string key — match it exactly
- `JwtRegisteredClaimNames.Sub` = `"sub"` — use this constant, not the string

---

## 3. UserManager and SignInManager Mocking

`UserManager<T>` requires 9 constructor parameters. Minimize boilerplate:

```csharp
private static UserManager<ApplicationUser> MakeUserManager()
{
    var store = new Mock<IUserStore<ApplicationUser>>();
    return new UserManager<ApplicationUser>(
        store.Object,
        /*options*/ null!,
        /*hasher*/  null!,
        /*userValidators*/     Array.Empty<IUserValidator<ApplicationUser>>(),
        /*passwordValidators*/ Array.Empty<IPasswordValidator<ApplicationUser>>(),
        /*keyNormalizer*/ null!,
        /*errors*/        null!,
        /*services*/      null!,
        /*logger*/        Mock.Of<ILogger<UserManager<ApplicationUser>>>()
    );
}

private static SignInManager<ApplicationUser> MakeSignInManager(
    UserManager<ApplicationUser> userManager)
{
    return new SignInManager<ApplicationUser>(
        userManager,
        Mock.Of<IHttpContextAccessor>(),
        Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
        /*options*/   null!,
        /*logger*/    null!,
        /*schemes*/   null!,
        /*confirmation*/ null!
    );
}
```

Then wrap these in `Mock<UserManager<T>>` if you need to `.Setup()` method returns:

```csharp
var userManagerMock = new Mock<UserManager<ApplicationUser>>(
    store.Object, null!, null!,
    Array.Empty<IUserValidator<ApplicationUser>>(),
    Array.Empty<IPasswordValidator<ApplicationUser>>(),
    null!, null!, null!,
    Mock.Of<ILogger<UserManager<ApplicationUser>>>());

userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
               .ReturnsAsync((ApplicationUser?)null);
```

---

## 4. Role-Based Authorization in Unit Tests

`[Authorize(Roles = "Administrator")]` attribute enforcement is middleware-level. It is **NOT testable** in controller unit tests — the attribute is simply ignored when you call the controller method directly.

**What IS testable:** runtime role checks in the method body:

```csharp
// In controller:
if (!User.IsInRole("Administrator") && customerId != requestingCustomerId)
    return Forbid();

// In test:
_sut.ControllerContext = MakeControllerContext([
    new Claim(ClaimTypes.Role, "Customer"),
    new Claim("customer_id", "42"),
]);
var result = await _sut.GetById(99); // different customer ID
result.Should().BeOfType<ForbidResult>();
```

For attribute-based auth, write integration tests using `WebApplicationFactory<Program>` instead.

---

## 5. ActionResult Assertions with FluentAssertions

```csharp
// Check for OkObjectResult with value
var ok = result.Should().BeOfType<OkObjectResult>().Subject;
ok.Value.Should().BeEquivalentTo(expected);

// Check for 201 Created
var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
created.StatusCode.Should().Be(201);

// Check for 404
result.Should().BeOfType<NotFoundResult>();

// Check for 400 with message
var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
bad.Value.Should().Be("Error message");
```

---

## 6. EF Core InMemory for Function/Service Unit Tests

```csharp
private static AppDbContext MakeDb(string? name = null)
{
    var opts = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())  // unique name = isolated DB
        .Options;
    return new AppDbContext(opts);
}

// Seed and test:
await using var db = MakeDb();
db.Products.Add(new Product { ProductID = 1, Name = "Tent", IsActive = true });
await db.SaveChangesAsync();
// ... call SUT ...
var updated = await db.Products.FindAsync(1);
updated!.Price.Should().Be(expectedPrice);
```

**InMemory gotchas:**
- Global query filters (`.HasQueryFilter(p => p.IsActive)`) ARE applied by InMemory. Use `.IgnoreQueryFilters()` to bypass.
- `HasData()` seed in `OnModelCreating` is NOT seeded into InMemory databases — you must insert manually.
- EF Core 10.0: Cannot register both SqlServer and InMemory in the same application service provider — causes "Only a single database provider" error. See `creta-test-strategy.md` §4.

---

## 7. Testing OperationResult<T> Patterns

When services return `OperationResult<T>`, test each outcome:

```csharp
// Success path
_service.Setup(s => s.GetAsync(id, userId))
        .ReturnsAsync(OperationResult<CustomerDto>.Success(dto));

// Not found path
_service.Setup(s => s.GetAsync(999, userId))
        .ReturnsAsync(OperationResult<CustomerDto>.NotFound("Customer not found"));

// Forbidden path
_service.Setup(s => s.GetAsync(otherId, userId))
        .ReturnsAsync(OperationResult<CustomerDto>.Forbidden("Access denied"));
```

The controller extension method `result.ToActionResult()` maps these to HTTP responses — so you only need to mock the service return value and assert the HTTP result type.
