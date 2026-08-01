using System.Security.Claims;
using Api.Data;
using FluentValidation;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Transactions;

public static partial class Transactions
{
    public static partial class Create
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

            public async Task<Response> HandleAsync(Command command, string createdByUserId, CancellationToken cancellationToken)
            {
                var transaction = new Transaction
                {
                    AccountId = command.AccountId,
                    CategoryId = command.CategoryId,
                    Date = command.Date,
                    Amount = command.Amount,
                    Description = command.Description,
                    NeedWant = command.NeedWant!.Value,
                    CreatedByUserId = createdByUserId,
                };
                _db.Transactions.Add(transaction);
                await _db.SaveChangesAsync(cancellationToken);
                return _mapper.Map(transaction);
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
            return Results.Created($"/api/transactions/{result.Id}", result);
        });
        return endpoints;
    }
}
