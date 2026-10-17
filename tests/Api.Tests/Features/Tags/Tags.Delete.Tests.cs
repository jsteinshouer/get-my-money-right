using System.Net;
using System.Net.Http.Json;
using Api.Tests.Features.Import;
using Api.Tests.Fixtures;
using static Api.Features.Tags.Tags;
using ImportFeature = Api.Features.Import.Import;
using NeedWant = Api.Features.Transactions.Transactions.NeedWant;

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

    [Fact]
    public async Task Delete_UsedByAssignmentRules_DropsItFromThemAndRemovesARuleLeftSettingNothing()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var tagId = await TagsTestHelpers.CreateTagAsync(client, $"Ruled Tag {Guid.NewGuid():N}");
        var keptTagId = await TagsTestHelpers.CreateTagAsync(client, $"Kept Tag {Guid.NewGuid():N}");
        var stillSetsSomething = await ImportTestHelpers.CreateAssignmentRuleAsync(client, new ImportFeature.CreateAssignmentRule.Command(
            null, $"TAG AND NEED {Guid.NewGuid():N}", ImportFeature.IgnoreMatchType.Contains, null, NeedWant.Need, [tagId]));
        var keepsAnotherTag = await ImportTestHelpers.CreateAssignmentRuleAsync(client, new ImportFeature.CreateAssignmentRule.Command(
            null, $"TWO TAGS {Guid.NewGuid():N}", ImportFeature.IgnoreMatchType.Contains, null, null, [tagId, keptTagId]));
        var onlyThisTag = await ImportTestHelpers.CreateAssignmentRuleAsync(client, new ImportFeature.CreateAssignmentRule.Command(
            null, $"ONLY TAG {Guid.NewGuid():N}", ImportFeature.IgnoreMatchType.Contains, null, null, [tagId]));

        var response = await client.DeleteAsync($"/api/tags/{tagId}");

        // A tag is a label the ledger does not depend on, so its rules never block the delete.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var rules = await client.GetFromJsonAsync<List<ImportFeature.FetchAllAssignmentRules.Response>>(
            "/api/import/assignment-rules", TestClientExtensions.JsonOptions);

        var need = Assert.Single(rules!, r => r.Id == stillSetsSomething.Id);
        Assert.Empty(need.Tags);
        Assert.Equal(NeedWant.Need, need.NeedWant);

        var kept = Assert.Single(rules!, r => r.Id == keepsAnotherTag.Id);
        Assert.Equal(keptTagId, Assert.Single(kept.Tags).Id);

        // Left setting nothing, the rule could not be read back as a sentence, so it goes too.
        Assert.DoesNotContain(rules!, r => r.Id == onlyThisTag.Id);
    }
}
