using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Import.Import;
using static Api.Features.Transactions.Transactions;
using static Api.Tests.Features.Import.ImportTestHelpers;

namespace Api.Tests.Features.Import;

/// <summary>Deleting is the off switch, and the only way to change a rule: delete and rewrite.</summary>
public class DeleteAssignmentRuleTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public DeleteAssignmentRuleTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_RemovesTheRuleAndLeavesItsCategoryAndTags()
    {
        var client = await LoggedInClientAsync(_factory);
        var categoryId = await CreateCategoryAsync(client, $"Delete Rule Category {Guid.NewGuid():N}");
        var tagId = await CreateTagAsync(client, $"Delete Rule Tag {Guid.NewGuid():N}");
        var rule = await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, $"DELETE ME {Guid.NewGuid():N}", IgnoreMatchType.Contains, categoryId, NeedWant.Need, [tagId]));

        var response = await client.DeleteAsync($"/api/import/assignment-rules/{rule.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var rules = await client.GetFromJsonAsync<List<FetchAllAssignmentRules.Response>>(
            "/api/import/assignment-rules", TestClientExtensions.JsonOptions);
        Assert.DoesNotContain(rules!, r => r.Id == rule.Id);
        // With the rule gone, nothing protects the category any more — and nothing needs to.
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/tags/{tagId}")).StatusCode);
    }

    [Fact]
    public async Task Delete_ARuleThatIsAlreadyGone_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);
        var rule = await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, $"DELETE TWICE {Guid.NewGuid():N}", IgnoreMatchType.Contains, null, NeedWant.Want, []));
        (await client.DeleteAsync($"/api/import/assignment-rules/{rule.Id}")).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/import/assignment-rules/{rule.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ThenWritingTheSameRuleAgain_IsAllowed()
    {
        var client = await LoggedInClientAsync(_factory);
        var command = new CreateAssignmentRule.Command(
            null, $"REWRITE {Guid.NewGuid():N}", IgnoreMatchType.Contains, null, NeedWant.Want, []);
        var rule = await CreateAssignmentRuleAsync(client, command);
        (await client.DeleteAsync($"/api/import/assignment-rules/{rule.Id}")).EnsureSuccessStatusCode();

        // There is no edit: a wrong rule is deleted and written again.
        var response = await PostAssignmentRuleAsync(client, command with { NeedWant = NeedWant.Need });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
