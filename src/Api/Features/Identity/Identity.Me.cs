using System.Security.Claims;
using Api.Data;
using Microsoft.AspNetCore.Identity;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Identity;

public static partial class Identity
{
    public static partial class Me
    {
        public record class Response(string Id, string Email, string DisplayName);

        [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
        public partial class Mapper
        {
            public partial Response Map(ApplicationUser user);
        }

        public class Handler
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly Mapper _mapper;

            public Handler(UserManager<ApplicationUser> userManager, Mapper mapper)
            {
                _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
                _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            }

            public async Task<Response> HandleAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
            {
                var user = await _userManager.GetUserAsync(principal)
                    ?? throw new InvalidOperationException("Authenticated principal has no matching user record.");
                return _mapper.Map(user);
            }
        }
    }

    public static IServiceCollection AddMe(this IServiceCollection services) => services
        .AddScoped<Me.Handler>()
        .AddSingleton<Me.Mapper>();

    public static IEndpointRouteBuilder MapMe(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/me", (ClaimsPrincipal user, Me.Handler handler, CancellationToken ct) =>
            handler.HandleAsync(user, ct))
            .RequireAuthorization();
        return endpoints;
    }
}
