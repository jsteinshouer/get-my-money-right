using System.Net.Http.Json;
using Api.Tests.Fixtures;
using AccountsFeature = Api.Features.Accounts.Accounts;
using CategoriesFeature = Api.Features.Categories.Categories;
using TagsFeature = Api.Features.Tags.Tags;

namespace Api.Tests.Features.Transactions;

internal static class TransactionsTestHelpers
{
    public static async Task<int> CreateAccountAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/api/accounts", new AccountsFeature.Create.Command(name, AccountsFeature.AccountType.Checking), TestClientExtensions.JsonOptions);
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<AccountsFeature.Create.Response>(TestClientExtensions.JsonOptions))!;
        return body.Id;
    }

    public static async Task<int> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/categories", new CategoriesFeature.Create.Command(name));
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<CategoriesFeature.Create.Response>())!;
        return body.Id;
    }

    public static async Task<int> CreateTagAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/tags", new TagsFeature.Create.Command(name));
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<TagsFeature.Create.Response>())!;
        return body.Id;
    }
}
