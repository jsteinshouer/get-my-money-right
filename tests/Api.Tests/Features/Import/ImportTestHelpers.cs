using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Tests.Fixtures;
using static Api.Features.Accounts.Accounts;
using static Api.Features.Import.Import;

namespace Api.Tests.Features.Import;

public static class ImportTestHelpers
{
    /// <summary>The sample CSVs live beside the test assembly; see Api.Tests.csproj.</summary>
    public static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Csv", fileName);

    public static async Task<int> CreateAccountAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/api/accounts", new Create.Command(name, AccountType.CreditCard), TestClientExtensions.JsonOptions);
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<Create.Response>(TestClientExtensions.JsonOptions);
        return account!.Id;
    }

    public static Task<HttpResponseMessage> UploadAsync(HttpClient client, int accountId, string fixtureFileName) =>
        UploadBytesAsync(client, accountId, fixtureFileName, File.ReadAllBytes(FixturePath(fixtureFileName)));

    public static async Task<HttpResponseMessage> UploadBytesAsync(
        HttpClient client, int accountId, string fileName, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", fileName);
        form.Add(new StringContent(accountId.ToString()), "accountId");
        return await client.PostAsync("/api/import/preview", form);
    }

    public static async Task<UploadPreview.Response> UploadOkAsync(
        HttpClient client, int accountId, string fixtureFileName)
    {
        var response = await UploadAsync(client, accountId, fixtureFileName);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UploadPreview.Response>(TestClientExtensions.JsonOptions))!;
    }

    public static async Task<HttpClient> LoggedInClientAsync(BudgetApiFactory factory)
    {
        var client = factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        return client;
    }
}
