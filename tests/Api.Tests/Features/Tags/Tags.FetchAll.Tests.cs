using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Tags.Tags;

namespace Api.Tests.Features.Tags;

public class FetchAllTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public FetchAllTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FetchAll_ReturnsCreatedTagsInNameOrder()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        await TagsTestHelpers.CreateTagAsync(client, "FetchAll Zeta Tag");
        await TagsTestHelpers.CreateTagAsync(client, "FetchAll Alpha Tag");

        var list = await client.GetFromJsonAsync<List<FetchAll.Response>>("/api/tags");

        Assert.NotNull(list);
        Assert.Contains(list!, t => t.Name == "FetchAll Alpha Tag");
        Assert.Contains(list!, t => t.Name == "FetchAll Zeta Tag");
        var names = list!.Select(t => t.Name).ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task FetchAll_WithoutSession_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tags");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
