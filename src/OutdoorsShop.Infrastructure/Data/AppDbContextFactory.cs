using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OutdoorsShop.Infrastructure.Data;

/// <summary>
/// Used by EF Core CLI tools (dotnet ef migrations) at design time.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ApiProjectFileName = "OutdoorsShop.Api.csproj";
    private const string MissingConnectionPlaceholder = "USE_USER_SECRETS_OR_ENV_VAR";
    private static readonly string ApiProjectRelativePath = Path.Combine("src", "OutdoorsShop.Api", ApiProjectFileName);

    public AppDbContext CreateDbContext(string[] args)
    {
        var environmentName = GetEnvironmentName();
        var apiProjectDirectory = ResolveApiProjectDirectory();
        var configuration = BuildConfiguration(apiProjectDirectory, environmentName);
        var connectionString = GetRequiredConnectionString(configuration, apiProjectDirectory, environmentName);

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    internal static IConfigurationRoot BuildConfiguration(string apiProjectDirectory, string environmentName)
    {
        var userSecretsId = TryGetUserSecretsId(apiProjectDirectory);
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(apiProjectDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true);

        if (TryGetUserSecretsPath(userSecretsId) is { } userSecretsPath)
        {
            configurationBuilder.AddJsonFile(userSecretsPath, optional: true);
        }

        return configurationBuilder
            .AddEnvironmentVariables()
            .Build();
    }

    internal static string GetRequiredConnectionString(IConfiguration configuration, string apiProjectDirectory, string environmentName)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString) ||
            string.Equals(connectionString, MissingConnectionPlaceholder, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Design-time EF could not find a usable ConnectionStrings:DefaultConnection value for environment '{environmentName}'. " +
                $"Checked '{Path.Combine(apiProjectDirectory, "appsettings.json")}', " +
                $"'{Path.Combine(apiProjectDirectory, $"appsettings.{environmentName}.json")}', API user secrets, and environment variables. " +
                "Set the connection string in API user secrets or via the ConnectionStrings__DefaultConnection environment variable before running dotnet ef.");
        }

        return connectionString;
    }

    private static string GetEnvironmentName()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";
    }

    private static string ResolveApiProjectDirectory()
    {
        foreach (var searchDirectory in GetSearchDirectories())
        {
            foreach (var directory in EnumerateDirectoryAndParents(searchDirectory))
            {
                var apiProjectPath = Path.Combine(directory, ApiProjectRelativePath);
                if (File.Exists(apiProjectPath))
                {
                    return Path.GetDirectoryName(apiProjectPath)!;
                }

                var directProjectPath = Path.Combine(directory, ApiProjectFileName);
                if (File.Exists(directProjectPath))
                {
                    return directory;
                }
            }
        }

        throw new InvalidOperationException(
            "Design-time EF could not locate the API project file. Expected to find 'src\\OutdoorsShop.Api\\OutdoorsShop.Api.csproj' from the current working directory or build output path.");
    }

    private static IEnumerable<string> GetSearchDirectories()
    {
        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;
    }

    private static IEnumerable<string> EnumerateDirectoryAndParents(string startDirectory)
    {
        var currentDirectory = new DirectoryInfo(startDirectory);
        while (currentDirectory is not null)
        {
            yield return currentDirectory.FullName;
            currentDirectory = currentDirectory.Parent;
        }
    }

    private static string? TryGetUserSecretsId(string apiProjectDirectory)
    {
        var apiProjectPath = Path.Combine(apiProjectDirectory, ApiProjectFileName);
        var projectDocument = XDocument.Load(apiProjectPath);

        return projectDocument
            .Descendants()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, "UserSecretsId", StringComparison.Ordinal))
            ?.Value
            .Trim();
    }

    private static string? TryGetUserSecretsPath(string? userSecretsId)
    {
        if (string.IsNullOrWhiteSpace(userSecretsId))
        {
            return null;
        }

        if (OperatingSystem.IsWindows())
        {
            var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(applicationDataPath, "Microsoft", "UserSecrets", userSecretsId, "secrets.json");
        }

        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfilePath, ".microsoft", "usersecrets", userSecretsId, "secrets.json");
    }
}
