using Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Api.Features.Identity;

public static partial class Identity
{
    public record class SeedUser(string UserName, string Email, string Password, string DisplayName);

    public static IServiceCollection AddIdentityFeature(this IServiceCollection services) => services
        .AddLogin()
        .AddLogout()
        .AddMe();

    public static IEndpointRouteBuilder MapIdentityFeature(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("identity").WithTags("Identity").AddFluentValidationAutoValidation();
        group.MapLogin().MapLogout().MapMe();
        return endpoints;
    }

    public static async Task SeedIdentityAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await SeedUsersAsync(
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            scope.ServiceProvider.GetRequiredService<IOptions<List<SeedUser>>>().Value);
    }

    /// <summary>Creates every configured household user that doesn't exist yet.</summary>
    public static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, IEnumerable<SeedUser> seedUsers)
    {
        foreach (var seedUser in seedUsers)
        {
            var existingUser = await userManager.FindByEmailAsync(seedUser.Email);
            if (existingUser is not null) { continue; }

            var user = new ApplicationUser
            {
                UserName = seedUser.UserName,
                Email = seedUser.Email,
                EmailConfirmed = true,
                DisplayName = seedUser.DisplayName,
            };

            var result = await userManager.CreateAsync(user, seedUser.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed household user '{seedUser.Email}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
