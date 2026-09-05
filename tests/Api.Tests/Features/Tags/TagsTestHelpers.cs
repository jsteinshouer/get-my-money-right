using System.Net.Http.Json;
using Api.Tests.Fixtures;
using AccountsFeature = Api.Features.Accounts.Accounts;
using CategoriesFeature = Api.Features.Categories.Categories;
using TagsFeature = Api.Features.Tags.Tags;
using TransactionsFeature = Api.Features.Transactions.Transactions;

namespace Api.Tests.Features.Tags;

internal static class TagsTestHelpers
{
    public static async Task<int> CreateTagAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/tags", new TagsFeature.Create.Command(name));
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<TagsFeature.Create.Response>())!;
        return body.Id;
    }

    /// <summary>Creates the account and category a transaction needs, then the transaction itself.</summary>
    public static async Task<int> CreateTransactionAsync(HttpClient client, string label)
    {
        var accountResponse = await client.PostAsJsonAsync(
            "/api/accounts",
            new AccountsFeature.Create.Command($"{label} Account", AccountsFeature.AccountType.Checking),
            TestClientExtensions.JsonOptions);
        accountResponse.EnsureSuccessStatusCode();
        var account = (await accountResponse.Content.ReadFromJsonAsync<AccountsFeature.Create.Response>(TestClientExtensions.JsonOptions))!;

        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new CategoriesFeature.Create.Command($"{label} Category"));
        categoryResponse.EnsureSuccessStatusCode();
        var category = (await categoryResponse.Content.ReadFromJsonAsync<CategoriesFeature.Create.Response>())!;

        var transactionResponse = await client.PostAsJsonAsync(
            "/api/transactions",
            new TransactionsFeature.Create.Command(
                account.Id, category.Id, new DateOnly(2026, 2, 14), -12.50m, $"{label} Transaction", TransactionsFeature.NeedWant.Want),
            TestClientExtensions.JsonOptions);
        transactionResponse.EnsureSuccessStatusCode();
        var transaction = (await transactionResponse.Content.ReadFromJsonAsync<TransactionsFeature.Create.Response>(TestClientExtensions.JsonOptions))!;
        return transaction.Id;
    }
}
