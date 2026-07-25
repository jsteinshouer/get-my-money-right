using Api.Data;
using Microsoft.AspNetCore.Identity;

namespace Api.Features.Identity;

public static partial class Identity
{
    public static class Logout
    {
        public class Handler
        {
            private readonly SignInManager<ApplicationUser> _signInManager;

            public Handler(SignInManager<ApplicationUser> signInManager)
            {
                _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            }

            public Task HandleAsync(CancellationToken cancellationToken) => _signInManager.SignOutAsync();
        }
    }

    public static IServiceCollection AddLogout(this IServiceCollection services) => services
        .AddScoped<Logout.Handler>();

    public static IEndpointRouteBuilder MapLogout(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/logout", async (Logout.Handler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(ct);
            return Results.NoContent();
        });
        return endpoints;
    }
}
