using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Identity.Identity;

namespace Api.Tests.Features.Identity;

public class MeTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public MeTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Me_WithoutSession_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/identity/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_AfterLogin_ReturnsOkWithCurrentUser()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/identity/login",
            new Login.Command(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password));

        var response = await client.GetAsync("/api/identity/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Me.Response>();
        Assert.NotNull(body);
        Assert.Equal(BudgetApiFactory.SeededUser1Email, body!.Email);
    }
}
