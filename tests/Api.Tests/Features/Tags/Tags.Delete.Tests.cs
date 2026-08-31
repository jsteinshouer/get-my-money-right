using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Tags.Tags;

namespace Api.Tests.Features.Tags;

public class DeleteTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public DeleteTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_WithKnownId_RemovesTag()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var tagId = await TagsTestHelpers.CreateTagAsync(client, "Delete Target Tag");

        var response = await client.DeleteAsync($"/api/tags/{tagId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var list = await client.GetFromJsonAsync<List<FetchAll.Response>>("/api/tags");
        Assert.DoesNotContain(list!, t => t.Id == tagId);
    }

    [Fact]
    public async Task Delete_WithUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.DeleteAsync("/api/tags/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithAssignedTransaction_SucceedsAndLeavesTheTransaction()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var tagId = await TagsTestHelpers.CreateTagAsync(client, "Delete Assigned Tag");
        var transactionId = await TagsTestHelpers.CreateTransactionAsync(client, "Delete Assigned");
        (await client.PutAsync($"/api/transactions/{transactionId}/tags/{tagId}", null)).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/tags/{tagId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var transactions = await client.GetFromJsonAsync<List<Api.Features.Transactions.Transactions.FetchAll.Response>>(
            "/api/transactions", TestClientExtensions.JsonOptions);
        var transaction = Assert.Single(transactions!.Where(t => t.Id == transactionId));
        Assert.Empty(transaction.TagIds);
    }
}
