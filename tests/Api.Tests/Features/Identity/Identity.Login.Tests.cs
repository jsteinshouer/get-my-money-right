using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Identity.Identity;

namespace Api.Tests.Features.Identity;

public class LoginTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public LoginTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password)]
    [InlineData(BudgetApiFactory.SeededUser2Email, BudgetApiFactory.SeededUser2Password)]
    public async Task Login_WithValidSeededCredentials_ReturnsOkWithUser(string email, string password)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/identity/login", new Login.Command(email, password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Login.Response>();
        Assert.NotNull(body);
        Assert.Equal(email, body!.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/identity/login", new Login.Command(BudgetApiFactory.SeededUser1Email, "WrongPassword123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/identity/login", new Login.Command("nobody@household.local", "WhateverPassword123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/identity/login", new Login.Command(string.Empty, "SomePassword123!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
