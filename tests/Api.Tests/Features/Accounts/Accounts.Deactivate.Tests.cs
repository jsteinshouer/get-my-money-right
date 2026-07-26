using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Accounts.Accounts;

namespace Api.Tests.Features.Accounts;

public class DeactivateTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public DeactivateTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Deactivate_WithKnownId_SetsIsActiveFalseWithoutDeleting()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var created = await client.PostAsJsonAsync(
            "/api/accounts", new Create.Command("Deactivate Target", AccountType.Savings), TestClientExtensions.JsonOptions);
        var createdAccount = (await created.Content.ReadFromJsonAsync<Create.Response>(TestClientExtensions.JsonOptions))!;

        var response = await client.PostAsync($"/api/accounts/{createdAccount.Id}/deactivate", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var refetched = await client.GetFromJsonAsync<FetchOne.Response>(
            $"/api/accounts/{createdAccount.Id}", TestClientExtensions.JsonOptions);
        Assert.NotNull(refetched);
        Assert.False(refetched!.IsActive);
    }

    [Fact]
    public async Task Deactivate_WithUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.PostAsync("/api/accounts/999999/deactivate", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
