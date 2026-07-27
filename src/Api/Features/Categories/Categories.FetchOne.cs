using Api.Data;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Categories;

public static partial class Categories
{
    public static partial class FetchOne
    {
        public record class Query(int Id);

        public record class Response(int Id, string Name);

        [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
        public partial class Mapper
        {
            public partial Response Map(Category category);
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
                var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken);
                return category is not null ? _mapper.Map(category) : null;
            }
        }
    }

    public static IServiceCollection AddFetchOne(this IServiceCollection services) => services
        .AddScoped<FetchOne.Handler>()
        .AddSingleton<FetchOne.Mapper>();

    public static IEndpointRouteBuilder MapFetchOne(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{id:int}", async ([AsParameters] FetchOne.Query query, FetchOne.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(query, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });
        return endpoints;
    }
}
