using System.Net.Http.Json;
using Api.Data;
using Api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static Api.Features.Budgets.Budgets;
using CategoriesFeature = Api.Features.Categories.Categories;
using IdentityFeature = Api.Features.Identity.Identity;
using TransactionsFeature = Api.Features.Transactions.Transactions;

namespace Api.Tests.Data;

/// <summary>The only factory that turns demo seeding on; every other test suite runs without it.</summary>
public class DemoDataApiFactory : BudgetApiFactory
{
    protected override bool SeedDemoData => true;
}

public class DemoDataSeederTests : IClassFixture<DemoDataApiFactory>
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    private readonly DemoDataApiFactory _factory;

    public DemoDataSeederTests(DemoDataApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeededCurrentMonth_HasRealSpendAgainstEveryBudgetedCategory()
    {
        var client = await LoggedInClientAsync();

        var budgets = await FetchBudgetsAsync(client, Today);

        Assert.NotEmpty(budgets);
        Assert.All(budgets, budget => Assert.True(budget.Amount > 0, $"category {budget.CategoryId} has no limit"));
        Assert.All(budgets, budget => Assert.True(budget.Actual > 0, $"category {budget.CategoryId} has no spend"));
    }

    [Fact]
    public async Task SeededCurrentMonth_IncludesAnOverspentCategory()
    {
        var client = await LoggedInClientAsync();

        var budgets = await FetchBudgetsAsync(client, Today);

        Assert.Contains(budgets, budget => budget.Actual > budget.Amount);
    }

    [Fact]
    public async Task SeededHistory_CoversEarlierMonthsSoMonthsCanBeCompared()
    {
        var client = await LoggedInClientAsync();

        var thisMonth = await FetchBudgetsAsync(client, Today);
        var lastMonth = await FetchBudgetsAsync(client, Today.AddMonths(-1));
        var monthBefore = await FetchBudgetsAsync(client, Today.AddMonths(-2));

        Assert.Equal(thisMonth.Count, lastMonth.Count);
        Assert.Equal(thisMonth.Count, monthBefore.Count);
        Assert.All(lastMonth, budget => Assert.True(budget.Actual > 0));
        Assert.All(monthBefore, budget => Assert.True(budget.Actual > 0));
        // Identical months would make a trend report meaningless.
        Assert.NotEqual(thisMonth.Sum(b => b.Actual), lastMonth.Sum(b => b.Actual));
    }

    [Fact]
    public async Task SeedingAgain_WipesWhatWasThereAndRebuildsTheSameDemoData()
    {
        var client = await LoggedInClientAsync();
        var before = await FetchTransactionsAsync(client);
        var strayCategory = $"Stray Category {Guid.NewGuid()}";
        (await client.PostAsJsonAsync("/api/categories", new CategoriesFeature.Create.Command(strayCategory)))
            .EnsureSuccessStatusCode();

        await ReseedAsync();

        // The wipe takes the users with it, so the old session is gone along with the old rows.
        var reseededClient = await LoggedInClientAsync();
        var categories = await reseededClient.GetFromJsonAsync<List<CategoriesFeature.FetchAll.Response>>("/api/categories");
        Assert.DoesNotContain(categories!, category => category.Name == strayCategory);
        Assert.Equal(before.Count, (await FetchTransactionsAsync(reseededClient)).Count);
    }

    [Fact]
    public async Task SeedingAgain_LeavesTheHouseholdUsersAbleToLogIn()
    {
        await ReseedAsync();

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/identity/login", new IdentityFeature.Login.Command(BudgetApiFactory.SeededUser2Email, BudgetApiFactory.SeededUser2Password));

        response.EnsureSuccessStatusCode();
    }

    private async Task ReseedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
    }

    private async Task<HttpClient> LoggedInClientAsync()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        return client;
    }

    private static async Task<List<FetchForMonth.Response>> FetchBudgetsAsync(HttpClient client, DateOnly month) =>
        (await client.GetFromJsonAsync<List<FetchForMonth.Response>>($"/api/budgets?year={month.Year}&month={month.Month}"))!;

    private static async Task<List<TransactionsFeature.FetchAll.Response>> FetchTransactionsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<TransactionsFeature.FetchAll.Response>>(
            "/api/transactions", TestClientExtensions.JsonOptions))!;
}
