using Api.Data;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Budgets;

public static partial class Budgets
{
    public static partial class FetchForMonth
    {
        public record class Query(int Year, int Month);

        /// <param name="Amount">The budgeted monthly limit.</param>
        /// <param name="Actual">
        /// Money actually spent in this category during the month. Transaction amounts are signed
        /// (negative is money out), so this is the negated net: refunds reduce it, and a category
        /// that took in more than it spent reports a negative actual.
        /// </param>
        public record class Response(int Id, int CategoryId, int Year, int Month, decimal Amount)
        {
            public decimal Actual { get; init; }
        }

        [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
        public partial class Mapper
        {
            [MapperIgnoreTarget(nameof(Response.Actual))]
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
                if (budgets.Count == 0)
                {
                    return [];
                }

                var actualByCategory = await ActualSpendByCategoryAsync(
                    budgets.Select(b => b.CategoryId).ToList(), query.Year, query.Month, cancellationToken);

                return budgets
                    .Select(b => _mapper.Map(b) with { Actual = actualByCategory.GetValueOrDefault(b.CategoryId) })
                    .ToList();
            }

            private async Task<Dictionary<int, decimal>> ActualSpendByCategoryAsync(
                List<int> categoryIds, int year, int month, CancellationToken cancellationToken)
            {
                var monthStart = new DateOnly(year, month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // Summed in memory rather than with a SQL GROUP BY: SQLite has no native decimal
                // type, so aggregating money columns in the database loses precision.
                var amounts = await _db.Transactions
                    .Where(t => categoryIds.Contains(t.CategoryId) && t.Date >= monthStart && t.Date <= monthEnd)
                    .Select(t => new { t.CategoryId, t.Amount })
                    .ToListAsync(cancellationToken);

                return amounts
                    .GroupBy(t => t.CategoryId)
                    .ToDictionary(g => g.Key, g => -g.Sum(t => t.Amount));
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
