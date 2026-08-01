using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Transactions.Transactions;

namespace Api.Tests.Features.Transactions;

public class FetchAllTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public FetchAllTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FetchAll_ReturnsCreatedTransactions()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var accountId = await TransactionsTestHelpers.CreateAccountAsync(client, "FetchAll Account");
        var categoryId = await TransactionsTestHelpers.CreateCategoryAsync(client, "FetchAll Category");
        var created = await CreateTransactionAsync(client, accountId, categoryId, new DateOnly(2026, 1, 10), NeedWant.Need);

        var list = await client.GetFromJsonAsync<List<FetchAll.Response>>(
            $"/api/transactions?accountId={accountId}", TestClientExtensions.JsonOptions);

        Assert.NotNull(list);
        Assert.Contains(list!, t => t.Id == created.Id);
    }

    [Fact]
    public async Task FetchAll_FilteredByCategoryAndNeedWant_ReturnsOnlyMatchingTransactions()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var accountId = await TransactionsTestHelpers.CreateAccountAsync(client, "Filter Account");
        var matchingCategoryId = await TransactionsTestHelpers.CreateCategoryAsync(client, "Filter Matching Category");
        var otherCategoryId = await TransactionsTestHelpers.CreateCategoryAsync(client, "Filter Other Category");

        var matching = await CreateTransactionAsync(client, accountId, matchingCategoryId, new DateOnly(2026, 3, 1), NeedWant.Want);
        await CreateTransactionAsync(client, accountId, matchingCategoryId, new DateOnly(2026, 3, 1), NeedWant.Need);
        await CreateTransactionAsync(client, accountId, otherCategoryId, new DateOnly(2026, 3, 1), NeedWant.Want);

        var list = await client.GetFromJsonAsync<List<FetchAll.Response>>(
            $"/api/transactions?categoryId={matchingCategoryId}&needWant=Want", TestClientExtensions.JsonOptions);

        Assert.NotNull(list);
        Assert.All(list!, t => Assert.Equal(matchingCategoryId, t.CategoryId));
        Assert.All(list!, t => Assert.Equal(NeedWant.Want, t.NeedWant));
        Assert.Contains(list!, t => t.Id == matching.Id);
    }

    [Fact]
    public async Task FetchAll_FilteredByDateRange_ExcludesTransactionsOutsideRange()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var accountId = await TransactionsTestHelpers.CreateAccountAsync(client, "Date Range Account");
        var categoryId = await TransactionsTestHelpers.CreateCategoryAsync(client, "Date Range Category");

        var inRange = await CreateTransactionAsync(client, accountId, categoryId, new DateOnly(2026, 5, 15), NeedWant.Need);
        var beforeRange = await CreateTransactionAsync(client, accountId, categoryId, new DateOnly(2026, 4, 1), NeedWant.Need);
        var afterRange = await CreateTransactionAsync(client, accountId, categoryId, new DateOnly(2026, 6, 1), NeedWant.Need);

        var list = await client.GetFromJsonAsync<List<FetchAll.Response>>(
            $"/api/transactions?accountId={accountId}&dateFrom=2026-05-01&dateTo=2026-05-31", TestClientExtensions.JsonOptions);

        Assert.NotNull(list);
        Assert.Contains(list!, t => t.Id == inRange.Id);
        Assert.DoesNotContain(list!, t => t.Id == beforeRange.Id);
        Assert.DoesNotContain(list!, t => t.Id == afterRange.Id);
    }

    private static async Task<Create.Response> CreateTransactionAsync(
        HttpClient client, int accountId, int categoryId, DateOnly date, NeedWant needWant)
    {
        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new Create.Command(accountId, categoryId, date, -10m, "Filter fixture", needWant),
            TestClientExtensions.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Create.Response>(TestClientExtensions.JsonOptions))!;
    }
}
