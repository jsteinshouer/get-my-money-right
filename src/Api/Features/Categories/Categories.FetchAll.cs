using Api.Data;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Categories;

public static partial class Categories
{
    public static partial class FetchAll
    {
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

            public async Task<List<Response>> HandleAsync(CancellationToken cancellationToken)
            {
                var categories = await _db.Categories
                    .OrderBy(c => c.Name)
                    .ToListAsync(cancellationToken);
                return categories.Select(_mapper.Map).ToList();
            }
        }
    }

    public static IServiceCollection AddFetchAll(this IServiceCollection services) => services
        .AddScoped<FetchAll.Handler>()
        .AddSingleton<FetchAll.Mapper>();

    public static IEndpointRouteBuilder MapFetchAll(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", (FetchAll.Handler handler, CancellationToken ct) =>
            handler.HandleAsync(ct));
        return endpoints;
    }
}
