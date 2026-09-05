using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using TransactionsFeature = Api.Features.Transactions.Transactions;

namespace Api.Tests.Features.Tags;

public class AssignToTransactionTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public AssignToTransactionTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Assign_WithKnownTransactionAndTag_AttachesTheTag()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var tagId = await TagsTestHelpers.CreateTagAsync(client, "Assign Tag");
        var transactionId = await TagsTestHelpers.CreateTransactionAsync(client, "Assign");

        var response = await client.PutAsync($"/api/transactions/{transactionId}/tags/{tagId}", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var transaction = await FetchTransactionAsync(client, transactionId);
        Assert.Contains(tagId, transaction.TagIds);
    }

    [Fact]
    public async Task Assign_WithMultipleTags_AttachesAllOfThem()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var firstTagId = await TagsTestHelpers.CreateTagAsync(client, "Assign Multiple First");
        var secondTagId = await TagsTestHelpers.CreateTagAsync(client, "Assign Multiple Second");
        var transactionId = await TagsTestHelpers.CreateTransactionAsync(client, "Assign Multiple");

        (await client.PutAsync($"/api/transactions/{transactionId}/tags/{firstTagId}", null)).EnsureSuccessStatusCode();
        (await client.PutAsync($"/api/transactions/{transactionId}/tags/{secondTagId}", null)).EnsureSuccessStatusCode();

        var transaction = await FetchTransactionAsync(client, transactionId);
        Assert.Contains(firstTagId, transaction.TagIds);
        Assert.Contains(secondTagId, transaction.TagIds);
    }

    [Fact]
    public async Task Assign_Twice_IsIdempotent()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var tagId = await TagsTestHelpers.CreateTagAsync(client, "Assign Twice Tag");
        var transactionId = await TagsTestHelpers.CreateTransactionAsync(client, "Assign Twice");
        (await client.PutAsync($"/api/transactions/{transactionId}/tags/{tagId}", null)).EnsureSuccessStatusCode();

        var response = await client.PutAsync($"/api/transactions/{transactionId}/tags/{tagId}", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var transaction = await FetchTransactionAsync(client, transactionId);
        Assert.Single(transaction.TagIds.Where(id => id == tagId));
    }

    [Fact]
    public async Task Assign_WithUnknownTransaction_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var tagId = await TagsTestHelpers.CreateTagAsync(client, "Assign Unknown Transaction Tag");

        var response = await client.PutAsync($"/api/transactions/999999/tags/{tagId}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Assign_WithUnknownTag_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var transactionId = await TagsTestHelpers.CreateTransactionAsync(client, "Assign Unknown Tag");

        var response = await client.PutAsync($"/api/transactions/{transactionId}/tags/999999", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Assign_WithoutSession_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsync("/api/transactions/1/tags/1", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<TransactionsFeature.FetchAll.Response> FetchTransactionAsync(HttpClient client, int transactionId)
    {
        var list = await client.GetFromJsonAsync<List<TransactionsFeature.FetchAll.Response>>(
            "/api/transactions", TestClientExtensions.JsonOptions);
        return Assert.Single(list!.Where(t => t.Id == transactionId));
    }
}
