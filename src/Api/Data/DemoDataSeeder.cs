using Api.Features.Accounts;
using Api.Features.Budgets;
using Api.Features.Categories;
using Api.Features.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

/// <summary>
/// Fills a development database with a plausible three-month household history — accounts,
/// categories, monthly budgets and the transactions that spend against them — so the UI has
/// something to show without hand-entering it.
/// </summary>
/// <remarks>
/// Runs only when the <c>SeedDemoData</c> setting is true, and re-running is a no-op once the
/// demo accounts exist, so restarting the app never duplicates rows or disturbs real data.
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
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(BudgetDbContext db, ILogger<DemoDataSeeder> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Accounts.AnyAsync(a => a.Name == Checking, cancellationToken))
        {
            _logger.LogInformation("Demo data is already present; nothing seeded.");
            return;
        }

        var userIds = await _db.Users.OrderBy(u => u.UserName).Select(u => u.Id).ToListAsync(cancellationToken);
        if (userIds.Count == 0)
        {
            _logger.LogWarning("No household users exist to attribute demo data to; nothing seeded.");
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

    /// <summary>Seeds demo data if the <c>SeedDemoData</c> setting is on; otherwise does nothing.</summary>
    public static async Task SeedDemoDataAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("SeedDemoData"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
    }
}
