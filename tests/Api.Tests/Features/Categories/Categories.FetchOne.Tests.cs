using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Categories.Categories;

namespace Api.Tests.Features.Categories;

public class FetchOneTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public FetchOneTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FetchOne_WithKnownId_ReturnsCategory()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var created = await client.PostAsJsonAsync("/api/categories", new Create.Command("FetchOne Target"));
        var createdCategory = (await created.Content.ReadFromJsonAsync<Create.Response>())!;

        var response = await client.GetAsync($"/api/categories/{createdCategory.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FetchOne.Response>();
        Assert.Equal("FetchOne Target", body!.Name);
    }

    [Fact]
    public async Task FetchOne_WithUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.GetAsync("/api/categories/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
