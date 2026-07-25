using Api.Data;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Identity;

public static partial class Identity
{
    public static partial class Login
    {
        public record class Command(string Email, string Password);

        public record class Response(string Id, string Email, string DisplayName);

        public record class Result(bool Succeeded, Response? User);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Email).NotEmpty().EmailAddress();
                RuleFor(x => x.Password).NotEmpty();
            }
        }

        [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
        public partial class Mapper
        {
            public partial Response Map(ApplicationUser user);
        }

        public class Handler
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly SignInManager<ApplicationUser> _signInManager;
            private readonly Mapper _mapper;

            public Handler(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, Mapper mapper)
            {
                _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
                _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
                _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            }

            public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByEmailAsync(command.Email);
                if (user is null)
                {
                    return new Result(false, null);
                }

                var signIn = await _signInManager.PasswordSignInAsync(
                    user, command.Password, isPersistent: true, lockoutOnFailure: false);
                if (!signIn.Succeeded)
                {
                    return new Result(false, null);
                }

                return new Result(true, _mapper.Map(user));
            }
        }
    }

    public static IServiceCollection AddLogin(this IServiceCollection services) => services
        .AddScoped<Login.Handler>()
        .AddSingleton<Login.Mapper>();

    public static IEndpointRouteBuilder MapLogin(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/login", async (Login.Command command, Login.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(command, ct);
            return result.Succeeded ? Results.Ok(result.User) : Results.Unauthorized();
        });
        return endpoints;
    }
}
