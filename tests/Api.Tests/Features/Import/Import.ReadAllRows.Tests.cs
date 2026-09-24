using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Import.Import;
using static Api.Tests.Features.Import.ImportTestHelpers;

namespace Api.Tests.Features.Import;

/// <summary>
/// Station 3: the whole file read through the mapping, with the rows a rule catches marked and
/// counted. Trust is built from counts, so the counts are part of the response rather than
/// something the client adds up for itself.
/// </summary>
public class ReadAllRowsTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public ReadAllRowsTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>The mapping every test here uses: ignore-match.csv read as its header names it.</summary>
    private static ReadAllRows.Command Mapping() => new(
        ",", true, "MM/dd/yyyy", "Posted Date", "Payee", "Amount", null, null);

    [Fact]
    public async Task Read_WithNoRules_ReturnsEveryRowUnstruck()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"No Rules {Guid.NewGuid():N}");
        var preview = await UploadOkAsync(client, accountId, "ignore-match.csv");

        var result = await ReadOkAsync(client, preview.Token, Mapping());

        // Seven rows, not the Map step's five: station 3 prints the whole file.
        Assert.Equal(7, result.Rows.Count);
        Assert.All(result.Rows, row => Assert.Null(row.SkippedReason));
        Assert.Equal(7, result.WillImportCount);
        Assert.Equal(0, result.SkippedCount);
    }

    [Fact]
    public async Task Read_WithAMatchingRule_MarksTheRowSkippedAndNamesTheRule()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Autopay {Guid.NewGuid():N}");
        await CreateIgnoreRuleAsync(client, accountId, "AUTOPAY", IgnoreMatchType.Contains);
        var preview = await UploadOkAsync(client, accountId, "ignore-match.csv");

        var result = await ReadOkAsync(client, preview.Token, Mapping());

        var autopay = result.Rows.Single(r => r.Description == "AUTOPAY THANK YOU");
        // The reason names the rule that caught the row, so an over-broad rule is diagnosable.
        Assert.Equal("contains AUTOPAY", autopay.SkippedReason);
        Assert.Null(autopay.Error);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(6, result.WillImportCount);
        Assert.All(
            result.Rows.Where(r => r.Description != "AUTOPAY THANK YOU"),
            row => Assert.Null(row.SkippedReason));
    }

    [Fact]
    public async Task Read_MatchesWithoutRegardToCase()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Lowercase Rule {Guid.NewGuid():N}");
        await CreateIgnoreRuleAsync(client, accountId, "autopay thank you", IgnoreMatchType.Equals);
        var preview = await UploadOkAsync(client, accountId, "ignore-match.csv");

        var result = await ReadOkAsync(client, preview.Token, Mapping());

        Assert.Equal(1, result.SkippedCount);
        Assert.Equal("is autopay thank you", result.Rows.Single(r => r.SkippedReason is not null).SkippedReason);
    }

    [Fact]
    public async Task Read_WithAStartsWithRule_CatchesTheTransferWithNoTransferEntity()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Transfer {Guid.NewGuid():N}");
        await CreateIgnoreRuleAsync(client, accountId, "TRANSFER TO", IgnoreMatchType.StartsWith);
        var preview = await UploadOkAsync(client, accountId, "ignore-match.csv");

        var result = await ReadOkAsync(client, preview.Token, Mapping());

        var transfer = result.Rows.Single(r => r.Description == "TRANSFER TO SAVINGS");
        Assert.Equal("starts with TRANSFER TO", transfer.SkippedReason);
        Assert.Equal(1, result.SkippedCount);
    }

    [Fact]
    public async Task Read_WithAGlobalRule_AppliesItToEveryAccount()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Global Applies {Guid.NewGuid():N}");
        var rule = await CreateIgnoreRuleAsync(client, null, "CITY UTILITIES", IgnoreMatchType.Contains);
        var preview = await UploadOkAsync(client, accountId, "ignore-match.csv");

        try
        {
            var result = await ReadOkAsync(client, preview.Token, Mapping());

            Assert.Equal(
                "contains CITY UTILITIES",
                result.Rows.Single(r => r.Description == "CITY UTILITIES").SkippedReason);
        }
        finally
        {
            // The only global rule in this class: left behind it would strike a row in every other
            // test here, which is exactly the reach a global rule is supposed to have.
            await client.DeleteAsync($"/api/import/ignore-rules/{rule.Id}");
        }
    }

    [Fact]
    public async Task Read_WithARuleScopedToAnotherAccount_LeavesTheRowsAlone()
    {
        var client = await LoggedInClientAsync(_factory);
        var otherAccountId = await CreateAccountAsync(client, $"Elsewhere {Guid.NewGuid():N}");
        await CreateIgnoreRuleAsync(client, otherAccountId, "KROGER", IgnoreMatchType.Contains);
        var accountId = await CreateAccountAsync(client, $"Untouched {Guid.NewGuid():N}");
        var preview = await UploadOkAsync(client, accountId, "ignore-match.csv");

        var result = await ReadOkAsync(client, preview.Token, Mapping());

        Assert.Equal(0, result.SkippedCount);
    }

    [Fact]
    public async Task Read_AfterTheRuleIsDeleted_UnstrikesTheRow()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Deleted Rule {Guid.NewGuid():N}");
        var rule = await CreateIgnoreRuleAsync(client, accountId, "SHELL OIL", IgnoreMatchType.Contains);
        var preview = await UploadOkAsync(client, accountId, "ignore-match.csv");
        Assert.Equal(1, (await ReadOkAsync(client, preview.Token, Mapping())).SkippedCount);

        (await client.DeleteAsync($"/api/import/ignore-rules/{rule.Id}")).EnsureSuccessStatusCode();
        var result = await ReadOkAsync(client, preview.Token, Mapping());

        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(7, result.WillImportCount);
    }

    [Fact]
    public async Task Read_WhenARowBothMatchesARuleAndFailsToParse_PrintsOnlyTheStrike()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Strike Wins {Guid.NewGuid():N}");
        await CreateIgnoreRuleAsync(client, accountId, "AUTOPAY", IgnoreMatchType.Contains);
        var preview = await UploadOkAsync(client, accountId, "ignore-match.csv");

        // The wrong date format makes every row unreadable; the AUTOPAY row is leaving either way,
        // and two reasons for one row would read as two problems.
        var result = await ReadOkAsync(client, preview.Token, Mapping() with { DateFormat = "yyyy-MM-dd" });

        var autopay = result.Rows.Single(r => r.Description == "AUTOPAY THANK YOU");
        Assert.Equal("contains AUTOPAY", autopay.SkippedReason);
        Assert.Null(autopay.Error);
        Assert.Equal(1, result.SkippedCount);
        // Nothing can import when no date parses, and the unreadable rows are counted as such.
        Assert.Equal(0, result.WillImportCount);
        Assert.Equal(6, result.ErrorCount);
    }

    [Fact]
    public async Task Read_WithAnUnknownToken_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await ReadAsync(client, "not-a-real-token", Mapping());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static Task<HttpResponseMessage> ReadAsync(HttpClient client, string token, ReadAllRows.Command command) =>
        client.PostAsJsonAsync($"/api/import/previews/{token}/rows", command, TestClientExtensions.JsonOptions);

    private static async Task<ReadAllRows.Response> ReadOkAsync(
        HttpClient client, string token, ReadAllRows.Command command)
    {
        var response = await ReadAsync(client, token, command);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReadAllRows.Response>(TestClientExtensions.JsonOptions))!;
    }
}
