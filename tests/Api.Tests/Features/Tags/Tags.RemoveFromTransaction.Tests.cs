using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using TransactionsFeature = Api.Features.Transactions.Transactions;

namespace Api.Tests.Features.Tags;

public class RemoveFromTransactionTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public RemoveFromTransactionTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Remove_WithAssignedTag_DetachesItAndLeavesTheOthers()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var removedTagId = await TagsTestHelpers.CreateTagAsync(client, "Remove Target Tag");
        var keptTagId = await TagsTestHelpers.CreateTagAsync(client, "Remove Kept Tag");
        var transactionId = await TagsTestHelpers.CreateTransactionAsync(client, "Remove");
        (await client.PutAsync($"/api/transactions/{transactionId}/tags/{removedTagId}", null)).EnsureSuccessStatusCode();
        (await client.PutAsync($"/api/transactions/{transactionId}/tags/{keptTagId}", null)).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/transactions/{transactionId}/tags/{removedTagId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var list = await client.GetFromJsonAsync<List<TransactionsFeature.FetchAll.Response>>(
            "/api/transactions", TestClientExtensions.JsonOptions);
        var transaction = Assert.Single(list!.Where(t => t.Id == transactionId));
        Assert.DoesNotContain(removedTagId, transaction.TagIds);
        Assert.Contains(keptTagId, transaction.TagIds);
    }

    [Fact]
    public async Task Remove_WhenNotAssigned_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var tagId = await TagsTestHelpers.CreateTagAsync(client, "Remove Unassigned Tag");
        var transactionId = await TagsTestHelpers.CreateTransactionAsync(client, "Remove Unassigned");

        var response = await client.DeleteAsync($"/api/transactions/{transactionId}/tags/{tagId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Remove_WithoutSession_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/transactions/1/tags/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
