using Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Budgets;

public static partial class Budgets
{
    public static partial class Update
    {
        public record class Command(int CategoryId, int Year, int Month, decimal Amount);

        public record class Response(int Id, int CategoryId, int Year, int Month, decimal Amount);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.CategoryId).GreaterThan(0);
                RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
                RuleFor(x => x.Month).InclusiveBetween(1, 12);
                RuleFor(x => x.Amount).GreaterThan(0);
            }
        }

        [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
        public partial class Mapper
        {
            public partial Response Map(Budget budget);
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
                var budget = await _db.Budgets.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
                if (budget is null)
                {
                    return null;
                }

                budget.CategoryId = command.CategoryId;
                budget.Year = command.Year;
                budget.Month = command.Month;
                budget.Amount = command.Amount;
                await _db.SaveChangesAsync(cancellationToken);
                return _mapper.Map(budget);
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
