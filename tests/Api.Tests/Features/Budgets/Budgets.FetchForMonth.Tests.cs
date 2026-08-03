using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Budgets.Budgets;

namespace Api.Tests.Features.Budgets;

public class FetchForMonthTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public FetchForMonthTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FetchForMonth_ReturnsOnlyBudgetsForThatYearAndMonth()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var categoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Fetch For Month Category");
        var otherCategoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Fetch For Month Other Category");

        var inMonth = await client.PostAsJsonAsync("/api/budgets", new Create.Command(categoryId, 2026, 5, 300.00m));
        var inMonthBudget = (await inMonth.Content.ReadFromJsonAsync<Create.Response>())!;
        await client.PostAsJsonAsync("/api/budgets", new Create.Command(otherCategoryId, 2026, 6, 150.00m));

        var response = await client.GetFromJsonAsync<List<FetchForMonth.Response>>("/api/budgets?year=2026&month=5");

        Assert.NotNull(response);
        var match = Assert.Single(response!, b => b.Id == inMonthBudget.Id);
        Assert.Equal(categoryId, match.CategoryId);
        Assert.Equal(300.00m, match.Amount);
        Assert.DoesNotContain(response!, b => b.CategoryId == otherCategoryId);
    }

    [Fact]
    public async Task FetchForMonth_ReportsActualSpendFromTransactionsInThatMonthAndCategory()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var categoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Actual Spend Category");
        var otherCategoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Actual Spend Other Category");
        var accountId = await BudgetsTestHelpers.CreateAccountAsync(client, "Actual Spend Account");

        await client.PostAsJsonAsync("/api/budgets", new Create.Command(categoryId, 2026, 7, 300.00m));

        // Counted: two spends inside the budgeted month and category.
        await BudgetsTestHelpers.CreateTransactionAsync(client, accountId, categoryId, new DateOnly(2026, 7, 1), -120.50m);
        await BudgetsTestHelpers.CreateTransactionAsync(client, accountId, categoryId, new DateOnly(2026, 7, 31), -40.00m);
        // Not counted: right category, adjacent months.
        await BudgetsTestHelpers.CreateTransactionAsync(client, accountId, categoryId, new DateOnly(2026, 6, 30), -999.00m);
        await BudgetsTestHelpers.CreateTransactionAsync(client, accountId, categoryId, new DateOnly(2026, 8, 1), -999.00m);
        // Not counted: right month, different category.
        await BudgetsTestHelpers.CreateTransactionAsync(client, accountId, otherCategoryId, new DateOnly(2026, 7, 15), -50.00m);

        var response = await client.GetFromJsonAsync<List<FetchForMonth.Response>>("/api/budgets?year=2026&month=7");

        var match = Assert.Single(response!, b => b.CategoryId == categoryId);
        Assert.Equal(300.00m, match.Amount);
        Assert.Equal(160.50m, match.Actual);
    }

    [Fact]
    public async Task FetchForMonth_TreatsMoneyInAsReducingActualSpend()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var categoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Refund Category");
        var accountId = await BudgetsTestHelpers.CreateAccountAsync(client, "Refund Account");

        await client.PostAsJsonAsync("/api/budgets", new Create.Command(categoryId, 2026, 9, 200.00m));
        await BudgetsTestHelpers.CreateTransactionAsync(client, accountId, categoryId, new DateOnly(2026, 9, 3), -100.00m);
        await BudgetsTestHelpers.CreateTransactionAsync(client, accountId, categoryId, new DateOnly(2026, 9, 4), 25.00m);

        var response = await client.GetFromJsonAsync<List<FetchForMonth.Response>>("/api/budgets?year=2026&month=9");

        var match = Assert.Single(response!, b => b.CategoryId == categoryId);
        Assert.Equal(75.00m, match.Actual);
    }

    [Fact]
    public async Task FetchForMonth_WithNoTransactions_ReportsZeroActualSpend()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var categoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Unspent Category");

        await client.PostAsJsonAsync("/api/budgets", new Create.Command(categoryId, 2026, 10, 80.00m));

        var response = await client.GetFromJsonAsync<List<FetchForMonth.Response>>("/api/budgets?year=2026&month=10");

        var match = Assert.Single(response!, b => b.CategoryId == categoryId);
        Assert.Equal(0m, match.Actual);
    }
}
