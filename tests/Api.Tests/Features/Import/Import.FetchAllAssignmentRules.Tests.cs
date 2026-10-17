using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Import.Import;
using static Api.Features.Transactions.Transactions;
using static Api.Tests.Features.Import.ImportTestHelpers;

namespace Api.Tests.Features.Import;

/// <summary>
/// The rules page reads every assignment rule back as a sentence, so the list has to carry
/// everything that sentence needs: the words, the names of what it assigns, and its scope.
/// </summary>
public class FetchAllAssignmentRulesTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public FetchAllAssignmentRulesTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FetchAll_ReturnsEachRuleWithWhatItAssignsByName()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountName = $"Fetch Assign {Guid.NewGuid():N}";
        var accountId = await CreateAccountAsync(client, accountName);
        var categoryName = $"Travel {Guid.NewGuid():N}";
        var categoryId = await CreateCategoryAsync(client, categoryName);
        var tagName = $"Vacation {Guid.NewGuid():N}";
        var tagId = await CreateTagAsync(client, tagName);
        var globalText = $"FETCH GLOBAL {Guid.NewGuid():N}";
        var scopedText = $"FETCH SCOPED {Guid.NewGuid():N}";
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, globalText, IgnoreMatchType.Contains, categoryId, NeedWant.Want, [tagId]));
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, scopedText, IgnoreMatchType.Equals, null, NeedWant.Need, []));

        var rules = await FetchAllAsync(client);

        var global = rules.Single(r => r.MatchText == globalText);
        Assert.Null(global.AccountId);
        Assert.Null(global.AccountName);
        Assert.Equal(categoryName, global.CategoryName);
        Assert.Equal(NeedWant.Want, global.NeedWant);
        Assert.Equal(tagName, Assert.Single(global.Tags).Name);
        Assert.True(global.IsActive);

        // A rule that sets only some fields carries only those.
        var scoped = rules.Single(r => r.MatchText == scopedText);
        Assert.Equal(accountName, scoped.AccountName);
        Assert.Equal(IgnoreMatchType.Equals, scoped.MatchType);
        Assert.Null(scoped.CategoryId);
        Assert.Null(scoped.CategoryName);
        Assert.Equal(NeedWant.Need, scoped.NeedWant);
        Assert.Empty(scoped.Tags);
    }

    [Fact]
    public async Task FetchAll_PutsGlobalRulesFirstThenOrdersByAccountName()
    {
        var client = await LoggedInClientAsync(_factory);
        var suffix = Guid.NewGuid().ToString("N");
        var zulu = await CreateAccountAsync(client, $"Zulu Order {suffix}");
        var alpha = await CreateAccountAsync(client, $"Alpha Order {suffix}");
        // Created in the reverse of the expected order, so an unordered read would fail.
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            zulu, $"ORDER Z {suffix}", IgnoreMatchType.Contains, null, NeedWant.Need, []));
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            alpha, $"ORDER A {suffix}", IgnoreMatchType.Contains, null, NeedWant.Need, []));
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, $"ORDER GLOBAL {suffix}", IgnoreMatchType.Contains, null, NeedWant.Need, []));

        var rules = await FetchAllAsync(client);

        var lastGlobal = rules.FindLastIndex(r => r.AccountId is null);
        var firstScoped = rules.FindIndex(r => r.AccountId is not null);
        Assert.True(lastGlobal < firstScoped);
        Assert.True(
            rules.FindIndex(r => r.AccountId == alpha) < rules.FindIndex(r => r.AccountId == zulu));
    }

    private static async Task<List<FetchAllAssignmentRules.Response>> FetchAllAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<FetchAllAssignmentRules.Response>>(
            "/api/import/assignment-rules", TestClientExtensions.JsonOptions))!;
}
