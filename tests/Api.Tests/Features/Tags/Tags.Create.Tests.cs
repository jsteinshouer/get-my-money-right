using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Tags.Tags;

namespace Api.Tests.Features.Tags;

public class CreateTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public CreateTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithValidCommand_ReturnsCreatedWithTag()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.PostAsJsonAsync("/api/tags", new Create.Command("Vacation - Create Test"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Create.Response>();
        Assert.NotNull(body);
        Assert.Equal("Vacation - Create Test", body!.Name);
        Assert.Equal($"/api/tags/{body.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Create_WithoutSession_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tags", new Create.Command("No Session Tag"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.PostAsJsonAsync("/api/tags", new Create.Command(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicateName_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        await client.PostAsJsonAsync("/api/tags", new Create.Command("Duplicate Tag Test"));

        var response = await client.PostAsJsonAsync("/api/tags", new Create.Command("Duplicate Tag Test"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
