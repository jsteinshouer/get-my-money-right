using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Categories.Categories;

namespace Api.Tests.Features.Categories;

/// <summary>Categories carry the same typed-name hazard as tags, and are fixed the same way.</summary>
public class CreateUniquenessTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public CreateUniquenessTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithNameDifferingOnlyByCase_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        (await client.PostAsJsonAsync("/api/categories", new Create.Command("Case Groceries Category"))).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/categories", new Create.Command("case groceries category"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_TrimsSurroundingWhitespace()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.PostAsJsonAsync("/api/categories", new Create.Command("   Trimmed Category   "));

        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<Create.Response>())!;
        Assert.Equal("Trimmed Category", body.Name);
    }

    [Fact]
    public async Task Update_TrimsAndRejectsACaseOnlyDuplicate()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        (await client.PostAsJsonAsync("/api/categories", new Create.Command("Update Target Existing"))).EnsureSuccessStatusCode();
        var created = await client.PostAsJsonAsync("/api/categories", new Create.Command("Update Target Renamed"));
        var category = (await created.Content.ReadFromJsonAsync<Create.Response>())!;

        var trimmed = await client.PutAsJsonAsync($"/api/categories/{category.Id}", new Update.Command("  Update Target Trimmed  "));
        trimmed.EnsureSuccessStatusCode();
        Assert.Equal("Update Target Trimmed", (await trimmed.Content.ReadFromJsonAsync<Update.Response>())!.Name);

        var clashing = await client.PutAsJsonAsync($"/api/categories/{category.Id}", new Update.Command("update target existing"));
        Assert.Equal(HttpStatusCode.Conflict, clashing.StatusCode);
    }
}
