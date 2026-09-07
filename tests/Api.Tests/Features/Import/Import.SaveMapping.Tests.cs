using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Import.Import;
using static Api.Tests.Features.Import.ImportTestHelpers;

namespace Api.Tests.Features.Import;

public class SaveMappingTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public SaveMappingTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    private static SaveMapping.Command SignedAmount() =>
        new("Posted Date", "Payee", "Amount", null, null, "MM/dd/yyyy", ",", true);

    private Task<HttpResponseMessage> SaveAsync(HttpClient client, int accountId, SaveMapping.Command command) =>
        client.PutAsJsonAsync($"/api/import/mappings/{accountId}", command, TestClientExtensions.JsonOptions);

    [Fact]
    public async Task Save_ThenFetch_ReturnsTheStoredMapping()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Save Then Fetch {Guid.NewGuid():N}");

        var save = await SaveAsync(client, accountId, SignedAmount());
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        var fetch = await client.GetAsync($"/api/import/mappings/{accountId}");

        Assert.Equal(HttpStatusCode.OK, fetch.StatusCode);
        var mapping = await fetch.Content.ReadFromJsonAsync<FetchMapping.Response>(TestClientExtensions.JsonOptions);
        Assert.Equal("Posted Date", mapping!.DateColumn);
        Assert.Equal("Payee", mapping.DescriptionColumn);
        Assert.Equal("Amount", mapping.AmountColumn);
        Assert.Null(mapping.DebitColumn);
        Assert.Null(mapping.CreditColumn);
        Assert.Equal("MM/dd/yyyy", mapping.DateFormat);
        Assert.Equal(",", mapping.Delimiter);
        Assert.True(mapping.HasHeaderRow);
    }

    [Fact]
    public async Task Save_Twice_ReplacesTheMappingRatherThanAddingASecond()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Save Twice {Guid.NewGuid():N}");

        var first = await SaveAsync(client, accountId, SignedAmount());
        var firstBody = await first.Content.ReadFromJsonAsync<SaveMapping.Response>(TestClientExtensions.JsonOptions);

        var second = await SaveAsync(
            client, accountId, new SaveMapping.Command("Date", "Description", null, "Withdrawal", "Deposit", "yyyy-MM-dd", ";", true));
        var secondBody = await second.Content.ReadFromJsonAsync<SaveMapping.Response>(TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(firstBody!.Id, secondBody!.Id);

        var mapping = await client.GetFromJsonAsync<FetchMapping.Response>(
            $"/api/import/mappings/{accountId}", TestClientExtensions.JsonOptions);
        Assert.Equal("Withdrawal", mapping!.DebitColumn);
        Assert.Equal("Deposit", mapping.CreditColumn);
        Assert.Null(mapping.AmountColumn);
        Assert.Equal(";", mapping.Delimiter);
    }

    [Fact]
    public async Task Save_WithNeitherAnAmountColumnNorADebitCreditPair_IsRejected()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"No Amount {Guid.NewGuid():N}");

        var response = await SaveAsync(
            client, accountId, new SaveMapping.Command("Posted Date", "Payee", null, null, null, "MM/dd/yyyy", ",", true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Save_WithBothAnAmountColumnAndADebitCreditPair_IsRejected()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Both Amount {Guid.NewGuid():N}");

        var response = await SaveAsync(
            client, accountId, new SaveMapping.Command("Posted Date", "Payee", "Amount", "Withdrawal", "Deposit", "MM/dd/yyyy", ",", true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Save_WithOnlyOneHalfOfTheDebitCreditPair_IsRejected()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Half Pair {Guid.NewGuid():N}");

        var response = await SaveAsync(
            client, accountId, new SaveMapping.Command("Date", "Description", null, "Withdrawal", null, "MM/dd/yyyy", ",", true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Save_WithAnUnparseableDateFormat_IsRejected()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Bad Format {Guid.NewGuid():N}");

        var response = await SaveAsync(
            client, accountId, new SaveMapping.Command("Posted Date", "Payee", "Amount", null, null, "", ",", true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Save_ForAnUnknownAccount_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);

        var response = await SaveAsync(client, 999999, SignedAmount());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Fetch_ForAnAccountWithNoMapping_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);
        var accountId = await CreateAccountAsync(client, $"Never Mapped {Guid.NewGuid():N}");

        var response = await client.GetAsync($"/api/import/mappings/{accountId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Fetch_WithoutAuthentication_IsRejected()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/import/mappings/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
