using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Import.Import;
using static Api.Tests.Features.Import.ImportTestHelpers;

namespace Api.Tests.Features.Import;

/// <summary>
/// The ledger of rules. A rule that catches too much is only diagnosable if the whole list can be
/// read at once, so every rule is returned — global ones first, where they belong.
/// </summary>
public class FetchAllIgnoreRulesTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public FetchAllIgnoreRulesTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FetchAll_ReturnsCreatedRulesWithTheirScope()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountName = $"Fetch Rules {Guid.NewGuid():N}";
        var accountId = await CreateAccountAsync(client, accountName);
        var globalText = $"FETCH GLOBAL {Guid.NewGuid():N}";
        var scopedText = $"FETCH SCOPED {Guid.NewGuid():N}";
        await CreateIgnoreRuleAsync(client, null, globalText, IgnoreMatchType.Contains);
        await CreateIgnoreRuleAsync(client, accountId, scopedText, IgnoreMatchType.Equals);

        var rules = await FetchAllAsync(client);

        var global = rules.Single(r => r.MatchText == globalText);
        Assert.Null(global.AccountId);
        Assert.Null(global.AccountName);
        Assert.True(global.IsActive);

        var scoped = rules.Single(r => r.MatchText == scopedText);
        Assert.Equal(accountId, scoped.AccountId);
        Assert.Equal(accountName, scoped.AccountName);
        Assert.Equal(IgnoreMatchType.Equals, scoped.MatchType);
    }

    [Fact]
    public async Task FetchAll_PutsGlobalRulesAheadOfAccountRules()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Order Rules {Guid.NewGuid():N}");
        // Created account-first, so an unordered read would hand them back the other way round.
        await CreateIgnoreRuleAsync(client, accountId, $"ORDER SCOPED {Guid.NewGuid():N}", IgnoreMatchType.Contains);
        await CreateIgnoreRuleAsync(client, null, $"ORDER GLOBAL {Guid.NewGuid():N}", IgnoreMatchType.Contains);

        var rules = await FetchAllAsync(client);

        var lastGlobal = rules.FindLastIndex(r => r.AccountId is null);
        var firstScoped = rules.FindIndex(r => r.AccountId is not null);
        Assert.True(lastGlobal < firstScoped);
    }

    private static async Task<List<FetchAllIgnoreRules.Response>> FetchAllAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<FetchAllIgnoreRules.Response>>(
            "/api/import/ignore-rules", TestClientExtensions.JsonOptions))!;
}
