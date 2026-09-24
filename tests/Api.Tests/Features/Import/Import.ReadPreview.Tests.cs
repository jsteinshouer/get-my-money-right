using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Import.Import;
using static Api.Tests.Features.Import.ImportTestHelpers;

namespace Api.Tests.Features.Import;

/// <summary>
/// The Map Columns step proves itself by re-reading the sampled rows through the mapping the user
/// is assembling — parsed date, description, sign-normalised amount — before anything is saved.
/// </summary>
public class ReadPreviewTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public ReadPreviewTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    private Task<HttpResponseMessage> ReadAsync(HttpClient client, string token, ReadPreview.Command command) =>
        client.PostAsJsonAsync($"/api/import/previews/{token}/reading", command, TestClientExtensions.JsonOptions);

    private async Task<ReadPreview.Response> ReadOkAsync(HttpClient client, string token, ReadPreview.Command command)
    {
        var response = await ReadAsync(client, token, command);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReadPreview.Response>(TestClientExtensions.JsonOptions))!;
    }

    [Fact]
    public async Task Read_WithASignedAmountColumn_ParsesDateDescriptionAndAmount()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Read Signed {Guid.NewGuid():N}");
        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        var reading = await ReadOkAsync(client, preview.Token, new ReadPreview.Command(
            ",", true, "MM/dd/yyyy", "Posted Date", "Payee", "Amount", null, null));

        Assert.Equal(5, reading.Rows.Count);
        Assert.Equal(new DateOnly(2026, 8, 14), reading.Rows[0].Date);
        Assert.Equal("KROGER #442", reading.Rows[0].Description);
        Assert.Equal(-84.19m, reading.Rows[0].Amount);
        Assert.Null(reading.Rows[0].Error);
        // A payment lands positive, exactly as the file has it — no sign is invented.
        Assert.Equal(312.00m, reading.Rows[3].Amount);
    }

    [Fact]
    public async Task Read_WithADebitCreditPair_NormalisesDebitsNegativeAndCreditsPositive()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Read Pair {Guid.NewGuid():N}");
        var preview = await UploadOkAsync(client, accountId, "debit-credit.csv");

        var reading = await ReadOkAsync(client, preview.Token, new ReadPreview.Command(
            ",", true, "MM/dd/yyyy", "Date", "Description", null, "Withdrawal", "Deposit"));

        Assert.Equal(-84.19m, reading.Rows[0].Amount);
        // Currency symbols and thousands separators are stripped before parsing.
        Assert.Equal(2410.55m, reading.Rows[1].Amount);
        Assert.Equal(-1650.00m, reading.Rows[2].Amount);
        Assert.Equal(45.00m, reading.Rows[3].Amount);
    }

    [Fact]
    public async Task Read_WithTheWrongDateFormat_ReportsTheRowRatherThanFailingTheRequest()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Read Wrong Date {Guid.NewGuid():N}");
        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        var reading = await ReadOkAsync(client, preview.Token, new ReadPreview.Command(
            ",", true, "yyyy-MM-dd", "Posted Date", "Payee", "Amount", null, null));

        Assert.Null(reading.Rows[0].Date);
        Assert.NotNull(reading.Rows[0].Error);
        Assert.Contains("08/14/2026", reading.Rows[0].Error);
    }

    [Fact]
    public async Task Read_WithAPartialMapping_ReturnsTheColumnsChosenSoFar()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Read Partial {Guid.NewGuid():N}");
        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        var reading = await ReadOkAsync(client, preview.Token, new ReadPreview.Command(
            ",", true, "MM/dd/yyyy", "Posted Date", null, null, null, null));

        Assert.Equal(new DateOnly(2026, 8, 14), reading.Rows[0].Date);
        Assert.Null(reading.Rows[0].Description);
        Assert.Null(reading.Rows[0].Amount);
    }

    [Fact]
    public async Task Read_WithACorrectedDelimiter_RecomputesTheColumns()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Read Delimiter {Guid.NewGuid():N}");
        var preview = await UploadOkAsync(client, accountId, "semicolon-dotted.csv");

        // Force the wrong delimiter: the whole line collapses into one column.
        var reading = await ReadOkAsync(client, preview.Token, new ReadPreview.Command(
            ",", true, "dd.MM.yyyy", null, null, null, null, null));

        Assert.Single(reading.Columns);
        Assert.Equal("Booking Date;Purpose;Amount", reading.Columns[0]);
    }

    [Fact]
    public async Task Read_WithHeaderRowTurnedOff_TreatsTheFirstLineAsData()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Read No Header {Guid.NewGuid():N}");
        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        var reading = await ReadOkAsync(client, preview.Token, new ReadPreview.Command(
            ",", false, "MM/dd/yyyy", "Column 1", "Column 3", "Column 4", null, null));

        Assert.Equal(new[] { "Column 1", "Column 2", "Column 3", "Column 4" }, reading.Columns);
        Assert.Equal("Payee", reading.Rows[0].Description);
        Assert.Null(reading.Rows[0].Date);
    }

    [Fact]
    public async Task Read_WithAnUnknownToken_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await ReadAsync(client, "deadbeefdeadbeefdeadbeefdeadbeef", new ReadPreview.Command(
            ",", true, "MM/dd/yyyy", "Date", "Description", "Amount", null, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

/// <summary>
/// Reading a money cell wrong is the one failure this whole step exists to prevent: a plausible
/// figure that is out by a factor of a hundred passes every eye it meets.
/// </summary>
public class ReadPreviewAmountTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public ReadPreviewAmountTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<ReadPreview.Response> ReadAsync(
        string fixture, string accountName, ReadPreview.Command command)
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"{accountName} {Guid.NewGuid():N}");
        var preview = await UploadOkAsync(client, accountId, fixture);
        var response = await client.PostAsJsonAsync(
            $"/api/import/previews/{preview.Token}/reading", command, TestClientExtensions.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReadPreview.Response>(TestClientExtensions.JsonOptions))!;
    }

    [Fact]
    public async Task Read_WithDecimalCommaAmounts_ReadsThemAsWrittenRatherThanAHundredTimesOver()
    {
        var reading = await ReadAsync("decimal-comma.csv", "Decimal Comma", new ReadPreview.Command(
            ";", true, "dd.MM.yyyy", "Booking Date", "Purpose", "Amount", null, null));

        Assert.Equal(-84.19m, reading.Rows[0].Amount);
        Assert.Equal(-1650.00m, reading.Rows[1].Amount);
        Assert.Equal(2410.55m, reading.Rows[2].Amount);
    }

    [Fact]
    public async Task Read_WithZeroFilledDebitCreditColumns_TakesTheSideCarryingTheFigure()
    {
        var reading = await ReadAsync("zero-filled-pair.csv", "Zero Filled", new ReadPreview.Command(
            ",", true, "MM/dd/yyyy", "Date", "Description", null, "Withdrawal", "Deposit"));

        Assert.Equal(-84.19m, reading.Rows[0].Amount);
        Assert.Equal(2410.55m, reading.Rows[1].Amount);
        Assert.Equal(-1650.00m, reading.Rows[2].Amount);
        Assert.All(reading.Rows, row => Assert.Null(row.Error));
    }

    [Fact]
    public async Task Read_WithARepeatedColumnName_KeepsTheColumnsSeparatelyMappable()
    {
        var reading = await ReadAsync("repeated-column-name.csv", "Repeated Name", new ReadPreview.Command(
            ",", true, "yyyy-MM-dd", "Date", "Description", "Amount (2)", null, null));

        Assert.Equal(new[] { "Date", "Description", "Amount", "Amount (2)" }, reading.Columns);
        // The running-balance column, not the transaction amount beside it.
        Assert.Equal(120.00m, reading.Rows[0].Amount);
    }

    [Fact]
    public async Task Read_NormalisesTheWhitespaceInsideADescription()
    {
        var reading = await ReadAsync("padded-description.csv", "Padded", new ReadPreview.Command(
            ",", true, "MM/dd/yyyy", "Posted Date", "Payee", "Amount", null, null));

        // The description is shown in the one spelling the app will store it in and match rules
        // against. A rule typed from what this row shows has to catch this row.
        Assert.Equal("KROGER #442", reading.Rows[0].Description);
        Assert.Equal("SHELL OIL 5578", reading.Rows[1].Description);
    }

    [Fact]
    public async Task Read_WithAnUnsupportedDelimiter_IsRejectedRatherThanThrowing()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Bad Delimiter {Guid.NewGuid():N}");
        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        var response = await client.PostAsJsonAsync(
            $"/api/import/previews/{preview.Token}/reading",
            new ReadPreview.Command("~~", true, "MM/dd/yyyy", "Posted Date", "Payee", "Amount", null, null),
            TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
