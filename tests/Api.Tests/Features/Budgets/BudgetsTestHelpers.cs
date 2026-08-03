using System.Net.Http.Json;
using CategoriesFeature = Api.Features.Categories.Categories;

namespace Api.Tests.Features.Budgets;

internal static class BudgetsTestHelpers
{
    public static async Task<int> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/categories", new CategoriesFeature.Create.Command(name));
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<CategoriesFeature.Create.Response>())!;
        return body.Id;
    }
}
