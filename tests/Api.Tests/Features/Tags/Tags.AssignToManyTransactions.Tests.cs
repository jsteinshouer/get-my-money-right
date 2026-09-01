using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Tags.Tags;
using TransactionsFeature = Api.Features.Transactions.Transactions;

namespace Api.Tests.Features.Tags;

/// <summary>
/// The investigation scene ("how much did the vacation cost?") tags a whole filtered selection at
/// once, so the batch is one request rather than one per row.
/// </summary>
public class AssignToManyTransactionsTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public AssignToManyTransactionsTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AssignToMany_TagsEverySelectedTransaction()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var tagId = await TagsTestHelpers.CreateTagAsync(client, "Bulk Assign Tag");
        var first = await TagsTestHelpers.CreateTransactionAsync(client, "Bulk Assign First");
        var second = await TagsTestHelpers.CreateTransactionAsync(client, "Bulk Assign Second");
        var untouched = await TagsTestHelpers.CreateTransactionAsync(client, "Bulk Assign Untouched");

        var response = await client.PostAsJsonAsync(
            $"/api/tags/{tagId}/transactions", new AssignToManyTransactions.Command([first, second]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<AssignToManyTransactions.Response>())!;
        Assert.Equal(2, body.AssignedCount);

        var list = (await client.GetFromJsonAsync<List<TransactionsFeature.FetchAll.Response>>(
            "/api/transactions", TestClientExtensions.JsonOptions))!;
        Assert.Contains(tagId, list.Single(t => t.Id == first).TagIds);
        Assert.Contains(tagId, list.Single(t => t.Id == second).TagIds);
        Assert.DoesNotContain(tagId, list.Single(t => t.Id == untouched).TagIds);
    }

    [Fact]
    public async Task AssignToMany_CountsOnlyTheTransactionsItActuallyAdded()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var tagId = await TagsTestHelpers.CreateTagAsync(client, "Bulk Partial Tag");
        var alreadyTagged = await TagsTestHelpers.CreateTransactionAsync(client, "Bulk Partial Already");
        var fresh = await TagsTestHelpers.CreateTransactionAsync(client, "Bulk Partial Fresh");
        (await client.PutAsync($"/api/transactions/{alreadyTagged}/tags/{tagId}", null)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            $"/api/tags/{tagId}/transactions", new AssignToManyTransactions.Command([alreadyTagged, fresh]));

        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<AssignToManyTransactions.Response>())!;
        // The already-tagged row is reported as skipped, so the UI can say what actually changed.
        Assert.Equal(1, body.AssignedCount);
        Assert.Equal(1, body.AlreadyTaggedCount);
    }

    [Fact]
    public async Task AssignToMany_IgnoresTransactionsThatDoNotExist()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var tagId = await TagsTestHelpers.CreateTagAsync(client, "Bulk Missing Tag");
        var real = await TagsTestHelpers.CreateTransactionAsync(client, "Bulk Missing Real");

        var response = await client.PostAsJsonAsync(
            $"/api/tags/{tagId}/transactions", new AssignToManyTransactions.Command([real, 999999]));

        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<AssignToManyTransactions.Response>())!;
        Assert.Equal(1, body.AssignedCount);
    }

    [Fact]
    public async Task AssignToMany_WithUnknownTag_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var transactionId = await TagsTestHelpers.CreateTransactionAsync(client, "Bulk Unknown Tag");

        var response = await client.PostAsJsonAsync(
            "/api/tags/999999/transactions", new AssignToManyTransactions.Command([transactionId]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignToMany_WithNoTransactions_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var tagId = await TagsTestHelpers.CreateTagAsync(client, "Bulk Empty Tag");

        var response = await client.PostAsJsonAsync(
            $"/api/tags/{tagId}/transactions", new AssignToManyTransactions.Command([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignToMany_WithoutSession_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tags/1/transactions", new AssignToManyTransactions.Command([1]));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
