using Api.Features.Accounts;
using Api.Features.Budgets;
using Api.Features.Categories;
using Api.Features.Identity;
using Api.Features.Transactions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Data;

/// <summary>
/// Resets a development database to a known demo state: everything already in it is deleted, then
/// the configured household users are recreated along with a plausible three-month history —
/// accounts, categories, monthly budgets and the transactions that spend against them.
/// </summary>
/// <remarks>
/// Runs only when the <c>SeedDemoData</c> setting is true <i>and</i> the app is in Development, and
/// <b>destroys all existing data</b> when it does, users included. Every start therefore lands on
/// the same known state rather than accumulating rows. Never point it at a database that matters.
/// </remarks>
public class DemoDataSeeder
{
    private const string Checking = "Everyday Checking";
    private const string Savings = "Household Savings";
    private const string CreditCard = "Rewards Card";

    private const string Groceries = "Groceries";
    private const string DiningOut = "Dining Out";
    private const string Utilities = "Utilities";
    private const string Transport = "Transport";
    private const string Entertainment = "Entertainment";
    private const string HomeMaintenance = "Home Maintenance";

    /// <summary>How many months of history to lay down, counting the current one.</summary>
    private const int MonthsOfHistory = 3;

    private static readonly (string Name, Accounts.AccountType Type)[] DemoAccounts =
    [
        (Checking, Accounts.AccountType.Checking),
        (Savings, Accounts.AccountType.Savings),
        (CreditCard, Accounts.AccountType.CreditCard),
    ];

    private static readonly (string Name, decimal MonthlyLimit)[] DemoBudgets =
    [
        (Groceries, 600.00m),
        (DiningOut, 200.00m),
        (Utilities, 250.00m),
        (Transport, 180.00m),
        (Entertainment, 120.00m),
        (HomeMaintenance, 150.00m),
    ];

    /// <summary>Amounts are signed the way the app expects: negative is money out.</summary>
    private record DemoTransaction(
        string Category, string Account, int Day, decimal Amount, string Description, Transactions.NeedWant NeedWant);

    /// <summary>The household's recurring shape, repeated in every seeded month.</summary>
    private static readonly DemoTransaction[] EveryMonth =
    [
        new(Groceries, Checking, 3, -142.18m, "Weekly shop — Fresh Market", Transactions.NeedWant.Need),
        new(Groceries, Checking, 10, -96.42m, "Weekly shop — Fresh Market", Transactions.NeedWant.Need),
        new(Groceries, Checking, 17, -118.75m, "Weekly shop — Fresh Market", Transactions.NeedWant.Need),
        new(Groceries, CreditCard, 24, -87.30m, "Corner store top-up", Transactions.NeedWant.Need),
        new(DiningOut, CreditCard, 6, -54.20m, "Pizza night", Transactions.NeedWant.Want),
        new(DiningOut, CreditCard, 14, -38.65m, "Coffee and brunch", Transactions.NeedWant.Want),
        new(Utilities, Checking, 5, -128.40m, "Electric bill", Transactions.NeedWant.Need),
        new(Utilities, Checking, 12, -64.75m, "Water and sewer", Transactions.NeedWant.Need),
        new(Transport, CreditCard, 2, -48.90m, "Fuel", Transactions.NeedWant.Need),
        new(Transport, CreditCard, 16, -52.10m, "Fuel", Transactions.NeedWant.Need),
        new(Entertainment, CreditCard, 8, -15.99m, "Streaming subscription", Transactions.NeedWant.Want),
        new(HomeMaintenance, Checking, 20, -74.25m, "Furnace filters and bulbs", Transactions.NeedWant.Need),
    ];

    /// <summary>
    /// One-off spending, keyed by how many months back it lands, so the months don't look identical:
    /// the current month runs Dining Out over its limit and carries a refund, and each earlier month
    /// has its own overspend for month-over-month comparison to bite on.
    /// </summary>
    private static readonly Dictionary<int, DemoTransaction[]> ExtrasByMonthsAgo = new()
    {
        [0] =
        [
            new(DiningOut, CreditCard, 18, -132.75m, "Anniversary dinner", Transactions.NeedWant.Want),
            new(Entertainment, CreditCard, 11, -22.50m, "Concert ticket", Transactions.NeedWant.Want),
            new(Entertainment, CreditCard, 21, 22.50m, "Refund — cancelled concert", Transactions.NeedWant.Want),
        ],
        [1] =
        [
            new(HomeMaintenance, Checking, 9, -310.00m, "Plumber call-out", Transactions.NeedWant.Need),
        ],
        [2] =
        [
            new(Transport, CreditCard, 22, -220.00m, "Tyre replacement", Transactions.NeedWant.Need),
        ],
    };

    private readonly BudgetDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<List<Identity.SeedUser>> _householdUsers;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        BudgetDbContext db,
        UserManager<ApplicationUser> userManager,
        IOptions<List<Identity.SeedUser>> householdUsers,
        ILogger<DemoDataSeeder> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _householdUsers = householdUsers ?? throw new ArgumentNullException(nameof(householdUsers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Deletes everything in the database, then rebuilds the household users and demo history.</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SeedDemoData is on: deleting all existing data before seeding the demo household.");
        await DeleteEverythingAsync(cancellationToken);

        // The wipe took the household users with it, so they come back before anything can be
        // attributed to them.
        await Identity.SeedUsersAsync(_userManager, _householdUsers.Value);

        var userIds = await _db.Users.OrderBy(u => u.UserName).Select(u => u.Id).ToListAsync(cancellationToken);
        if (userIds.Count == 0)
        {
            _logger.LogWarning("No household users are configured to attribute demo data to; nothing seeded.");
            return;
        }

        var accountIds = AddAccounts(userIds[0]);
        var categoryIds = AddCategories(userIds[0]);
        // Budgets and transactions reference these by id, so they have to exist first.
        await _db.SaveChangesAsync(cancellationToken);

        AddHistory(accountIds, categoryIds, userIds);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded demo data: {Accounts} accounts, {Categories} categories and {Months} months of budgets and transactions.",
            DemoAccounts.Length, DemoBudgets.Length, MonthsOfHistory);
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

    private Dictionary<string, Accounts.Account> AddAccounts(string ownerUserId)
    {
        var accounts = DemoAccounts.ToDictionary(
            a => a.Name,
            a => new Accounts.Account { Name = a.Name, Type = a.Type, CreatedByUserId = ownerUserId });
        _db.Accounts.AddRange(accounts.Values);
        return accounts;
    }

    private Dictionary<string, Categories.Category> AddCategories(string ownerUserId)
    {
        var categories = DemoBudgets.ToDictionary(
            b => b.Name,
            b => new Categories.Category { Name = b.Name, CreatedByUserId = ownerUserId });
        _db.Categories.AddRange(categories.Values);
        return categories;
    }

    private void AddHistory(
        Dictionary<string, Accounts.Account> accounts,
        Dictionary<string, Categories.Category> categories,
        List<string> userIds)
    {
        var currentMonth = DateOnly.FromDateTime(DateTime.Today);
        var enteredBy = 0;

        for (var monthsAgo = 0; monthsAgo < MonthsOfHistory; monthsAgo++)
        {
            var month = currentMonth.AddMonths(-monthsAgo);

            foreach (var (name, monthlyLimit) in DemoBudgets)
            {
                _db.Budgets.Add(new Budgets.Budget
                {
                    CategoryId = categories[name].Id,
                    Year = month.Year,
                    Month = month.Month,
                    Amount = monthlyLimit,
                });
            }

            var extras = ExtrasByMonthsAgo.GetValueOrDefault(monthsAgo, []);
            foreach (var demo in EveryMonth.Concat(extras))
            {
                _db.Transactions.Add(new Transactions.Transaction
                {
                    AccountId = accounts[demo.Account].Id,
                    CategoryId = categories[demo.Category].Id,
                    Date = new DateOnly(month.Year, month.Month, demo.Day),
                    Amount = demo.Amount,
                    Description = demo.Description,
                    NeedWant = demo.NeedWant,
                    // Alternate the two household members so "who entered what" has something to show.
                    CreatedByUserId = userIds[enteredBy++ % userIds.Count],
                });
            }
        }
    }
}

public static class DemoDataSeederExtensions
{
    public static IServiceCollection AddDemoDataSeeder(this IServiceCollection services) => services
        .AddScoped<DemoDataSeeder>();

    /// <summary>
    /// Resets the database to the demo state if the <c>SeedDemoData</c> setting is on and the app is
    /// running in Development; otherwise does nothing.
    /// </summary>
    public static async Task SeedDemoDataAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("SeedDemoData"))
        {
            return;
        }

        if (!app.Environment.IsDevelopment())
        {
            // Refused rather than obeyed: this setting destroys every row, and nothing outside a
            // developer's own machine should be able to ask for that by flipping a flag.
            app.Logger.LogWarning(
                "SeedDemoData is on but the environment is {Environment}, not Development; the database was left untouched.",
                app.Environment.EnvironmentName);
            return;
        }

        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
    }
}
