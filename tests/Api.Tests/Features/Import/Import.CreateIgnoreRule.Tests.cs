using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Import.Import;
using static Api.Tests.Features.Import.ImportTestHelpers;

namespace Api.Tests.Features.Import;

/// <summary>
/// A rule is how the household says "this row is noise" once instead of every month — including
/// the inter-account transfer, which this app deliberately has no other way to describe.
/// </summary>
public class CreateIgnoreRuleTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public CreateIgnoreRuleTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithNoAccount_MakesAGlobalRule()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await PostIgnoreRuleAsync(
            client, null, $"GLOBAL AUTOPAY {Guid.NewGuid():N}", IgnoreMatchType.Contains);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var rule = await ReadRuleAsync(response);
        Assert.Null(rule.AccountId);
        Assert.Null(rule.AccountName);
        Assert.Equal(IgnoreMatchType.Contains, rule.MatchType);
        // Delete is the off switch in this ticket, so a rule is born switched on.
        Assert.True(rule.IsActive);
    }

    [Fact]
    public async Task Create_WithAnAccount_ScopesTheRuleToThatAccountAndNamesIt()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountName = $"Scoped Rule {Guid.NewGuid():N}";
        var accountId = await CreateAccountAsync(client, accountName);

        var response = await PostIgnoreRuleAsync(
            client, accountId, "TRANSFER TO SAVINGS", IgnoreMatchType.StartsWith);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var rule = await ReadRuleAsync(response);
        Assert.Equal(accountId, rule.AccountId);
        Assert.Equal(accountName, rule.AccountName);
        Assert.Equal(IgnoreMatchType.StartsWith, rule.MatchType);
    }

    [Fact]
    public async Task Create_TrimsAndCollapsesTheMatchText()
    {
        var client = await LoggedInClientAsync(_factory);
        var text = $"  PADDED   {Guid.NewGuid():N}  ";

        var response = await PostIgnoreRuleAsync(client, null, text, IgnoreMatchType.Contains);

        response.EnsureSuccessStatusCode();
        var rule = await ReadRuleAsync(response);
        // The description a row shows is whitespace-collapsed, so a rule carrying a double space
        // would silently never match anything.
        Assert.Equal(text.Trim().Replace("   ", " "), rule.MatchText);
    }

    [Fact]
    public async Task Create_WithTheSameTextTypeAndScope_ReturnsConflict()
    {
        var client = await LoggedInClientAsync(_factory);
        var text = $"DUPLICATE {Guid.NewGuid():N}";
        (await PostIgnoreRuleAsync(client, null, text, IgnoreMatchType.Contains)).EnsureSuccessStatusCode();

        var response = await PostIgnoreRuleAsync(client, null, text.ToLowerInvariant(), IgnoreMatchType.Contains);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithTheSameTextButADifferentType_IsAllowed()
    {
        var client = await LoggedInClientAsync(_factory);
        var text = $"SAME TEXT {Guid.NewGuid():N}";
        (await PostIgnoreRuleAsync(client, null, text, IgnoreMatchType.Contains)).EnsureSuccessStatusCode();

        var response = await PostIgnoreRuleAsync(client, null, text, IgnoreMatchType.Equals);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithWhitespaceOnlyMatchText_ReturnsBadRequest()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await PostIgnoreRuleAsync(client, null, "   ", IgnoreMatchType.Contains);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ForAnAccountThatDoesNotExist_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await PostIgnoreRuleAsync(client, 987654, "NO SUCH ACCOUNT", IgnoreMatchType.Contains);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<CreateIgnoreRule.Response> ReadRuleAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<CreateIgnoreRule.Response>(TestClientExtensions.JsonOptions))!;
}
