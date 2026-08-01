using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Transactions.Transactions;

namespace Api.Tests.Features.Transactions;

public class CreateTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public CreateTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithValidCommand_ReturnsCreatedWithTransaction()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var accountId = await TransactionsTestHelpers.CreateAccountAsync(client, "Create Test Account");
        var categoryId = await TransactionsTestHelpers.CreateCategoryAsync(client, "Create Test Category");

        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new Create.Command(accountId, categoryId, new DateOnly(2026, 1, 15), -42.50m, "Groceries run", NeedWant.Need),
            TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Create.Response>(TestClientExtensions.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(accountId, body!.AccountId);
        Assert.Equal(categoryId, body.CategoryId);
        Assert.Equal(new DateOnly(2026, 1, 15), body.Date);
        Assert.Equal(-42.50m, body.Amount);
        Assert.Equal("Groceries run", body.Description);
        Assert.Equal(NeedWant.Need, body.NeedWant);
        Assert.Equal($"/api/transactions/{body.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Create_WithoutSession_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new Create.Command(1, 1, new DateOnly(2026, 1, 1), -10m, "No Session", NeedWant.Want),
            TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutNeedWant_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var accountId = await TransactionsTestHelpers.CreateAccountAsync(client, "Missing NeedWant Account");
        var categoryId = await TransactionsTestHelpers.CreateCategoryAsync(client, "Missing NeedWant Category");

        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new Create.Command(accountId, categoryId, new DateOnly(2026, 1, 15), -10m, "Missing classification", null),
            TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnknownAccount_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var categoryId = await TransactionsTestHelpers.CreateCategoryAsync(client, "Unknown Account Category");

        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new Create.Command(999999, categoryId, new DateOnly(2026, 1, 15), -10m, "Bad account", NeedWant.Want),
            TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
