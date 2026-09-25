using System.Net;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Import.Import;
using static Api.Tests.Features.Import.ImportTestHelpers;

namespace Api.Tests.Features.Import;

/// <summary>
/// There is no active/inactive toggle in this ticket: deleting a rule is how it is switched off,
/// so the delete has to actually take it out of the list the preview reads.
/// </summary>
public class DeleteIgnoreRuleTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public DeleteIgnoreRuleTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_RemovesTheRuleFromTheList()
    {
        var client = await LoggedInClientAsync(_factory);
        var text = $"DELETE ME {Guid.NewGuid():N}";
        var rule = await CreateIgnoreRuleAsync(client, null, text, IgnoreMatchType.Contains);

        var response = await client.DeleteAsync($"/api/import/ignore-rules/{rule.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var rules = (await client.GetFromJsonAsync<List<FetchAllIgnoreRules.Response>>(
            "/api/import/ignore-rules", TestClientExtensions.JsonOptions))!;
        Assert.DoesNotContain(rules, r => r.Id == rule.Id);
    }

    [Fact]
    public async Task Delete_AfterTheRuleIsGone_ReturnsNotFound()
    {
        var client = await LoggedInClientAsync(_factory);
        var rule = await CreateIgnoreRuleAsync(client, null, $"DELETE TWICE {Guid.NewGuid():N}", IgnoreMatchType.Contains);
        (await client.DeleteAsync($"/api/import/ignore-rules/{rule.Id}")).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/import/ignore-rules/{rule.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_FreesTheMatchTextToBeWrittenAgain()
    {
        var client = await LoggedInClientAsync(_factory);
        var text = $"REWRITTEN {Guid.NewGuid():N}";
        var rule = await CreateIgnoreRuleAsync(client, null, text, IgnoreMatchType.Contains);
        (await client.DeleteAsync($"/api/import/ignore-rules/{rule.Id}")).EnsureSuccessStatusCode();

        var response = await PostIgnoreRuleAsync(client, null, text, IgnoreMatchType.Contains);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
