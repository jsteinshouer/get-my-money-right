using System.Reflection;
using Api.Features.Accounts;
using Api.Features.Budgets;
using Api.Features.Categories;
using Api.Features.Identity;
using Api.Features.Tags;
using Api.Features.Transactions;
using FluentValidation;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Api.Features;

public static class Features
{
    public static IServiceCollection AddFeatures(this IServiceCollection services) => services
        .AddFluentValidationAutoValidation()
        .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly())
        .AddIdentityFeature()
        .AddAccountsFeature()
        .AddCategoriesFeature()
        .AddTransactionsFeature()
        .AddBudgetsFeature()
        .AddTagsFeature();

    public static IEndpointRouteBuilder MapFeatures(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");
        group.MapIdentityFeature();
        group.MapAccountsFeature();
        group.MapCategoriesFeature();
        group.MapTransactionsFeature();
        group.MapBudgetsFeature();
        group.MapTagsFeature();
        return endpoints;
    }

    public static async Task SeedFeaturesAsync(this WebApplication app)
    {
        await app.SeedIdentityAsync();
    }
}
