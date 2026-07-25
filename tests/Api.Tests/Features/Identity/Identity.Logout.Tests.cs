using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Identity.Identity;

namespace Api.Tests.Features.Identity;

public class LogoutTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public LogoutTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Logout_AfterLogin_EndsSessionSoMeReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/identity/login",
            new Login.Command(BudgetApiFactory.SeededUser2Email, BudgetApiFactory.SeededUser2Password));

        var logoutResponse = await client.PostAsync("/api/identity/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/identity/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }
}
