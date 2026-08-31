using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Tags;

public static partial class Tags
{
    public static class AssignToTransaction
    {
        /// <summary>Why an assignment could not be made, or <see cref="Result.Assigned"/> when it was.</summary>
        public enum Result
        {
            Assigned,
            TransactionNotFound,
            TagNotFound,
        }

        public class Handler
        {
            private readonly BudgetDbContext _db;

            public Handler(BudgetDbContext db)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
            }

            public async Task<Result> HandleAsync(int transactionId, int tagId, CancellationToken cancellationToken)
            {
                if (!await _db.Transactions.AnyAsync(t => t.Id == transactionId, cancellationToken))
                {
                    return Result.TransactionNotFound;
                }

                if (!await _db.Tags.AnyAsync(t => t.Id == tagId, cancellationToken))
                {
                    return Result.TagNotFound;
                }

                var alreadyAssigned = await _db.TransactionTags
                    .AnyAsync(tt => tt.TransactionId == transactionId && tt.TagId == tagId, cancellationToken);
                if (alreadyAssigned)
                {
                    // Assigning is idempotent: the caller asked for a state, not for an insert.
                    return Result.Assigned;
                }

                _db.TransactionTags.Add(new TransactionTag { TransactionId = transactionId, TagId = tagId });
                await _db.SaveChangesAsync(cancellationToken);
                return Result.Assigned;
            }
        }
    }

    public static IServiceCollection AddAssignToTransaction(this IServiceCollection services) => services
        .AddScoped<AssignToTransaction.Handler>();

    public static IEndpointRouteBuilder MapAssignToTransaction(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/{tagId:int}", async (int transactionId, int tagId, AssignToTransaction.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(transactionId, tagId, ct);
            return result is AssignToTransaction.Result.Assigned ? Results.NoContent() : Results.NotFound();
        });
        return endpoints;
    }
}
