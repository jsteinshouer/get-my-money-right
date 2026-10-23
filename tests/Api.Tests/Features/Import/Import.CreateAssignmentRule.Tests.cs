using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Import.Import;
using static Api.Features.Transactions.Transactions;
using static Api.Tests.Features.Import.ImportTestHelpers;

namespace Api.Tests.Features.Import;

/// <summary>
/// An assignment rule is the household saying what a row *is* once — "KROGER is Groceries and a
/// Need" — instead of every month. It sets any mix of category, Need/Want and tags, but never nothing.
/// </summary>
public class CreateAssignmentRuleTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public CreateAssignmentRuleTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithACategoryAndNeedWant_MakesAGlobalRuleThatNamesThem()
    {
        var client = await LoggedInClientAsync(_factory);
        var categoryName = $"Groceries {Guid.NewGuid():N}";
        var categoryId = await CreateCategoryAsync(client, categoryName);

        var response = await PostAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, $"KROGER {Guid.NewGuid():N}", IgnoreMatchType.Contains, categoryId, NeedWant.Need, []));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var rule = await ReadRuleAsync(response);
        Assert.Null(rule.AccountId);
        Assert.Null(rule.AccountName);
        Assert.Equal(IgnoreMatchType.Contains, rule.MatchType);
        Assert.Equal(categoryId, rule.CategoryId);
        Assert.Equal(categoryName, rule.CategoryName);
        Assert.Equal(NeedWant.Need, rule.NeedWant);
        Assert.Empty(rule.Tags);
        Assert.True(rule.IsActive);
    }

    [Fact]
    public async Task Create_WithOnlyTags_ScopedToAnAccount_ReadsBackTheTagsByName()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountName = $"Assign Scope {Guid.NewGuid():N}";
        var accountId = await CreateAccountAsync(client, accountName);
        var vacation = await CreateTagAsync(client, $"Vacation {Guid.NewGuid():N}");
        var deductible = await CreateTagAsync(client, $"Deductible {Guid.NewGuid():N}");

        var response = await PostAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "DELTA AIR", IgnoreMatchType.StartsWith, null, null, [vacation, deductible]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var rule = await ReadRuleAsync(response);
        Assert.Equal(accountId, rule.AccountId);
        Assert.Equal(accountName, rule.AccountName);
        // A rule that only tags is a whole rule: it tracks a vacation without dictating a category.
        Assert.Null(rule.CategoryId);
        Assert.Null(rule.NeedWant);
        Assert.Equal([deductible, vacation], rule.Tags.Select(t => t.Id));
    }

    [Fact]
    public async Task Create_TrimsAndCollapsesTheMatchText()
    {
        var client = await LoggedInClientAsync(_factory);
        var text = $"  PADDED   {Guid.NewGuid():N}  ";

        var response = await PostAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, text, IgnoreMatchType.Contains, null, NeedWant.Want, []));

        response.EnsureSuccessStatusCode();
        var rule = await ReadRuleAsync(response);
        Assert.Equal(text.Trim().Replace("   ", " "), rule.MatchText);
    }

    [Fact]
    public async Task Create_ThatSetsNothing_IsRejected()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await PostAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, $"NOTHING {Guid.NewGuid():N}", IgnoreMatchType.Contains, null, null, []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var rules = await client.GetFromJsonAsync<List<FetchAllAssignmentRules.Response>>(
            "/api/import/assignment-rules", TestClientExtensions.JsonOptions);
        Assert.DoesNotContain(rules!, r => r.MatchText.StartsWith("NOTHING"));
    }

    [Fact]
    public async Task Create_WithTheSameTextTypeAndScope_ReturnsConflict()
    {
        var client = await LoggedInClientAsync(_factory);
        var text = $"DUPLICATE {Guid.NewGuid():N}";
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, text, IgnoreMatchType.Contains, null, NeedWant.Need, []));

        // Different assignments, but the same words, kind of match and scope: the same rule.
        var response = await PostAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, text.ToLowerInvariant(), IgnoreMatchType.Contains, null, NeedWant.Want, []));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithTheSameTextOnAnotherScope_IsAllowed()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Assign Other Scope {Guid.NewGuid():N}");
        var text = $"SAME TEXT {Guid.NewGuid():N}";
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, text, IgnoreMatchType.Contains, null, NeedWant.Need, []));

        var response = await PostAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, text, IgnoreMatchType.Contains, null, NeedWant.Need, []));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThatMatchesAnIgnoreRulesWords_IsAllowed()
    {
        var client = await LoggedInClientAsync(_factory);
        var text = $"BOTH KINDS {Guid.NewGuid():N}";
        await CreateIgnoreRuleAsync(client, null, text, IgnoreMatchType.Contains);

        var response = await PostAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, text, IgnoreMatchType.Contains, null, NeedWant.Need, []));

        // Duplicates are per kind of rule; which one wins at import is ticket 14's to decide.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ForACategoryThatDoesNotExist_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await PostAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, "NO SUCH CATEGORY", IgnoreMatchType.Contains, 987654, null, []));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ForATagThatDoesNotExist_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await PostAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            null, "NO SUCH TAG", IgnoreMatchType.Contains, null, null, [987654]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ForAnAccountThatDoesNotExist_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await PostAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            987654, "NO SUCH ACCOUNT", IgnoreMatchType.Contains, null, NeedWant.Need, []));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<CreateAssignmentRule.Response> ReadRuleAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<CreateAssignmentRule.Response>(TestClientExtensions.JsonOptions))!;
}
