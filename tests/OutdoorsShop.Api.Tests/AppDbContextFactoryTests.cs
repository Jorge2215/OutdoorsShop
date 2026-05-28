using FluentAssertions;
using Microsoft.Extensions.Configuration;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Api.Tests;

public sealed class AppDbContextFactoryTests : IDisposable
{
    private const string UserSecretsId = "outdoorsshop-design-time-factory-tests";
    private readonly string? _originalConnectionStringEnvironmentVariable = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    private readonly string _userSecretsDirectory = GetUserSecretsDirectory(UserSecretsId);

    [Fact]
    public void BuildConfiguration_LoadsSourcesInExpectedOrder()
    {
        Directory.CreateDirectory(_userSecretsDirectory);
        File.WriteAllText(
            Path.Combine(_userSecretsDirectory, "secrets.json"),
            """
            {
              "ConnectionStrings": {
                "DefaultConnection": "Server=user-secrets;Database=FromSecrets;"
              }
            }
            """);

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=env-var;Database=FromEnvironment;");

        var configuration = AppDbContextFactory.BuildConfiguration(GetTestApiProjectDirectory(), "Development");

        configuration.GetConnectionString("DefaultConnection")
            .Should()
            .Be("Server=env-var;Database=FromEnvironment;");
    }

    [Fact]
    public void GetRequiredConnectionString_ThrowsClearException_WhenConnectionIsMissing()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        DeleteUserSecretsDirectory();

        var configuration = AppDbContextFactory.BuildConfiguration(GetTestApiProjectDirectory(), "Development");

        var action = () => AppDbContextFactory.GetRequiredConnectionString(configuration, GetTestApiProjectDirectory(), "Development");

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Design-time EF could not find a usable ConnectionStrings:DefaultConnection value for environment 'Development'.*Set the connection string in API user secrets or via the ConnectionStrings__DefaultConnection environment variable before running dotnet ef.*");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _originalConnectionStringEnvironmentVariable);
        if (Directory.Exists(_userSecretsDirectory))
        {
            Directory.Delete(_userSecretsDirectory, recursive: true);
        }
    }

    private static string GetTestApiProjectDirectory()
    {
        return Path.Combine(GetRepositoryRoot(), "tests", "OutdoorsShop.Api.Tests", "TestAssets", "DesignTimeFactoryApi");
    }

    private static string GetRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "OutdoorsShop.slnx")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root for AppDbContextFactory tests.");
    }

    private static string GetUserSecretsDirectory(string userSecretsId)
    {
        var baseDirectory = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft", "usersecrets");

        return Path.Combine(baseDirectory, userSecretsId);
    }

    private void DeleteUserSecretsDirectory()
    {
        if (Directory.Exists(_userSecretsDirectory))
        {
            Directory.Delete(_userSecretsDirectory, recursive: true);
        }

        Directory.CreateDirectory(_userSecretsDirectory);
    }
}
