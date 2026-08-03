using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Budgets.Budgets;

namespace Api.Tests.Features.Budgets;

public class CreateTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public CreateTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithValidCommand_ReturnsCreatedWithBudget()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var categoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Create Test Category");

        var response = await client.PostAsJsonAsync("/api/budgets", new Create.Command(categoryId, 2026, 8, 400.00m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Create.Response>();
        Assert.NotNull(body);
        Assert.Equal(categoryId, body!.CategoryId);
        Assert.Equal(2026, body.Year);
        Assert.Equal(8, body.Month);
        Assert.Equal(400.00m, body.Amount);
        Assert.Equal($"/api/budgets/{body.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Create_WithoutSession_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/budgets", new Create.Command(1, 2026, 8, 400.00m));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidMonth_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var categoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Invalid Month Category");

        var response = await client.PostAsJsonAsync("/api/budgets", new Create.Command(categoryId, 2026, 13, 400.00m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnknownCategory_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.PostAsJsonAsync("/api/budgets", new Create.Command(999999, 2026, 8, 400.00m));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_SecondBudgetForSameCategoryAndMonth_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var categoryId = await BudgetsTestHelpers.CreateCategoryAsync(client, "Duplicate Budget Category");
        await client.PostAsJsonAsync("/api/budgets", new Create.Command(categoryId, 2026, 8, 400.00m));

        var response = await client.PostAsJsonAsync("/api/budgets", new Create.Command(categoryId, 2026, 8, 500.00m));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
