using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Accounts.Accounts;

namespace Api.Tests.Features.Accounts;

public class CreateTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public CreateTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithValidCommand_ReturnsCreatedWithAccount()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.PostAsJsonAsync(
            "/api/accounts", new Create.Command("Checking - Create Test", AccountType.Checking), TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Create.Response>(TestClientExtensions.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Checking - Create Test", body!.Name);
        Assert.Equal(AccountType.Checking, body.Type);
        Assert.True(body.IsActive);
        Assert.Equal($"/api/accounts/{body.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Create_WithoutSession_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/accounts", new Create.Command("No Session Account", AccountType.Savings), TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.PostAsJsonAsync(
            "/api/accounts", new Create.Command(string.Empty, AccountType.CreditCard), TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
