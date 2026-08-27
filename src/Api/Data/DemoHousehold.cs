using AccountsFeature = Api.Features.Accounts.Accounts;
using TransactionsFeature = Api.Features.Transactions.Transactions;

namespace Api.Data;

/// <summary>
/// The fictional household <see cref="DemoDataSeeder"/> lays down: what it banks with, what it
/// budgets for, and what it spends. Content only — the seeder owns how any of it reaches the
/// database, so the two change for different reasons and live in different files.
/// </summary>
public static class DemoHousehold
{
    public const string Checking = "Everyday Checking";
    public const string Savings = "Household Savings";
    public const string CreditCard = "Rewards Card";

    public const string Groceries = "Groceries";
    public const string DiningOut = "Dining Out";
    public const string Utilities = "Utilities";
    public const string Transport = "Transport";
    public const string Entertainment = "Entertainment";
    public const string HomeMaintenance = "Home Maintenance";

    /// <summary>How many months of history to lay down, counting the current one.</summary>
    public const int MonthsOfHistory = 3;

    public static readonly (string Name, AccountsFeature.AccountType Type)[] Accounts =
    [
        (Checking, AccountsFeature.AccountType.Checking),
        (Savings, AccountsFeature.AccountType.Savings),
        (CreditCard, AccountsFeature.AccountType.CreditCard),
    ];

    public static readonly (string Name, decimal MonthlyLimit)[] Budgets =
    [
        (Groceries, 600.00m),
        (DiningOut, 200.00m),
        (Utilities, 250.00m),
        (Transport, 180.00m),
        (Entertainment, 120.00m),
        (HomeMaintenance, 150.00m),
    ];

    /// <summary>Amounts are signed the way the app expects: negative is money out.</summary>
    public record Spend(
        string Category, string Account, int Day, decimal Amount, string Description, TransactionsFeature.NeedWant NeedWant);

    /// <summary>The household's recurring shape, repeated in every seeded month.</summary>
    public static readonly Spend[] EveryMonth =
    [
        new(Groceries, Checking, 3, -142.18m, "Weekly shop — Fresh Market", TransactionsFeature.NeedWant.Need),
        new(Groceries, Checking, 10, -96.42m, "Weekly shop — Fresh Market", TransactionsFeature.NeedWant.Need),
        new(Groceries, Checking, 17, -118.75m, "Weekly shop — Fresh Market", TransactionsFeature.NeedWant.Need),
        new(Groceries, CreditCard, 24, -87.30m, "Corner store top-up", TransactionsFeature.NeedWant.Need),
        new(DiningOut, CreditCard, 6, -54.20m, "Pizza night", TransactionsFeature.NeedWant.Want),
        new(DiningOut, CreditCard, 14, -38.65m, "Coffee and brunch", TransactionsFeature.NeedWant.Want),
        new(Utilities, Checking, 5, -128.40m, "Electric bill", TransactionsFeature.NeedWant.Need),
        new(Utilities, Checking, 12, -64.75m, "Water and sewer", TransactionsFeature.NeedWant.Need),
        new(Transport, CreditCard, 2, -48.90m, "Fuel", TransactionsFeature.NeedWant.Need),
        new(Transport, CreditCard, 16, -52.10m, "Fuel", TransactionsFeature.NeedWant.Need),
        new(Entertainment, CreditCard, 8, -15.99m, "Streaming subscription", TransactionsFeature.NeedWant.Want),
        new(HomeMaintenance, Checking, 20, -74.25m, "Furnace filters and bulbs", TransactionsFeature.NeedWant.Need),
    ];

    /// <summary>
    /// One-off spending, keyed by how many months back it lands, so the months don't look identical:
    /// the most recent full month runs Dining Out over its limit and carries a refund, and each
    /// earlier month has its own overspend for month-over-month comparison to bite on.
    /// </summary>
    public static readonly Dictionary<int, Spend[]> ExtrasByMonthsAgo = new()
    {
        [0] =
        [
            new(DiningOut, CreditCard, 18, -132.75m, "Anniversary dinner", TransactionsFeature.NeedWant.Want),
            new(Entertainment, CreditCard, 11, -22.50m, "Concert ticket", TransactionsFeature.NeedWant.Want),
            new(Entertainment, CreditCard, 21, 22.50m, "Refund — cancelled concert", TransactionsFeature.NeedWant.Want),
        ],
        [1] =
        [
            new(HomeMaintenance, Checking, 9, -310.00m, "Plumber call-out", TransactionsFeature.NeedWant.Need),
        ],
        [2] =
        [
            new(Transport, CreditCard, 22, -220.00m, "Tyre replacement", TransactionsFeature.NeedWant.Need),
        ],
    };

    public static IEnumerable<Spend> ForMonthsAgo(int monthsAgo) =>
        EveryMonth.Concat(ExtrasByMonthsAgo.GetValueOrDefault(monthsAgo, []));
}
