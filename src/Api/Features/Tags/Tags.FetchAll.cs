using Api.Data;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Tags;

public static partial class Tags
{
    public static partial class FetchAll
    {
        public record class Response(int Id, string Name);

        [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
        public partial class Mapper
        {
            public partial Response Map(Tag tag);
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
                var tags = await _db.Tags
                    .OrderBy(t => t.Name)
                    .ToListAsync(cancellationToken);
                return tags.Select(_mapper.Map).ToList();
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
