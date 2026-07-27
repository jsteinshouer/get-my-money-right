using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Categories.Categories;

namespace Api.Tests.Features.Categories;

public class UpdateTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public UpdateTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Update_WithKnownId_ChangesName()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var created = await client.PostAsJsonAsync("/api/categories", new Create.Command("Update Original Name"));
        var createdCategory = (await created.Content.ReadFromJsonAsync<Create.Response>())!;

        var response = await client.PutAsJsonAsync($"/api/categories/{createdCategory.Id}", new Update.Command("Update Renamed"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Update.Response>();
        Assert.Equal("Update Renamed", body!.Name);

        var refetched = await client.GetFromJsonAsync<FetchOne.Response>($"/api/categories/{createdCategory.Id}");
        Assert.Equal("Update Renamed", refetched!.Name);
    }

    [Fact]
    public async Task Update_WithUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.PutAsJsonAsync("/api/categories/999999", new Update.Command("Doesn't Matter"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ToNameUsedByAnotherCategory_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        await client.PostAsJsonAsync("/api/categories", new Create.Command("Update Existing Name"));
        var created = await client.PostAsJsonAsync("/api/categories", new Create.Command("Update Target For Rename"));
        var createdCategory = (await created.Content.ReadFromJsonAsync<Create.Response>())!;

        var response = await client.PutAsJsonAsync($"/api/categories/{createdCategory.Id}", new Update.Command("Update Existing Name"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
