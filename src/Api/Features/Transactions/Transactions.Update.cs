using Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Transactions;

public static partial class Transactions
{
    public static partial class Update
    {
        public record class Command(int AccountId, int CategoryId, DateOnly Date, decimal Amount, string Description, NeedWant? NeedWant);

        public record class Response(int Id, int AccountId, int CategoryId, DateOnly Date, decimal Amount, string Description, NeedWant NeedWant);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.AccountId).GreaterThan(0);
                RuleFor(x => x.CategoryId).GreaterThan(0);
                RuleFor(x => x.Date).NotEqual(default(DateOnly));
                RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
                RuleFor(x => x.NeedWant).NotNull().IsInEnum();
            }
        }

        [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
        public partial class Mapper
        {
            public partial Response Map(Transaction transaction);
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
                var transaction = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
                if (transaction is null)
                {
                    return null;
                }

                transaction.AccountId = command.AccountId;
                transaction.CategoryId = command.CategoryId;
                transaction.Date = command.Date;
                transaction.Amount = command.Amount;
                transaction.Description = command.Description;
                transaction.NeedWant = command.NeedWant!.Value;
                await _db.SaveChangesAsync(cancellationToken);
                return _mapper.Map(transaction);
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
