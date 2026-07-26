using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Accounts.Accounts;

namespace Api.Tests.Features.Accounts;

public class FetchOneTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public FetchOneTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FetchOne_WithKnownId_ReturnsAccount()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var created = await client.PostAsJsonAsync(
            "/api/accounts", new Create.Command("FetchOne Target", AccountType.Savings), TestClientExtensions.JsonOptions);
        var createdAccount = (await created.Content.ReadFromJsonAsync<Create.Response>(TestClientExtensions.JsonOptions))!;

        var response = await client.GetAsync($"/api/accounts/{createdAccount.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FetchOne.Response>(TestClientExtensions.JsonOptions);
        Assert.Equal("FetchOne Target", body!.Name);
        Assert.Equal(AccountType.Savings, body.Type);
    }

    [Fact]
    public async Task FetchOne_WithUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.GetAsync("/api/accounts/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
