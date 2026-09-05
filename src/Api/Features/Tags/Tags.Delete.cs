using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Tags;

public static partial class Tags
{
    public static class Delete
    {
        public class Handler
        {
            private readonly BudgetDbContext _db;

            public Handler(BudgetDbContext db)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
            }

            public async Task<bool> HandleAsync(int id, CancellationToken cancellationToken)
            {
                var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
                if (tag is null)
                {
                    return false;
                }

                // A tag is a label, not a classification the ledger depends on, so deleting one
                // detaches it from its transactions rather than being blocked by them.
                _db.Tags.Remove(tag);
                await _db.SaveChangesAsync(cancellationToken);
                return true;
            }
        }
    }

    public static IServiceCollection AddDelete(this IServiceCollection services) => services
        .AddScoped<Delete.Handler>();

    public static IEndpointRouteBuilder MapDelete(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/{id:int}", async (int id, Delete.Handler handler, CancellationToken ct) =>
        {
            var found = await handler.HandleAsync(id, ct);
            return found ? Results.NoContent() : Results.NotFound();
        });
        return endpoints;
    }
}
