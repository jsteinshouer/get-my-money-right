using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Tags.Tags;

namespace Api.Tests.Features.Tags;

/// <summary>
/// Tags are typed rather than picked, so case and stray whitespace are the two ways a household
/// ends up with "Vacation" and "vacation" meaning the same thing and counting separately.
/// </summary>
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
        (await client.PostAsJsonAsync("/api/tags", new Create.Command("Case Vacation Tag"))).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/tags", new Create.Command("case vacation tag"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_TrimsSurroundingWhitespace()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.PostAsJsonAsync("/api/tags", new Create.Command("   Trimmed Tag   "));

        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<Create.Response>())!;
        Assert.Equal("Trimmed Tag", body.Name);
    }

    [Fact]
    public async Task Create_WithNameDifferingOnlyByWhitespace_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        (await client.PostAsJsonAsync("/api/tags", new Create.Command("Padded Tag"))).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/tags", new Create.Command("  Padded Tag  "));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithWhitespaceOnlyName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.PostAsJsonAsync("/api/tags", new Create.Command("     "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FetchAll_SortsWithoutRegardToCase()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        await TagsTestHelpers.CreateTagAsync(client, "zzz sort lowercase last");
        await TagsTestHelpers.CreateTagAsync(client, "ZZZ Sort Uppercase First");

        var list = (await client.GetFromJsonAsync<List<FetchAll.Response>>("/api/tags"))!;

        var sortNames = list.Where(t => t.Name.StartsWith("zzz sort", StringComparison.OrdinalIgnoreCase)).Select(t => t.Name).ToList();
        // Case-sensitive ordering would put every capitalised name ahead of every lowercase one.
        Assert.Equal(sortNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase), sortNames);
    }
}
