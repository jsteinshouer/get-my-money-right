using Api.Features.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AccountsFeature = Api.Features.Accounts.Accounts;
using BudgetsFeature = Api.Features.Budgets.Budgets;
using CategoriesFeature = Api.Features.Categories.Categories;
using TransactionsFeature = Api.Features.Transactions.Transactions;

namespace Api.Data;

/// <summary>
/// Resets a development database to a known demo state: everything already in it is deleted, then
/// the configured household users are recreated along with <see cref="DemoHousehold"/>'s history of
/// accounts, categories, monthly budgets and the transactions that spend against them.
/// </summary>
/// <remarks>
/// <b>Destroys all existing data</b>, users included, so it never runs by itself — only when the
/// <c>seed-demo-data</c> command is passed explicitly, and even then it refuses outside Development.
/// </remarks>
public class DemoDataSeeder
{
    /// <summary>The command-line argument that asks for a reset: <c>dotnet run -- seed-demo-data</c>.</summary>
    public const string CommandName = "seed-demo-data";

    private readonly BudgetDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<List<Identity.SeedUser>> _householdUsers;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        BudgetDbContext db,
        UserManager<ApplicationUser> userManager,
        IOptions<List<Identity.SeedUser>> householdUsers,
        IHostEnvironment environment,
        ILogger<DemoDataSeeder> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _householdUsers = householdUsers ?? throw new ArgumentNullException(nameof(householdUsers));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Deletes everything in the database, then rebuilds the household users and demo history.
    /// Outside Development this refuses and leaves the database untouched.
    /// </summary>
    /// <param name="today">
    /// The date to treat as today; history runs back from here and stops here. Defaults to the real
    /// today, and exists so a test can pin how far into the current month the seeding has got.
    /// </param>
    /// <returns>Whether the reset ran.</returns>
    public async Task<bool> ResetToDemoStateAsync(DateOnly? today = null, CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            // Refused rather than obeyed: this destroys every row, and nothing outside a developer's
            // own machine should be able to ask for that.
            _logger.LogWarning(
                "Demo data was requested but the environment is {Environment}, not Development; the database was left untouched.",
                _environment.EnvironmentName);
            return false;
        }

        _logger.LogWarning("Resetting to demo data: deleting all existing data first.");

        // One transaction around the whole reset: a failure part-way through would otherwise leave a
        // half-wiped database with no users in it, which is worse than either end state.
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        await DeleteEverythingAsync(cancellationToken);

        // The wipe took the household users with it, so they come back before anything can be
        // attributed to them.
        await Identity.SeedUsersAsync(_userManager, _householdUsers.Value);

        var userIds = await _db.Users.OrderBy(u => u.UserName).Select(u => u.Id).ToListAsync(cancellationToken);
        if (userIds.Count == 0)
        {
            throw new InvalidOperationException(
                "No household users are configured, so demo data has no one to be attributed to.");
        }

        var accounts = AddAccounts(userIds[0]);
        var categories = AddCategories(userIds[0]);
        // Budgets and transactions reference these by id, so they have to exist first.
        await _db.SaveChangesAsync(cancellationToken);

        var seededSpends = AddHistory(accounts, categories, userIds, today ?? DateOnly.FromDateTime(DateTime.Today));
        await _db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded demo data: {Accounts} accounts, {Categories} categories, {Months} months of budgets and {Transactions} transactions.",
            DemoHousehold.Accounts.Length, DemoHousehold.Budgets.Length, DemoHousehold.MonthsOfHistory, seededSpends);
        return true;
    }

    /// <summary>
    /// Ordered so that restricted foreign keys never block a delete: the rows that point at
    /// something go before the thing they point at, and the users everything hangs off go last.
    /// </summary>
    private async Task DeleteEverythingAsync(CancellationToken cancellationToken)
    {
        await _db.Transactions.ExecuteDeleteAsync(cancellationToken);
        await _db.Budgets.ExecuteDeleteAsync(cancellationToken);
        await _db.Categories.ExecuteDeleteAsync(cancellationToken);
        await _db.Accounts.ExecuteDeleteAsync(cancellationToken);

        await _db.UserTokens.ExecuteDeleteAsync(cancellationToken);
        await _db.UserLogins.ExecuteDeleteAsync(cancellationToken);
        await _db.UserClaims.ExecuteDeleteAsync(cancellationToken);
        await _db.UserRoles.ExecuteDeleteAsync(cancellationToken);
        await _db.RoleClaims.ExecuteDeleteAsync(cancellationToken);
        await _db.Roles.ExecuteDeleteAsync(cancellationToken);
        await _db.Users.ExecuteDeleteAsync(cancellationToken);
    }

    private Dictionary<string, AccountsFeature.Account> AddAccounts(string ownerUserId)
    {
        var accounts = DemoHousehold.Accounts.ToDictionary(
            a => a.Name,
            a => new AccountsFeature.Account { Name = a.Name, Type = a.Type, CreatedByUserId = ownerUserId });
        _db.Accounts.AddRange(accounts.Values);
        return accounts;
    }

    private Dictionary<string, CategoriesFeature.Category> AddCategories(string ownerUserId)
    {
        var categories = DemoHousehold.Budgets.ToDictionary(
            b => b.Name,
            b => new CategoriesFeature.Category { Name = b.Name, CreatedByUserId = ownerUserId });
        _db.Categories.AddRange(categories.Values);
        return categories;
    }

    /// <returns>How many transactions were added.</returns>
    private int AddHistory(
        Dictionary<string, AccountsFeature.Account> accounts,
        Dictionary<string, CategoriesFeature.Category> categories,
        List<string> userIds,
        DateOnly today)
    {
        var enteredBy = 0;

        for (var monthsAgo = 0; monthsAgo < DemoHousehold.MonthsOfHistory; monthsAgo++)
        {
            var month = today.AddMonths(-monthsAgo);

            foreach (var (name, monthlyLimit) in DemoHousehold.Budgets)
            {
                _db.Budgets.Add(new BudgetsFeature.Budget
                {
                    CategoryId = categories[name].Id,
                    Year = month.Year,
                    Month = month.Month,
                    Amount = monthlyLimit,
                });
            }

            foreach (var spend in DemoHousehold.ForMonthsAgo(monthsAgo))
            {
                var date = new DateOnly(month.Year, month.Month, spend.Day);
                // The current month is only as far along as today is; a household hasn't yet spent
                // money it will spend later this month.
                if (date > today) { continue; }

                _db.Transactions.Add(new TransactionsFeature.Transaction
                {
                    AccountId = accounts[spend.Account].Id,
                    CategoryId = categories[spend.Category].Id,
                    Date = date,
                    Amount = spend.Amount,
                    Description = spend.Description,
                    NeedWant = spend.NeedWant,
                    // Alternate the two household members so "who entered what" has something to show.
                    CreatedByUserId = userIds[enteredBy++ % userIds.Count],
                });
            }
        }

        return enteredBy;
    }
}

public static class DemoDataSeederExtensions
{
    public static IServiceCollection AddDemoDataSeeder(this IServiceCollection services) => services
        .AddScoped<DemoDataSeeder>();

    /// <summary>
    /// Runs the <c>seed-demo-data</c> command if it was asked for, so the caller can exit instead of
    /// serving. Resetting the database is never a side effect of an ordinary start.
    /// </summary>
    /// <returns>Whether the command was handled.</returns>
    public static async Task<bool> TryRunDemoDataCommandAsync(this WebApplication app, string[] args)
    {
        if (!args.Contains(DemoDataSeeder.CommandName))
        {
            return false;
        }

        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().ResetToDemoStateAsync();
        return true;
    }
}
