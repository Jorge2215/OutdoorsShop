using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using OutdoorsShop.Api.Extensions;
using OutdoorsShop.Api.Middleware;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Database
builder.Services.AddDatabase(builder.Configuration);

// Identity
builder.Services.AddIdentityServices();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization
builder.Services.AddAuthorization();

// Repositories
builder.Services.AddRepositories();
builder.Services.AddDomainServices();

// Blob Storage
builder.Services.AddBlobStorage(builder.Configuration);

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Swagger
builder.Services.AddSwagger();

// CORS — allow React dev server
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins")
    .GetChildren()
    .Select(c => c.Value ?? string.Empty)
    .Where(v => v.Length > 0)
    .ToArray();
if (allowedOrigins.Length == 0)
    allowedOrigins = ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDevPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseHttpsRedirection();
app.UseCors("ReactDevPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

// Seed Identity roles and default admin user on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in new[] { "Administrator", "Customer" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
            logger.LogInformation("Seeded role: {Role}", roleName);
        }
    }

    // Seed default admin user
    const string adminEmail = "admin@outdoorsshop.dev";
    const string adminPassword = "Admin@123456";
    const string adminName = "Admin User";

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
    if (existingAdmin is null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Administrator");

            var customerRepo = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
            var adminCustomer = new Customer
            {
                UserId = adminUser.Id,
                Name = adminName,
                Email = adminEmail
            };
            await customerRepo.AddAsync(adminCustomer);
            await customerRepo.SaveChangesAsync();

            logger.LogInformation("Admin user seeded: {Email}", adminEmail);
        }
        else
        {
            logger.LogError("Failed to seed admin user: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        logger.LogInformation("Admin user already exists: {Email}", adminEmail);
    }

    var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
    var backfilledInventoryCount = await inventoryRepository.EnsureForAllProductsAsync();
    if (backfilledInventoryCount > 0)
    {
        logger.LogInformation(
            "Backfilled missing inventory rows for {BackfilledInventoryCount} existing products during startup.",
            backfilledInventoryCount);
    }
}

app.Run();

// Partial class for WebApplicationFactory access in tests
public partial class Program { }
