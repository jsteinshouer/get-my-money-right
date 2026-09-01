using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Tags;

public static partial class Tags
{
    public static class FetchAll
    {
        /// <summary>
        /// <paramref name="TransactionCount"/> is what makes a delete confirmable and ranks the
        /// most-used tags in the tag line, so it is part of the tag rather than a second request.
        /// </summary>
        public record class Response(int Id, string Name, int TransactionCount);

        public class Handler
        {
            private readonly BudgetDbContext _db;

            public Handler(BudgetDbContext db)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
            }

            public async Task<List<Response>> HandleAsync(CancellationToken cancellationToken) =>
                await _db.Tags
                    .OrderBy(t => t.Name)
                    .Select(t => new Response(
                        t.Id,
                        t.Name,
                        _db.TransactionTags.Count(tt => tt.TagId == t.Id)))
                    .ToListAsync(cancellationToken);
        }
    }

    public static IServiceCollection AddFetchAll(this IServiceCollection services) => services
        .AddScoped<FetchAll.Handler>();

    public static IEndpointRouteBuilder MapFetchAll(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", (FetchAll.Handler handler, CancellationToken ct) =>
            handler.HandleAsync(ct));
        return endpoints;
    }
}
