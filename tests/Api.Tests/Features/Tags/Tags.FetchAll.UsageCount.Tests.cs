using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Tags.Tags;

namespace Api.Tests.Features.Tags;

/// <summary>
/// Deleting a tag detaches it from every transaction carrying it, so the count has to be on the
/// tag before anyone is asked to confirm that. It also ranks the most-used tags in the tag line.
/// </summary>
public class FetchAllUsageCountTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public FetchAllUsageCountTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FetchAll_ReportsHowManyTransactionsCarryEachTag()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var usedTagId = await TagsTestHelpers.CreateTagAsync(client, "Usage Counted Tag");
        var unusedTagId = await TagsTestHelpers.CreateTagAsync(client, "Usage Uncounted Tag");
        var first = await TagsTestHelpers.CreateTransactionAsync(client, "Usage First");
        var second = await TagsTestHelpers.CreateTransactionAsync(client, "Usage Second");
        (await client.PutAsync($"/api/transactions/{first}/tags/{usedTagId}", null)).EnsureSuccessStatusCode();
        (await client.PutAsync($"/api/transactions/{second}/tags/{usedTagId}", null)).EnsureSuccessStatusCode();

        var list = (await client.GetFromJsonAsync<List<FetchAll.Response>>("/api/tags"))!;

        Assert.Equal(2, list.Single(t => t.Id == usedTagId).TransactionCount);
        Assert.Equal(0, list.Single(t => t.Id == unusedTagId).TransactionCount);
    }

    [Fact]
    public async Task FetchAll_CountDropsWhenATagIsRemovedFromATransaction()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var tagId = await TagsTestHelpers.CreateTagAsync(client, "Usage Dropping Tag");
        var transactionId = await TagsTestHelpers.CreateTransactionAsync(client, "Usage Dropping");
        (await client.PutAsync($"/api/transactions/{transactionId}/tags/{tagId}", null)).EnsureSuccessStatusCode();

        (await client.DeleteAsync($"/api/transactions/{transactionId}/tags/{tagId}")).EnsureSuccessStatusCode();

        var list = (await client.GetFromJsonAsync<List<FetchAll.Response>>("/api/tags"))!;
        Assert.Equal(0, list.Single(t => t.Id == tagId).TransactionCount);
    }
}
