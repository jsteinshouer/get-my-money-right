using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Budgets.Budgets;

namespace Api.Tests.Features.Budgets;

public class UpdateTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public UpdateTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Update_WithKnownId_ChangesAmount()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var categoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Update Target Category");
        var created = await client.PostAsJsonAsync("/api/budgets", new Create.Command(categoryId, 2026, 8, 400.00m));
        var createdBudget = (await created.Content.ReadFromJsonAsync<Create.Response>())!;

        var response = await client.PutAsJsonAsync(
            $"/api/budgets/{createdBudget.Id}", new Update.Command(categoryId, 2026, 8, 500.00m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Update.Response>();
        Assert.Equal(500.00m, body!.Amount);
    }

    [Fact]
    public async Task Update_WithUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var categoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Unknown Budget Category");

        var response = await client.PutAsJsonAsync("/api/budgets/999999", new Update.Command(categoryId, 2026, 8, 400.00m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ToCategoryAndMonthUsedByAnotherBudget_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var categoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Conflict Update Category");
        await client.PostAsJsonAsync("/api/budgets", new Create.Command(categoryId, 2026, 9, 400.00m));
        var created = await client.PostAsJsonAsync("/api/budgets", new Create.Command(categoryId, 2026, 10, 400.00m));
        var createdBudget = (await created.Content.ReadFromJsonAsync<Create.Response>())!;

        var response = await client.PutAsJsonAsync(
            $"/api/budgets/{createdBudget.Id}", new Update.Command(categoryId, 2026, 9, 400.00m));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
