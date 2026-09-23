using Api.Data;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Import;

public static partial class Import
{
    public static partial class FetchMapping
    {
        public record class Query(int AccountId);

        public record class Response(
            int Id,
            int AccountId,
            string DateColumn,
            string DescriptionColumn,
            string? AmountColumn,
            string? DebitColumn,
            string? CreditColumn,
            string DateFormat,
            string Delimiter,
            bool HasHeaderRow,
            DateTimeOffset UpdatedAt);

        [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
        public partial class Mapper
        {
            public partial Response Map(CsvImportMapping mapping);
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

            public async Task<Response?> HandleAsync(Query query, CancellationToken cancellationToken)
            {
                var mapping = await _db.CsvImportMappings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.AccountId == query.AccountId, cancellationToken);
                return mapping is not null ? _mapper.Map(mapping) : null;
            }
        }
    }

    public static IServiceCollection AddFetchMapping(this IServiceCollection services) => services
        .AddScoped<FetchMapping.Handler>()
        .AddSingleton<FetchMapping.Mapper>();

    public static IEndpointRouteBuilder MapFetchMapping(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/mappings/{accountId:int}", async (
            [AsParameters] FetchMapping.Query query, FetchMapping.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(query, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });
        return endpoints;
    }
}
