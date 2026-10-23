using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Import.Import;
using static Api.Features.Transactions.Transactions;
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
    public async Task Read_AfterAnOverlappingImport_MarksAndCountsTheRowsAlreadyImported()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Duplicates {Guid.NewGuid():N}");
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var first = await UploadOkAsync(client, accountId, "signed-amount.csv");
        (await client.PostAsync($"/api/import/previews/{first.Token}/confirm", null)).EnsureSuccessStatusCode();
        var preview = await UploadOkAsync(client, accountId, "overlapping-export.csv");

        var result = await ReadOkAsync(client, preview.Token, Mapping());

        Assert.Equal(
            ["BLUE BOTTLE COFFEE", "CITY UTILITIES"],
            result.Rows.Where(r => r.IsDuplicate).Select(r => r.Description));
        Assert.Equal(2, result.DuplicateCount);
        // The count on the Import button is the count the confirm will write.
        Assert.Equal(3, result.WillImportCount);
        Assert.Equal(0, result.SkippedCount);
    }

    [Fact]
    public async Task Read_ARowWithABlankDate_IsUnreadableRatherThanImportedUndated()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Blank Date {Guid.NewGuid():N}");
        var preview = await UploadBytesAsync(client, accountId, "blank-date.csv",
            "Posted Date,Reference,Payee,Amount\n08/14/2026,1,KROGER #442,-84.19\n,2,PENDING HOLD,-1.00\n,,,\n"u8.ToArray());
        preview.EnsureSuccessStatusCode();
        var token = (await preview.Content.ReadFromJsonAsync<UploadPreview.Response>(TestClientExtensions.JsonOptions))!.Token;

        var result = await ReadOkAsync(client, token, Mapping());

        // The line of bare commas is not a row at all, so it is neither printed nor counted.
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("This row has no date.", result.Rows.Single(r => r.Description == "PENDING HOLD").Error);
        Assert.Equal(1, result.ErrorCount);
        Assert.Equal(1, result.WillImportCount);
    }

    [Fact]
    public async Task Read_WithAnUnknownToken_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await ReadAsync(client, "not-a-real-token", Mapping());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- assignment rules in the preview (ticket 14). Rules are scoped to each test's own account,
    // because the test class shares one database.

    [Fact]
    public async Task Read_WithAssignmentRules_PrintsHowEachRowWillBeFiledAndWhichRulesDecidedIt()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Classified {Guid.NewGuid():N}");
        var groceriesName = $"Groceries {Guid.NewGuid():N}";
        var groceries = await CreateCategoryAsync(client, groceriesName);
        var travelName = $"Travel {Guid.NewGuid():N}";
        var travel = await CreateCategoryAsync(client, travelName);
        var vacationName = $"Vacation {Guid.NewGuid():N}";
        var vacation = await CreateTagAsync(client, vacationName);
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "KROGER", IgnoreMatchType.Contains, groceries, NeedWant.Need, []));
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "DELTA", IgnoreMatchType.StartsWith, travel, null, []));
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "AIR LINES", IgnoreMatchType.Contains, null, null, [vacation]));
        // Matches nothing in this file, and says nothing about it.
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "NO SUCH PAYEE", IgnoreMatchType.Contains, groceries, null, []));
        var preview = await UploadOkAsync(client, accountId, "assignment-match.csv");

        var result = await ReadOkAsync(client, preview.Token, Mapping());

        var kroger = result.Rows.Single(r => r.Description == "KROGER #442");
        Assert.Equal(groceries, kroger.CategoryId);
        Assert.Equal(groceriesName, kroger.CategoryName);
        Assert.Equal(NeedWant.Need, kroger.NeedWant);
        Assert.Empty(kroger.Tags);
        Assert.Equal(["contains KROGER"], kroger.ClassifiedBy);

        // Two rules contributed, so both are cited — the category from one, the tag from the other.
        var delta = result.Rows.Single(r => r.Description == "DELTA AIR LINES 0062");
        Assert.Equal(travelName, delta.CategoryName);
        Assert.Equal(NeedWant.Want, delta.NeedWant);
        Assert.Equal(vacationName, Assert.Single(delta.Tags).Name);
        Assert.Equal(["starts with DELTA", "contains AIR LINES"], delta.ClassifiedBy);

        // Nobody taught the app about this one: no category, the import's Want, and no rule to cite.
        var traderJoes = result.Rows.Single(r => r.Description == "TRADER JOE'S #221");
        Assert.Null(traderJoes.CategoryId);
        Assert.Null(traderJoes.CategoryName);
        Assert.Equal(NeedWant.Want, traderJoes.NeedWant);
        Assert.Empty(traderJoes.ClassifiedBy);

        Assert.Equal(5, result.WillImportCount);
        Assert.Equal(2, result.ClassifiedCount);
        Assert.Equal(3, result.ForReviewCount);
    }

    [Fact]
    public async Task Read_WithNoAssignmentRules_EveryRowGoingInIsForReview()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Unclassified {Guid.NewGuid():N}");
        var preview = await UploadOkAsync(client, accountId, "assignment-match.csv");

        var result = await ReadOkAsync(client, preview.Token, Mapping());

        Assert.All(result.Rows, row =>
        {
            Assert.Null(row.CategoryId);
            Assert.Equal(NeedWant.Want, row.NeedWant);
            Assert.Empty(row.ClassifiedBy);
        });
        Assert.Equal(0, result.ClassifiedCount);
        Assert.Equal(5, result.ForReviewCount);
    }

    [Fact]
    public async Task Read_RowsThatAreLeaving_CarryNoAssignments()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Leaving {Guid.NewGuid():N}");
        var fuel = await CreateCategoryAsync(client, $"Fuel {Guid.NewGuid():N}");
        var bills = await CreateCategoryAsync(client, $"Bills {Guid.NewGuid():N}");
        await CreateIgnoreRuleAsync(client, accountId, "AUTOPAY", IgnoreMatchType.Contains);
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "AUTOPAY", IgnoreMatchType.Contains, bills, NeedWant.Need, []));
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "STATEMENT", IgnoreMatchType.StartsWith, bills, NeedWant.Need, []));
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "SHELL", IgnoreMatchType.StartsWith, fuel, NeedWant.Need, []));
        // Import the file once, so reading it again finds its rows already in the ledger.
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        (await client.PostAsync($"/api/import/previews/{(await UploadOkAsync(client, accountId, "assignment-match.csv")).Token}/confirm", null))
            .EnsureSuccessStatusCode();

        var again = await ReadOkAsync(client, (await UploadOkAsync(client, accountId, "assignment-match.csv")).Token, Mapping());
        var unreadable = await ReadOkAsync(client, (await UploadOkAsync(client, accountId, "unreadable-row.csv")).Token, Mapping());

        // Struck: the row is leaving, so classifying it would mean nothing.
        var autopay = again.Rows.Single(r => r.Description == "AUTOPAY THANK YOU");
        Assert.NotNull(autopay.SkippedReason);
        AssertNoAssignments(autopay);

        // Already in the ledger: the duplicate mark wins.
        var shell = again.Rows.Single(r => r.Description == "SHELL OIL 5578");
        Assert.True(shell.IsDuplicate);
        AssertNoAssignments(shell);
        Assert.Equal(0, again.ClassifiedCount);
        Assert.Equal(0, again.ForReviewCount);

        // Unreadable: there is nothing to classify until it can be read.
        var statement = unreadable.Rows.Single(r => r.Description == "STATEMENT BALANCE");
        Assert.NotNull(statement.Error);
        AssertNoAssignments(statement);
    }

    private static void AssertNoAssignments(ReadAllRows.Row row)
    {
        Assert.Null(row.CategoryId);
        Assert.Null(row.NeedWant);
        Assert.Empty(row.Tags);
        Assert.Empty(row.ClassifiedBy);
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
