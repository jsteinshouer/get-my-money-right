using System.Net.Http.Json;
using Api.Tests.Fixtures;
using AccountsFeature = Api.Features.Accounts.Accounts;
using CategoriesFeature = Api.Features.Categories.Categories;
using TransactionsFeature = Api.Features.Transactions.Transactions;

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

    public static async Task<int> CreateAccountAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/api/accounts", new AccountsFeature.Create.Command(name, AccountsFeature.AccountType.Checking), TestClientExtensions.JsonOptions);
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<AccountsFeature.Create.Response>(TestClientExtensions.JsonOptions))!;
        return body.Id;
    }

    public static async Task CreateTransactionAsync(HttpClient client, int accountId, int categoryId, DateOnly date, decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new TransactionsFeature.Create.Command(accountId, categoryId, date, amount, $"Txn {date:yyyy-MM-dd} {amount}", TransactionsFeature.NeedWant.Need),
            TestClientExtensions.JsonOptions);
        response.EnsureSuccessStatusCode();
    }
}
