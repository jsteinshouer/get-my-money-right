using System.Reflection;
using Api.Features.Identity;
using FluentValidation;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Api.Features;

public static class Features
{
    public static IServiceCollection AddFeatures(this IServiceCollection services) => services
        .AddFluentValidationAutoValidation()
        .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly())
        .AddIdentityFeature();

    public static IEndpointRouteBuilder MapFeatures(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");
        group.MapIdentityFeature();
        return endpoints;
    }

    public static async Task SeedFeaturesAsync(this WebApplication app)
    {
        await app.SeedIdentityAsync();
    }
}
