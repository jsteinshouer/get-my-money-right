using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Tags;

public static partial class Tags
{
    public static class RemoveFromTransaction
    {
        public class Handler
        {
            private readonly BudgetDbContext _db;

            public Handler(BudgetDbContext db)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
            }

            public async Task<bool> HandleAsync(int transactionId, int tagId, CancellationToken cancellationToken)
            {
                var assignment = await _db.TransactionTags
                    .FirstOrDefaultAsync(tt => tt.TransactionId == transactionId && tt.TagId == tagId, cancellationToken);
                if (assignment is null)
                {
                    return false;
                }

                _db.TransactionTags.Remove(assignment);
                await _db.SaveChangesAsync(cancellationToken);
                return true;
            }
        }
    }

    public static IServiceCollection AddRemoveFromTransaction(this IServiceCollection services) => services
        .AddScoped<RemoveFromTransaction.Handler>();

    public static IEndpointRouteBuilder MapRemoveFromTransaction(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/{tagId:int}", async (int transactionId, int tagId, RemoveFromTransaction.Handler handler, CancellationToken ct) =>
        {
            var removed = await handler.HandleAsync(transactionId, tagId, ct);
            return removed ? Results.NoContent() : Results.NotFound();
        });
        return endpoints;
    }
}
