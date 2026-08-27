using Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Api.Tests.Fixtures;

/// <summary>
/// Hosts the API against a real, kept-open SQLite connection (not the EF Core InMemory
/// provider) so unique-constraint enforcement is meaningfully tested.
/// </summary>
public class BudgetApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    public const string SeededUser1Email = "testuser1@household.local";
    public const string SeededUser1Password = "Test123!Password";
    public const string SeededUser2Email = "testuser2@household.local";
    public const string SeededUser2Password = "Test123!Password";

    /// <summary>Overridden by fixtures that need the app to believe it is deployed.</summary>
    protected virtual string Environment => Environments.Development;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HouseholdUsers:0:UserName"] = "testuser1",
                ["HouseholdUsers:0:Email"] = SeededUser1Email,
                ["HouseholdUsers:0:Password"] = SeededUser1Password,
                ["HouseholdUsers:0:DisplayName"] = "Test User One",
                ["HouseholdUsers:1:UserName"] = "testuser2",
                ["HouseholdUsers:1:Email"] = SeededUser2Email,
                ["HouseholdUsers:1:Password"] = SeededUser2Password,
                ["HouseholdUsers:1:DisplayName"] = "Test User Two",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BudgetDbContext>>();
            services.AddDbContext<BudgetDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }
}
