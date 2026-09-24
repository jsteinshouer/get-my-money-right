using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// Deleting a rule is the off switch. Nothing depends on a rule once it is gone — it only ever
    /// decided what a preview struck — so the delete is unconditional and takes effect on the next
    /// read of the preview.
    /// </summary>
    public static class DeleteIgnoreRule
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
                var rule = await _db.ImportIgnoreRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
                if (rule is null)
                {
                    return false;
                }

                _db.ImportIgnoreRules.Remove(rule);
                await _db.SaveChangesAsync(cancellationToken);
                return true;
            }
        }
    }

    public static IServiceCollection AddDeleteIgnoreRule(this IServiceCollection services) => services
        .AddScoped<DeleteIgnoreRule.Handler>();

    public static IEndpointRouteBuilder MapDeleteIgnoreRule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/ignore-rules/{id:int}", async (
            int id, DeleteIgnoreRule.Handler handler, CancellationToken ct) =>
        {
            var found = await handler.HandleAsync(id, ct);
            return found ? Results.NoContent() : Results.NotFound();
        });
        return endpoints;
    }
}
