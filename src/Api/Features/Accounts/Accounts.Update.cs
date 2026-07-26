using Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Accounts;

public static partial class Accounts
{
    public static partial class Update
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

            public async Task<Response?> HandleAsync(int id, Command command, CancellationToken cancellationToken)
            {
                var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
                if (account is null)
                {
                    return null;
                }

                account.Name = command.Name;
                account.Type = command.Type;
                await _db.SaveChangesAsync(cancellationToken);
                return _mapper.Map(account);
            }
        }
    }

    public static IServiceCollection AddUpdate(this IServiceCollection services) => services
        .AddScoped<Update.Handler>()
        .AddSingleton<Update.Mapper>();

    public static IEndpointRouteBuilder MapUpdate(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/{id:int}", async (int id, Update.Command command, Update.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, command, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });
        return endpoints;
    }
}
