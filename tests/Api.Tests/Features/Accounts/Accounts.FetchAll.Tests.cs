using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Accounts.Accounts;

namespace Api.Tests.Features.Accounts;

public class FetchAllTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public FetchAllTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FetchAll_DefaultsToExcludingDeactivatedAccounts()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var active = await CreateAccountAsync(client, "FetchAll Active");
        var inactive = await CreateAccountAsync(client, "FetchAll Inactive");
        await client.PostAsync($"/api/accounts/{inactive.Id}/deactivate", content: null);

        var defaultList = await client.GetFromJsonAsync<List<FetchAll.Response>>("/api/accounts", TestClientExtensions.JsonOptions);
        Assert.NotNull(defaultList);
        Assert.Contains(defaultList!, a => a.Id == active.Id);
        Assert.DoesNotContain(defaultList!, a => a.Id == inactive.Id);

        var fullList = await client.GetFromJsonAsync<List<FetchAll.Response>>(
            "/api/accounts?includeInactive=true", TestClientExtensions.JsonOptions);
        Assert.NotNull(fullList);
        Assert.Contains(fullList!, a => a.Id == active.Id);
        Assert.Contains(fullList!, a => a.Id == inactive.Id && !a.IsActive);
    }

    private static async Task<Create.Response> CreateAccountAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/api/accounts", new Create.Command(name, AccountType.Checking), TestClientExtensions.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Create.Response>(TestClientExtensions.JsonOptions))!;
    }
}
