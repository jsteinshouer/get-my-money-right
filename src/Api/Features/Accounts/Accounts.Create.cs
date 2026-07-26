using System.Security.Claims;
using Api.Data;
using FluentValidation;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Accounts;

public static partial class Accounts
{
    public static partial class Create
    {
        public record class Command(string Name, AccountType Type);

        public record class Response(int Id, string Name, AccountType Type, bool IsActive);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
                RuleFor(x => x.Type).IsInEnum();
            }
        }

        [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
        public partial class Mapper
        {
            public partial Response Map(Account account);
        }

        public class Handler
        {
            private readonly BudgetDbContext _db;
            private readonly Mapper _mapper;

            public Handler(BudgetDbContext db, Mapper mapper)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
                _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            }

            public async Task<Response> HandleAsync(Command command, string createdByUserId, CancellationToken cancellationToken)
            {
                var account = new Account
                {
                    Name = command.Name,
                    Type = command.Type,
                    CreatedByUserId = createdByUserId,
                };
                _db.Accounts.Add(account);
                await _db.SaveChangesAsync(cancellationToken);
                return _mapper.Map(account);
            }
        }
    }

    public static IServiceCollection AddCreate(this IServiceCollection services) => services
        .AddScoped<Create.Handler>()
        .AddSingleton<Create.Mapper>();

    public static IEndpointRouteBuilder MapCreate(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", async (Create.Command command, ClaimsPrincipal user, Create.Handler handler, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await handler.HandleAsync(command, userId, ct);
            return Results.Created($"/api/accounts/{result.Id}", result);
        });
        return endpoints;
    }
}
