using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Budgets.Budgets;

namespace Api.Tests.Features.Budgets;

public class DeleteTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public DeleteTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_WithKnownId_RemovesBudget()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var categoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Delete Target Category");
        var created = await client.PostAsJsonAsync("/api/budgets", new Create.Command(categoryId, 2026, 8, 400.00m));
        var createdBudget = (await created.Content.ReadFromJsonAsync<Create.Response>())!;

        var response = await client.DeleteAsync($"/api/budgets/{createdBudget.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var refetched = await client.GetFromJsonAsync<List<FetchForMonth.Response>>("/api/budgets?year=2026&month=8");
        Assert.DoesNotContain(refetched!, b => b.Id == createdBudget.Id);
    }

    [Fact]
    public async Task Delete_WithUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.DeleteAsync("/api/budgets/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
