using Api.Data;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Budgets;

public static partial class Budgets
{
    public static partial class FetchForMonth
    {
        public record class Query(int Year, int Month);

        public record class Response(int Id, int CategoryId, int Year, int Month, decimal Amount);

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

            public async Task<List<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
            {
                var budgets = await _db.Budgets
                    .Where(b => b.Year == query.Year && b.Month == query.Month)
                    .OrderBy(b => b.CategoryId)
                    .ToListAsync(cancellationToken);
                return budgets.Select(_mapper.Map).ToList();
            }
        }
    }

    public static IServiceCollection AddFetchForMonth(this IServiceCollection services) => services
        .AddScoped<FetchForMonth.Handler>()
        .AddSingleton<FetchForMonth.Mapper>();

    public static IEndpointRouteBuilder MapFetchForMonth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", ([AsParameters] FetchForMonth.Query query, FetchForMonth.Handler handler, CancellationToken ct) =>
            handler.HandleAsync(query, ct));
        return endpoints;
    }
}
