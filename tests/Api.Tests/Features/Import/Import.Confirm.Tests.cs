using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Import.Import;
using static Api.Features.Transactions.Transactions;
using static Api.Tests.Features.Import.ImportTestHelpers;

namespace Api.Tests.Features.Import;

/// <summary>
/// Confirming an import: the whole file read through the account's saved mapping, struck by its
/// ignore rules, checked against what the ledger already holds, and written as transactions. The
/// counts are the household's only evidence the import did what the preview said it would, so
/// every test here asserts them exactly.
/// </summary>
public class ConfirmTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public ConfirmTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Confirm_ASignedAmountExport_ImportsEveryRowWithItsOwnSign()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Signed {Guid.NewGuid():N}");
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        var result = await ConfirmOkAsync(client, preview.Token);

        Assert.Equal(new Confirm.Response(6, 0, 0), result);
        var stored = await TransactionsOnAsync(client, accountId);
        Assert.Equal(6, stored.Count);
        Assert.Equal(-84.19m, stored.Single(t => t.Description == "KROGER #442").Amount);
        Assert.Equal(312.00m, stored.Single(t => t.Description == "AUTOPAY THANK YOU").Amount);
        // The quoted comma is part of the payee, not a column break.
        Assert.Equal(new DateOnly(2026, 8, 16), stored.Single(t => t.Description == "BOOKS, ETC").Date);
    }

    [Fact]
    public async Task Confirm_ADebitCreditExport_StoresMoneyOutNegativeAndMoneyInPositive()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Debit Credit {Guid.NewGuid():N}");
        await SaveMappingAsync(client, accountId, new SaveMapping.Command(
            "Date", "Description", null, "Withdrawal", "Deposit", "MM/dd/yyyy", ",", true));
        var preview = await UploadOkAsync(client, accountId, "debit-credit.csv");

        var result = await ConfirmOkAsync(client, preview.Token);

        Assert.Equal(new Confirm.Response(5, 0, 0), result);
        var stored = await TransactionsOnAsync(client, accountId);
        // The file writes every figure unsigned; which column it sits in is the sign.
        Assert.Equal(-84.19m, stored.Single(t => t.Description == "GROCERY OUTLET").Amount);
        Assert.Equal(2410.55m, stored.Single(t => t.Description == "PAYROLL DEPOSIT").Amount);
        Assert.Equal(-1650.00m, stored.Single(t => t.Description == "RENT").Amount);
        Assert.Equal(45.00m, stored.Single(t => t.Description == "REFUND CONCERT TIX").Amount);
    }

    [Fact]
    public async Task Confirm_WithIgnoreRulesThatMatch_SkipsAndCountsTheStruckRows()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Ignored {Guid.NewGuid():N}");
        await CreateIgnoreRuleAsync(client, accountId, "AUTOPAY", IgnoreMatchType.Contains);
        await CreateIgnoreRuleAsync(client, accountId, "TRANSFER TO", IgnoreMatchType.StartsWith);
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "ignore-match.csv");

        var result = await ConfirmOkAsync(client, preview.Token);

        Assert.Equal(new Confirm.Response(5, 2, 0), result);
        var descriptions = (await TransactionsOnAsync(client, accountId)).Select(t => t.Description).ToList();
        Assert.DoesNotContain("AUTOPAY THANK YOU", descriptions);
        Assert.DoesNotContain("TRANSFER TO SAVINGS", descriptions);
    }

    [Fact]
    public async Task Confirm_AnOverlappingExport_SkipsOnlyTheRowsAlreadyImported()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Overlap {Guid.NewGuid():N}");
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var first = await UploadOkAsync(client, accountId, "signed-amount.csv");
        Assert.Equal(new Confirm.Response(6, 0, 0), await ConfirmOkAsync(client, first.Token));

        var second = await UploadOkAsync(client, accountId, "overlapping-export.csv");
        var result = await ConfirmOkAsync(client, second.Token);

        // The coffee on the 18th and the utilities bill (padded differently this time) were in the
        // first file. The coffee on the 20th is a new purchase, and the two identical Trader Joe's
        // rows are two real purchases — neither is in the ledger, so both import.
        Assert.Equal(new Confirm.Response(3, 0, 2), result);
        var stored = await TransactionsOnAsync(client, accountId);
        Assert.Equal(9, stored.Count);
        Assert.Equal(2, stored.Count(t => t.Description == "TRADER JOE'S #221"));
        Assert.Single(stored, t => t.Description == "CITY UTILITIES");
    }

    [Fact]
    public async Task Confirm_TheSameExportTwice_ImportsNothingTheSecondTime()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Twice {Guid.NewGuid():N}");
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        await ConfirmOkAsync(client, (await UploadOkAsync(client, accountId, "overlapping-export.csv")).Token);

        var again = await UploadOkAsync(client, accountId, "overlapping-export.csv");
        var result = await ConfirmOkAsync(client, again.Token);

        // Both Trader Joe's rows are already there, so both are duplicates now.
        Assert.Equal(new Confirm.Response(0, 0, 5), result);
        Assert.Equal(5, (await TransactionsOnAsync(client, accountId)).Count);
    }

    [Fact]
    public async Task Confirm_ARowAlreadyEnteredByHand_IsADuplicateWhateverItsCase()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"By Hand {Guid.NewGuid():N}");
        var categoryId = await CreateCategoryAsync(client, $"Groceries {Guid.NewGuid():N}");
        var manual = await client.PostAsJsonAsync("/api/transactions", new Create.Command(
            accountId, categoryId, new DateOnly(2026, 8, 14), -84.19m, "Kroger  #442", NeedWant.Need),
            TestClientExtensions.JsonOptions);
        manual.EnsureSuccessStatusCode();
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        var result = await ConfirmOkAsync(client, preview.Token);

        Assert.Equal(new Confirm.Response(5, 0, 1), result);
        // The household's own entry is the one that stays, category and all.
        var kroger = Assert.Single(
            await TransactionsOnAsync(client, accountId), t => t.Date == new DateOnly(2026, 8, 14));
        Assert.Equal(categoryId, kroger.CategoryId);
    }

    [Fact]
    public async Task Confirm_ARowOnAnotherAccount_IsNotADuplicate()
    {
        var client = await LoggedInClientAsync(_factory);
        var otherAccountId = await CreateAccountAsync(client, $"Other {Guid.NewGuid():N}");
        await SaveMappingAsync(client, otherAccountId, SignedAmountMapping());
        await ConfirmOkAsync(client, (await UploadOkAsync(client, otherAccountId, "signed-amount.csv")).Token);
        var accountId = await CreateAccountAsync(client, $"This One {Guid.NewGuid():N}");
        await SaveMappingAsync(client, accountId, SignedAmountMapping());

        var result = await ConfirmOkAsync(client, (await UploadOkAsync(client, accountId, "signed-amount.csv")).Token);

        Assert.Equal(new Confirm.Response(6, 0, 0), result);
    }

    [Fact]
    public async Task Confirm_ImportedRows_LandWithNoCategoryAndAsAWant()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Review Queue {Guid.NewGuid():N}");
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        await ConfirmOkAsync(client, preview.Token);

        var stored = await TransactionsOnAsync(client, accountId);
        Assert.Equal(6, stored.Count);
        Assert.All(stored, t => Assert.Null(t.CategoryId));
        // Even the payroll-shaped credit: an unjudged row is a guessed Want, never a guessed Need.
        Assert.All(stored, t => Assert.Equal(NeedWant.Want, t.NeedWant));
    }

    [Fact]
    public async Task Confirm_WithNoSavedMapping_RefusesAndWritesNothing()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Unmapped {Guid.NewGuid():N}");
        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        var response = await ConfirmAsync(client, preview.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await TransactionsOnAsync(client, accountId));
    }

    [Fact]
    public async Task Confirm_WithARowThatCannotBeRead_RefusesAndWritesNothing()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Unreadable {Guid.NewGuid():N}");
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "unreadable-row.csv");

        var response = await ConfirmAsync(client, preview.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("1 row could not be read", await response.Content.ReadAsStringAsync());
        Assert.Empty(await TransactionsOnAsync(client, accountId));
    }

    [Fact]
    public async Task Confirm_WhenARuleSkipsTheUnreadableRow_ImportsTheRest()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Rule Fixes It {Guid.NewGuid():N}");
        await CreateIgnoreRuleAsync(client, accountId, "STATEMENT BALANCE", IgnoreMatchType.Equals);
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "unreadable-row.csv");

        var result = await ConfirmOkAsync(client, preview.Token);

        Assert.Equal(new Confirm.Response(2, 1, 0), result);
    }

    [Fact]
    public async Task Confirm_TheSameUploadTwice_ReturnsNotFoundTheSecondTime()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Double Click {Guid.NewGuid():N}");
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");
        await ConfirmOkAsync(client, preview.Token);

        var response = await ConfirmAsync(client, preview.Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(6, (await TransactionsOnAsync(client, accountId)).Count);
    }

    [Fact]
    public async Task Confirm_WithAnUnknownToken_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await ConfirmAsync(client, "not-a-real-token");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- assignment rules at confirm (ticket 14). Every rule here is scoped to the test's own account:
    // the test class shares one database, and a global KROGER rule would classify other tests' rows.

    [Fact]
    public async Task Confirm_WithARuleSettingCategoryAndNeedWant_WritesBothOntoTheMatchingTransaction()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Assign One {Guid.NewGuid():N}");
        var groceries = await CreateCategoryAsync(client, $"Groceries {Guid.NewGuid():N}");
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "KROGER", IgnoreMatchType.Contains, groceries, NeedWant.Need, []));
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "assignment-match.csv");

        await ConfirmOkAsync(client, preview.Token);

        var kroger = (await TransactionsOnAsync(client, accountId)).Single(t => t.Description == "KROGER #442");
        Assert.Equal(groceries, kroger.CategoryId);
        Assert.Equal(NeedWant.Need, kroger.NeedWant);
    }

    [Fact]
    public async Task Confirm_WithTwoRulesMatchingOneRow_TakesTheCategoryFromOneAndUnionsTheTags()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Assign Two {Guid.NewGuid():N}");
        var travel = await CreateCategoryAsync(client, $"Travel {Guid.NewGuid():N}");
        var vacation = await CreateTagAsync(client, $"Vacation {Guid.NewGuid():N}");
        var deductible = await CreateTagAsync(client, $"Deductible {Guid.NewGuid():N}");
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "DELTA", IgnoreMatchType.Contains, travel, null, [vacation]));
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "AIR LINES", IgnoreMatchType.Contains, null, null, [deductible, vacation]));
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "assignment-match.csv");

        await ConfirmOkAsync(client, preview.Token);

        var delta = (await TransactionsOnAsync(client, accountId)).Single(t => t.Description == "DELTA AIR LINES 0062");
        Assert.Equal(travel, delta.CategoryId);
        // Neither rule judged it, so it keeps the import's guess.
        Assert.Equal(NeedWant.Want, delta.NeedWant);
        Assert.Equivalent(new[] { vacation, deductible }, delta.TagIds, strict: true);
    }

    [Fact]
    public async Task Confirm_WithTwoRulesSettingTheSameFields_TheEarlierRuleWins()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Assign Order {Guid.NewGuid():N}");
        var first = await CreateCategoryAsync(client, $"Fuel {Guid.NewGuid():N}");
        var second = await CreateCategoryAsync(client, $"Snacks {Guid.NewGuid():N}");
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "SHELL", IgnoreMatchType.StartsWith, first, NeedWant.Need, []));
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "OIL", IgnoreMatchType.Contains, second, NeedWant.Want, []));
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "assignment-match.csv");

        await ConfirmOkAsync(client, preview.Token);

        var shell = (await TransactionsOnAsync(client, accountId)).Single(t => t.Description == "SHELL OIL 5578");
        Assert.Equal(first, shell.CategoryId);
        Assert.Equal(NeedWant.Need, shell.NeedWant);
    }

    [Fact]
    public async Task Confirm_WhenAnIgnoreRuleAndAnAssignmentRuleMatchOneRow_SkipsItAndAssignsNothing()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Assign Ignored {Guid.NewGuid():N}");
        var income = await CreateCategoryAsync(client, $"Income {Guid.NewGuid():N}");
        await CreateIgnoreRuleAsync(client, accountId, "AUTOPAY", IgnoreMatchType.Contains);
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "AUTOPAY", IgnoreMatchType.Contains, income, NeedWant.Need, []));
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "assignment-match.csv");

        var result = await ConfirmOkAsync(client, preview.Token);

        Assert.Equal(new Confirm.Response(4, 1, 0), result);
        var stored = await TransactionsOnAsync(client, accountId);
        Assert.DoesNotContain(stored, t => t.Description == "AUTOPAY THANK YOU");
        Assert.DoesNotContain(stored, t => t.CategoryId == income);
    }

    [Fact]
    public async Task Confirm_RowsNoRuleMatches_ImportWithNoCategoryAsAWant()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Assign Unmatched {Guid.NewGuid():N}");
        var groceries = await CreateCategoryAsync(client, $"Groceries {Guid.NewGuid():N}");
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "KROGER", IgnoreMatchType.Contains, groceries, NeedWant.Need, []));
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "assignment-match.csv");

        await ConfirmOkAsync(client, preview.Token);

        var unmatched = (await TransactionsOnAsync(client, accountId))
            .Where(t => t.Description != "KROGER #442")
            .ToList();
        Assert.Equal(4, unmatched.Count);
        Assert.All(unmatched, t =>
        {
            Assert.Null(t.CategoryId);
            Assert.Equal(NeedWant.Want, t.NeedWant);
            Assert.Empty(t.TagIds);
        });
    }

    [Fact]
    public async Task Confirm_AppliesARuleWrittenAfterTheFileWasUploaded()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Assign Late {Guid.NewGuid():N}");
        var groceries = await CreateCategoryAsync(client, $"Groceries {Guid.NewGuid():N}");
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "assignment-match.csv");

        // Rules are read when the import runs, not when the file arrived.
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            accountId, "KROGER", IgnoreMatchType.Contains, groceries, NeedWant.Need, []));
        await ConfirmOkAsync(client, preview.Token);

        var kroger = (await TransactionsOnAsync(client, accountId)).Single(t => t.Description == "KROGER #442");
        Assert.Equal(groceries, kroger.CategoryId);
    }

    [Fact]
    public async Task Confirm_ARuleScopedToAnotherAccount_DoesNotTouchThisImport()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Assign Here {Guid.NewGuid():N}");
        var otherAccountId = await CreateAccountAsync(client, $"Assign Elsewhere {Guid.NewGuid():N}");
        var groceries = await CreateCategoryAsync(client, $"Groceries {Guid.NewGuid():N}");
        await CreateAssignmentRuleAsync(client, new CreateAssignmentRule.Command(
            otherAccountId, "KROGER", IgnoreMatchType.Contains, groceries, NeedWant.Need, []));
        await SaveMappingAsync(client, accountId, SignedAmountMapping());
        var preview = await UploadOkAsync(client, accountId, "assignment-match.csv");

        await ConfirmOkAsync(client, preview.Token);

        var kroger = (await TransactionsOnAsync(client, accountId)).Single(t => t.Description == "KROGER #442");
        Assert.Null(kroger.CategoryId);
        Assert.Equal(NeedWant.Want, kroger.NeedWant);
    }

    private static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, string token) =>
        client.PostAsync($"/api/import/previews/{token}/confirm", null);

    private static async Task<Confirm.Response> ConfirmOkAsync(HttpClient client, string token)
    {
        var response = await ConfirmAsync(client, token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Confirm.Response>(TestClientExtensions.JsonOptions))!;
    }

    private static async Task<int> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/categories", new Api.Features.Categories.Categories.Create.Command(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Api.Features.Categories.Categories.Create.Response>())!.Id;
    }

    private static async Task<List<FetchAll.Response>> TransactionsOnAsync(HttpClient client, int accountId) =>
        (await client.GetFromJsonAsync<List<FetchAll.Response>>(
            $"/api/transactions?accountId={accountId}", TestClientExtensions.JsonOptions))!;
}
