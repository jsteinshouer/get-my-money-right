using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Transactions.Transactions;

namespace Api.Tests.Features.Transactions;

public class UpdateTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public UpdateTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Update_WithKnownId_ChangesFields()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var accountId = await TransactionsTestHelpers.CreateAccountAsync(client, "Update Account");
        var categoryId = await TransactionsTestHelpers.CreateCategoryAsync(client, "Update Category");
        var otherAccountId = await TransactionsTestHelpers.CreateAccountAsync(client, "Update Other Account");
        var otherCategoryId = await TransactionsTestHelpers.CreateCategoryAsync(client, "Update Other Category");
        var created = await client.PostAsJsonAsync(
            "/api/transactions",
            new Create.Command(accountId, categoryId, new DateOnly(2026, 1, 1), -10m, "Original", NeedWant.Need),
            TestClientExtensions.JsonOptions);
        var createdTransaction = (await created.Content.ReadFromJsonAsync<Create.Response>(TestClientExtensions.JsonOptions))!;

        var response = await client.PutAsJsonAsync(
            $"/api/transactions/{createdTransaction.Id}",
            new Update.Command(otherAccountId, otherCategoryId, new DateOnly(2026, 2, 2), -99.99m, "Updated", NeedWant.Want),
            TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Update.Response>(TestClientExtensions.JsonOptions);
        Assert.Equal(otherAccountId, body!.AccountId);
        Assert.Equal(otherCategoryId, body.CategoryId);
        Assert.Equal(new DateOnly(2026, 2, 2), body.Date);
        Assert.Equal(-99.99m, body.Amount);
        Assert.Equal("Updated", body.Description);
        Assert.Equal(NeedWant.Want, body.NeedWant);
    }

    [Fact]
    public async Task Update_WithUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.PutAsJsonAsync(
            "/api/transactions/999999",
            new Update.Command(1, 1, new DateOnly(2026, 1, 1), -10m, "Doesn't matter", NeedWant.Need),
            TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithoutNeedWant_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var accountId = await TransactionsTestHelpers.CreateAccountAsync(client, "Update Missing NeedWant Account");
        var categoryId = await TransactionsTestHelpers.CreateCategoryAsync(client, "Update Missing NeedWant Category");
        var created = await client.PostAsJsonAsync(
            "/api/transactions",
            new Create.Command(accountId, categoryId, new DateOnly(2026, 1, 1), -10m, "Original", NeedWant.Need),
            TestClientExtensions.JsonOptions);
        var createdTransaction = (await created.Content.ReadFromJsonAsync<Create.Response>(TestClientExtensions.JsonOptions))!;

        var response = await client.PutAsJsonAsync(
            $"/api/transactions/{createdTransaction.Id}",
            new Update.Command(accountId, categoryId, new DateOnly(2026, 1, 1), -10m, "Original", null),
            TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
