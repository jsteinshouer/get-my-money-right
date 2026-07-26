using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Accounts.Accounts;

namespace Api.Tests.Features.Accounts;

public class UpdateTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public UpdateTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Update_WithKnownId_ChangesNameAndType()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var created = await client.PostAsJsonAsync(
            "/api/accounts", new Create.Command("Update Original Name", AccountType.Checking), TestClientExtensions.JsonOptions);
        var createdAccount = (await created.Content.ReadFromJsonAsync<Create.Response>(TestClientExtensions.JsonOptions))!;

        var response = await client.PutAsJsonAsync(
            $"/api/accounts/{createdAccount.Id}",
            new Update.Command("Update Renamed", AccountType.CreditCard),
            TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Update.Response>(TestClientExtensions.JsonOptions);
        Assert.Equal("Update Renamed", body!.Name);
        Assert.Equal(AccountType.CreditCard, body.Type);

        var refetched = await client.GetFromJsonAsync<FetchOne.Response>(
            $"/api/accounts/{createdAccount.Id}", TestClientExtensions.JsonOptions);
        Assert.Equal("Update Renamed", refetched!.Name);
    }

    [Fact]
    public async Task Update_WithUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.PutAsJsonAsync(
            "/api/accounts/999999", new Update.Command("Doesn't Matter", AccountType.Savings), TestClientExtensions.JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
