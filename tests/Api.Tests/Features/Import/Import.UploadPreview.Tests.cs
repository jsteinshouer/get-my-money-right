using System.Net;
using System.Net.Http.Json;
using System.Text;
using Api.Tests.Fixtures;
using static Api.Features.Import.Import;
using static Api.Tests.Features.Import.ImportTestHelpers;

namespace Api.Tests.Features.Import;

public class UploadPreviewTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public UploadPreviewTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Upload_ReturnsColumnsAndSampleRows_AndInsertsNoTransactions()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Upload Preview {Guid.NewGuid():N}");

        var before = await client.GetStringAsync("/api/transactions");
        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");
        var after = await client.GetStringAsync("/api/transactions");

        Assert.NotEmpty(preview.Token);
        Assert.Equal(new[] { "Posted Date", "Reference", "Payee", "Amount" }, preview.Columns);
        Assert.Equal(5, preview.SampleRows.Count);
        Assert.Equal(new[] { "08/14/2026", "4471", "KROGER #442", "-84.19" }, preview.SampleRows[0]);
        // Quoted fields keep their embedded delimiter instead of splitting into a new column.
        Assert.Equal("BOOKS, ETC", preview.SampleRows[2][2]);
        // Preview parses only: the transaction list is byte-for-byte what it was.
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Upload_DetectsCommaDelimiterHeaderRowAndDateFormat()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Detect Comma {Guid.NewGuid():N}");

        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        Assert.Equal(",", preview.Delimiter);
        Assert.True(preview.HasHeaderRow);
        Assert.Equal("MM/dd/yyyy", preview.DateFormat);
    }

    [Fact]
    public async Task Upload_DetectsSemicolonDelimiterAndDottedDateFormat()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Detect Semicolon {Guid.NewGuid():N}");

        var preview = await UploadOkAsync(client, accountId, "semicolon-dotted.csv");

        Assert.Equal(";", preview.Delimiter);
        Assert.Equal("dd.MM.yyyy", preview.DateFormat);
        Assert.Equal(new[] { "Booking Date", "Purpose", "Amount" }, preview.Columns);
    }

    [Fact]
    public async Task Upload_StripsUtf8Bom_FromTheFirstColumnName()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Bom {Guid.NewGuid():N}");

        var preview = await UploadOkAsync(client, accountId, "bom.csv");

        Assert.Equal("Date", preview.Columns[0]);
        Assert.Equal("yyyy-MM-dd", preview.DateFormat);
    }

    [Fact]
    public async Task Upload_WithoutHeaderRow_NamesColumnsPositionally()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Headerless {Guid.NewGuid():N}");

        var preview = await UploadOkAsync(client, accountId, "headerless.csv");

        Assert.False(preview.HasHeaderRow);
        Assert.Equal(new[] { "Column 1", "Column 2", "Column 3" }, preview.Columns);
        // The first line is data, so it must still appear among the sample rows.
        Assert.Equal(new[] { "2026-08-14", "CORNER STORE", "-9.99" }, preview.SampleRows[0]);
    }

    [Fact]
    public async Task Upload_WithNoSavedMapping_ReturnsNoSavedValues()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"No Mapping {Guid.NewGuid():N}");

        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        Assert.Null(preview.Saved);
        Assert.Empty(preview.MissingColumns);
    }

    [Fact]
    public async Task Upload_AfterSavingAMapping_PreFillsFromTheSavedValues()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Remembered {Guid.NewGuid():N}");
        var save = await client.PutAsJsonAsync(
            $"/api/import/mappings/{accountId}",
            new SaveMapping.Command("Posted Date", "Payee", "Amount", null, null, "MM/dd/yyyy", ",", true),
            TestClientExtensions.JsonOptions);
        save.EnsureSuccessStatusCode();

        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        Assert.NotNull(preview.Saved);
        Assert.Equal("Posted Date", preview.Saved!.DateColumn);
        Assert.Equal("Payee", preview.Saved.DescriptionColumn);
        Assert.Equal("Amount", preview.Saved.AmountColumn);
        Assert.Null(preview.Saved.DebitColumn);
        Assert.Equal("MM/dd/yyyy", preview.Saved.DateFormat);
        Assert.Empty(preview.MissingColumns);
    }

    [Fact]
    public async Task Upload_WhenASavedColumnIsAbsentFromTheFile_NamesTheMissingColumn()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Drifted {Guid.NewGuid():N}");
        var save = await client.PutAsJsonAsync(
            $"/api/import/mappings/{accountId}",
            new SaveMapping.Command("Date", "Description", null, "Withdrawal", "Deposit", "MM/dd/yyyy", ",", true),
            TestClientExtensions.JsonOptions);
        save.EnsureSuccessStatusCode();

        // The bank now exports a single signed column, so Withdrawal and Deposit are gone.
        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        Assert.NotNull(preview.Saved);
        Assert.Equal(new[] { "Date", "Description", "Withdrawal", "Deposit" }, preview.MissingColumns);
    }

    [Fact]
    public async Task Upload_ForAnUnknownAccount_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await UploadAsync(client, 999999, "signed-amount.csv");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithAnEmptyFile_ReturnsBadRequest()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Empty {Guid.NewGuid():N}");

        var response = await UploadBytesAsync(client, accountId, "empty.csv", Array.Empty<byte>());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithASingleColumnFile_ReturnsBadRequest()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"One Column {Guid.NewGuid():N}");

        var response = await UploadBytesAsync(
            client, accountId, "notes.csv", Encoding.UTF8.GetBytes("just one column\nand one value\n"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithHeaderButNoDataRows_ReturnsBadRequest()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Header Only {Guid.NewGuid():N}");

        var response = await UploadBytesAsync(
            client, accountId, "header-only.csv", Encoding.UTF8.GetBytes("Date,Description,Amount\n"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithoutAuthentication_IsRejected()
    {
        var client = _factory.CreateClient();

        var response = await UploadAsync(client, 1, "signed-amount.csv");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

/// <summary>
/// A remembered mapping describes its own file. Re-detecting the shape on every upload lets a guess
/// contradict what the household already told us, and reports a file that has not changed as broken.
/// </summary>
public class UploadPreviewRemembersFormatTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public UploadPreviewRemembersFormatTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Upload_WithAMappingSavedAgainstPositionalColumns_ReportsNothingMissing()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"No Header Remembered {Guid.NewGuid():N}");

        // The household turned the header row off for a file whose first line only looks like names.
        var save = await client.PutAsJsonAsync(
            $"/api/import/mappings/{accountId}",
            new SaveMapping.Command("Column 1", "Column 3", "Column 4", null, null, "MM/dd/yyyy", ",", false),
            TestClientExtensions.JsonOptions);
        save.EnsureSuccessStatusCode();

        var preview = await UploadOkAsync(client, accountId, "signed-amount.csv");

        Assert.False(preview.HasHeaderRow);
        Assert.Equal(new[] { "Column 1", "Column 2", "Column 3", "Column 4" }, preview.Columns);
        Assert.Empty(preview.MissingColumns);
        Assert.Equal("MM/dd/yyyy", preview.DateFormat);
    }

    [Fact]
    public async Task Upload_WithARepeatedColumnName_MakesEachColumnAddressable()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Repeated Header {Guid.NewGuid():N}");

        var preview = await UploadOkAsync(client, accountId, "repeated-column-name.csv");

        Assert.Equal(new[] { "Date", "Description", "Amount", "Amount (2)" }, preview.Columns);
    }
}
