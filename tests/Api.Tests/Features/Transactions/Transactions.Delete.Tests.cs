using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Transactions.Transactions;

namespace Api.Tests.Features.Transactions;

public class DeleteTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public DeleteTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_WithKnownId_RemovesTransaction()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var accountId = await TransactionsTestHelpers.CreateAccountAsync(client, "Delete Account");
        var categoryId = await TransactionsTestHelpers.CreateCategoryAsync(client, "Delete Category");
        var created = await client.PostAsJsonAsync(
            "/api/transactions",
            new Create.Command(accountId, categoryId, new DateOnly(2026, 1, 1), -10m, "Delete target", NeedWant.Need),
            TestClientExtensions.JsonOptions);
        var createdTransaction = (await created.Content.ReadFromJsonAsync<Create.Response>(TestClientExtensions.JsonOptions))!;

        var response = await client.DeleteAsync($"/api/transactions/{createdTransaction.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var list = await client.GetFromJsonAsync<List<FetchAll.Response>>(
            $"/api/transactions?accountId={accountId}", TestClientExtensions.JsonOptions);
        Assert.DoesNotContain(list!, t => t.Id == createdTransaction.Id);
    }

    [Fact]
    public async Task Delete_WithUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.DeleteAsync("/api/transactions/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
