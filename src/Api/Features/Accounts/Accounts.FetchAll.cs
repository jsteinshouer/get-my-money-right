using Api.Data;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Accounts;

public static partial class Accounts
{
    public static partial class FetchAll
    {
        public record class Query(bool IncludeInactive = false);

        public record class Response(int Id, string Name, AccountType Type, bool IsActive);

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

            public async Task<List<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
            {
                var accounts = await _db.Accounts
                    .Where(a => query.IncludeInactive || a.IsActive)
                    .OrderBy(a => a.Name)
                    .ToListAsync(cancellationToken);
                return accounts.Select(_mapper.Map).ToList();
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
