using System.Net.Http.Json;
using Api.Data;
using Api.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using static Api.Features.Budgets.Budgets;
using CategoriesFeature = Api.Features.Categories.Categories;
using TransactionsFeature = Api.Features.Transactions.Transactions;

namespace Api.Tests.Data;

public class DemoDataSeederTests : IClassFixture<BudgetApiFactory>
{
    /// <summary>
    /// Pinned, and deliberately mid-month: the demo history runs to day 24, so seeding "as at" the
    /// 10th leaves part of the month in the future and every month boundary is stated, not inferred.
    /// </summary>
    private static readonly DateOnly Today = new(2026, 3, 10);

    private readonly BudgetApiFactory _factory;

    public DemoDataSeederTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reset_SeedsSpendAgainstEveryBudgetedCategory()
    {
        var client = await ResetAndLogInAsync();

        // The most recent complete month: the current one is only seeded as far as today.
        var budgets = await FetchBudgetsAsync(client, Today.AddMonths(-1));

        Assert.NotEmpty(budgets);
        Assert.All(budgets, budget => Assert.True(budget.Amount > 0, $"category {budget.CategoryId} has no limit"));
        Assert.All(budgets, budget => Assert.True(budget.Actual > 0, $"category {budget.CategoryId} has no spend"));
    }

    [Fact]
    public async Task Reset_IncludesAnOverspentCategory()
    {
        var client = await ResetAndLogInAsync();

        var budgets = await FetchBudgetsAsync(client, Today.AddMonths(-1));

        Assert.Contains(budgets, budget => budget.Actual > budget.Amount);
    }

    [Fact]
    public async Task Reset_CoversEarlierMonthsSoMonthsCanBeCompared()
    {
        var client = await ResetAndLogInAsync();

        var lastMonth = await FetchBudgetsAsync(client, Today.AddMonths(-1));
        var monthBefore = await FetchBudgetsAsync(client, Today.AddMonths(-2));

        Assert.Equal(lastMonth.Count, monthBefore.Count);
        Assert.All(monthBefore, budget => Assert.True(budget.Actual > 0));
        // Identical months would make a trend report meaningless.
        Assert.NotEqual(lastMonth.Sum(b => b.Actual), monthBefore.Sum(b => b.Actual));
    }

    [Fact]
    public async Task Reset_SeedsNoSpendDatedInTheFuture()
    {
        var client = await ResetAndLogInAsync();

        var transactions = await FetchTransactionsAsync(client);

        Assert.NotEmpty(transactions);
        Assert.All(transactions, transaction => Assert.True(
            transaction.Date <= Today, $"transaction {transaction.Id} is dated {transaction.Date}, in the future"));
        // The clock is mid-month, so the current month must be genuinely truncated rather than
        // trivially satisfied by a date that happens to be past the end of the demo history.
        var currentMonth = transactions.Where(t => t.Date.Year == Today.Year && t.Date.Month == Today.Month).ToList();
        Assert.NotEmpty(currentMonth);
        Assert.True(currentMonth.Count < transactions.Count(t => t.Date.Month == Today.AddMonths(-1).Month),
            "the current month should hold less spend than a complete month");
    }

    [Fact]
    public async Task Reset_DeletesDataThatWasAlreadyThere()
    {
        var client = await ResetAndLogInAsync();
        var strayCategory = $"Stray Category {Guid.NewGuid()}";
        (await client.PostAsJsonAsync("/api/categories", new CategoriesFeature.Create.Command(strayCategory)))
            .EnsureSuccessStatusCode();

        var reseededClient = await ResetAndLogInAsync();

        var categories = await reseededClient.GetFromJsonAsync<List<CategoriesFeature.FetchAll.Response>>("/api/categories");
        Assert.DoesNotContain(categories!, category => category.Name == strayCategory);
    }

    [Fact]
    public async Task Reset_DeletesUsersThatWereAlreadyThere()
    {
        await ResetAsync();
        var strayEmail = $"stray-{Guid.NewGuid():N}@household.local";
        await CreateUserAsync(strayEmail);

        await ResetAsync();

        Assert.Null(await FindUserAsync(strayEmail));
    }

    [Fact]
    public async Task Reset_LeavesTheHouseholdUsersAbleToLogIn()
    {
        await ResetAsync();

        var client = _factory.CreateClient();
        // The second user, so this can't pass on the strength of the first one alone.
        await client.LoginAsync(BudgetApiFactory.SeededUser2Email, BudgetApiFactory.SeededUser2Password);
    }

    private async Task ResetAsync()
    {
        using var scope = _factory.Services.CreateScope();
        // Driven through DI rather than HTTP: resetting the database has no endpoint by design.
        Assert.True(await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().ResetToDemoStateAsync(Today));
    }

    private async Task<HttpClient> ResetAndLogInAsync()
    {
        await ResetAsync();
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        return client;
    }

    private async Task CreateUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await userManager.CreateAsync(
            new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = "Stray" },
            "Stray123!Password");
        Assert.True(result.Succeeded);
    }

    private async Task<ApplicationUser?> FindUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email);
    }

    private static async Task<List<FetchForMonth.Response>> FetchBudgetsAsync(HttpClient client, DateOnly month) =>
        (await client.GetFromJsonAsync<List<FetchForMonth.Response>>($"/api/budgets?year={month.Year}&month={month.Month}"))!;

    private static async Task<List<TransactionsFeature.FetchAll.Response>> FetchTransactionsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<TransactionsFeature.FetchAll.Response>>(
            "/api/transactions", TestClientExtensions.JsonOptions))!;
}

public class DemoDataSeederOutsideDevelopmentTests : IClassFixture<DeployedEnvironmentApiFactory>
{
    private readonly DeployedEnvironmentApiFactory _factory;

    public DemoDataSeederOutsideDevelopmentTests(DeployedEnvironmentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reset_OutsideDevelopment_IsRefusedAndLeavesTheDataAlone()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var realCategory = $"Real Category {Guid.NewGuid()}";
        (await client.PostAsJsonAsync("/api/categories", new CategoriesFeature.Create.Command(realCategory)))
            .EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var ran = await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().ResetToDemoStateAsync();

        Assert.False(ran);
        var categories = await client.GetFromJsonAsync<List<CategoriesFeature.FetchAll.Response>>("/api/categories");
        // Still logged in, still holding the row that was there — nothing was wiped or seeded.
        Assert.Contains(categories!, category => category.Name == realCategory);
        Assert.DoesNotContain(categories!, category => category.Name == DemoHousehold.Groceries);
    }
}
