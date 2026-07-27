using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Categories.Categories;

namespace Api.Tests.Features.Categories;

public class FetchAllTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public FetchAllTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FetchAll_ReturnsCreatedCategories()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var created = await CreateCategoryAsync(client, "FetchAll Category");

        var list = await client.GetFromJsonAsync<List<FetchAll.Response>>("/api/categories");
        Assert.NotNull(list);
        Assert.Contains(list!, c => c.Id == created.Id && c.Name == "FetchAll Category");
    }

    private static async Task<Create.Response> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/categories", new Create.Command(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Create.Response>())!;
    }
}
