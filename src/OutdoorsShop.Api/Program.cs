using FluentValidation;
using FluentValidation.AspNetCore;
using OutdoorsShop.Api.Extensions;
using OutdoorsShop.Api.Middleware;

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
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // OpenAPI document at /openapi/v1.json
}

app.UseHttpsRedirection();
app.UseCors("ReactDevPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Partial class for WebApplicationFactory access in tests
public partial class Program { }
