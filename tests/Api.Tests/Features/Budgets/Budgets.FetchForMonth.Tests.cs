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
}
