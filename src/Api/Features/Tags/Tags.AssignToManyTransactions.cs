using Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Tags;

public static partial class Tags
{
    public static class AssignToManyTransactions
    {
        public record class Command(IReadOnlyList<int> TransactionIds);

        /// <summary>
        /// Counted rather than silent: the household is told what the batch actually changed, and
        /// what it left alone because the tag was already there.
        /// </summary>
        public record class Response(int AssignedCount, int AlreadyTaggedCount);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.TransactionIds).NotEmpty();
            }
        }

        public class Handler
        {
            private readonly BudgetDbContext _db;

            public Handler(BudgetDbContext db)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
            }

            public async Task<Response?> HandleAsync(int tagId, Command command, CancellationToken cancellationToken)
            {
                if (!await _db.Tags.AnyAsync(t => t.Id == tagId, cancellationToken))
                {
                    return null;
                }

                var requestedIds = command.TransactionIds.Distinct().ToList();

                // A selection can outlive the rows it named — another session may have deleted one —
                // so the batch tags what still exists rather than failing whole.
                var existingIds = await _db.Transactions
                    .Where(t => requestedIds.Contains(t.Id))
                    .Select(t => t.Id)
                    .ToListAsync(cancellationToken);

                var alreadyTaggedIds = await _db.TransactionTags
                    .Where(tt => tt.TagId == tagId && existingIds.Contains(tt.TransactionId))
                    .Select(tt => tt.TransactionId)
                    .ToListAsync(cancellationToken);

                var toAssign = existingIds.Except(alreadyTaggedIds).ToList();
                foreach (var transactionId in toAssign)
                {
                    _db.TransactionTags.Add(new TransactionTag { TransactionId = transactionId, TagId = tagId });
                }

                await _db.SaveChangesAsync(cancellationToken);
                return new Response(toAssign.Count, alreadyTaggedIds.Count);
            }
        }
    }

    public static IServiceCollection AddAssignToManyTransactions(this IServiceCollection services) => services
        .AddScoped<AssignToManyTransactions.Handler>();

    public static IEndpointRouteBuilder MapAssignToManyTransactions(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{tagId:int}/transactions", async (
            int tagId, AssignToManyTransactions.Command command, AssignToManyTransactions.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(tagId, command, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });
        return endpoints;
    }
}
