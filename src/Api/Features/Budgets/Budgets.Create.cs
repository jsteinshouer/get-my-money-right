using Api.Data;
using FluentValidation;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Budgets;

public static partial class Budgets
{
    public static partial class Create
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

            public async Task<Response> HandleAsync(Command command, CancellationToken cancellationToken)
            {
                var budget = new Budget
                {
                    CategoryId = command.CategoryId,
                    Year = command.Year,
                    Month = command.Month,
                    Amount = command.Amount,
                };
                _db.Budgets.Add(budget);
                await _db.SaveChangesAsync(cancellationToken);
                return _mapper.Map(budget);
            }
        }
    }

    public static IServiceCollection AddCreate(this IServiceCollection services) => services
        .AddScoped<Create.Handler>()
        .AddSingleton<Create.Mapper>();

    public static IEndpointRouteBuilder MapCreate(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", async (Create.Command command, Create.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/budgets/{result.Id}", result);
        });
        return endpoints;
    }
}
