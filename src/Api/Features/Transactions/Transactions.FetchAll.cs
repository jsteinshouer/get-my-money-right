using Api.Data;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Transactions;

public static partial class Transactions
{
    public static partial class FetchAll
    {
        public record class Query(int? AccountId = null, int? CategoryId = null, DateOnly? DateFrom = null, DateOnly? DateTo = null, NeedWant? NeedWant = null, int? TagId = null);

        public record class Response(int Id, int AccountId, int CategoryId, DateOnly Date, decimal Amount, string Description, NeedWant NeedWant)
        {
            /// <summary>Filled in from the tag assignments after the transaction itself is mapped.</summary>
            public IReadOnlyList<int> TagIds { get; init; } = [];
        }

        [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
        public partial class Mapper
        {
            [MapperIgnoreTarget(nameof(Response.TagIds))]
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

            public async Task<List<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
            {
                var transactions = await _db.Transactions
                    .Where(t => query.AccountId == null || t.AccountId == query.AccountId)
                    .Where(t => query.CategoryId == null || t.CategoryId == query.CategoryId)
                    .Where(t => query.DateFrom == null || t.Date >= query.DateFrom)
                    .Where(t => query.DateTo == null || t.Date <= query.DateTo)
                    .Where(t => query.NeedWant == null || t.NeedWant == query.NeedWant)
                    .Where(t => query.TagId == null || _db.TransactionTags.Any(tt => tt.TransactionId == t.Id && tt.TagId == query.TagId))
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.Id)
                    .ToListAsync(cancellationToken);

                var transactionIds = transactions.Select(t => t.Id).ToList();
                var tagIdsByTransaction = (await _db.TransactionTags
                        .Where(tt => transactionIds.Contains(tt.TransactionId))
                        .ToListAsync(cancellationToken))
                    .GroupBy(tt => tt.TransactionId)
                    .ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Select(tt => tt.TagId).Order().ToList());

                return transactions
                    .Select(t => _mapper.Map(t) with
                    {
                        TagIds = tagIdsByTransaction.TryGetValue(t.Id, out var tagIds) ? tagIds : [],
                    })
                    .ToList();
            }
        }
    }

    public static IServiceCollection AddFetchAll(this IServiceCollection services) => services
        .AddScoped<FetchAll.Handler>()
        .AddSingleton<FetchAll.Mapper>();

    public static IEndpointRouteBuilder MapFetchAll(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", ([AsParameters] FetchAll.Query query, FetchAll.Handler handler, CancellationToken ct) =>
            handler.HandleAsync(query, ct));
        return endpoints;
    }
}
