using System.Net;
using System.Net.Http.Json;
using Api.Data;
using Api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static Api.Features.Categories.Categories;
using static Api.Features.Identity.Identity;
using static Api.Features.Transactions.Transactions;

namespace Api.Tests.Features.Categories;

public class DeleteTests : IClassFixture<BudgetApiFactory>
{
    private readonly BudgetApiFactory _factory;

    public DeleteTests(BudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_WithKnownId_RemovesCategory()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var created = await client.PostAsJsonAsync("/api/categories", new Api.Features.Categories.Categories.Create.Command("Delete Target"));
        var createdCategory = (await created.Content.ReadFromJsonAsync<Api.Features.Categories.Categories.Create.Response>())!;

        var response = await client.DeleteAsync($"/api/categories/{createdCategory.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var refetched = await client.GetAsync($"/api/categories/{createdCategory.Id}");
        Assert.Equal(HttpStatusCode.NotFound, refetched.StatusCode);
    }

    [Fact]
    public async Task Delete_WithUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);

        var response = await client.DeleteAsync("/api/categories/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithAttachedTransaction_ReturnsConflictAndDoesNotDelete()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(BudgetApiFactory.SeededUser1Email, BudgetApiFactory.SeededUser1Password);
        var me = await client.GetFromJsonAsync<Me.Response>("/api/identity/me");
        var userId = me!.Id;
        var created = await client.PostAsJsonAsync("/api/categories", new Api.Features.Categories.Categories.Create.Command("Delete In-Use Category"));
        var createdCategory = (await created.Content.ReadFromJsonAsync<Api.Features.Categories.Categories.Create.Response>())!;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
            var account = new Api.Features.Accounts.Accounts.Account
            {
                Name = "Delete In-Use Account",
                Type = Api.Features.Accounts.Accounts.AccountType.Checking,
                CreatedByUserId = userId,
            };
            db.Accounts.Add(account);
            await db.SaveChangesAsync();

            db.Transactions.Add(new Transaction
            {
                AccountId = account.Id,
                CategoryId = createdCategory.Id,
                Date = new DateOnly(2026, 1, 1),
                Amount = -10.00m,
                Description = "In-use guard test",
                NeedWant = NeedWant.Want,
                CreatedByUserId = userId,
            });
            await db.SaveChangesAsync();
        }

        var response = await client.DeleteAsync($"/api/categories/{createdCategory.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var refetched = await client.GetAsync($"/api/categories/{createdCategory.Id}");
        Assert.Equal(HttpStatusCode.OK, refetched.StatusCode);
    }
}
