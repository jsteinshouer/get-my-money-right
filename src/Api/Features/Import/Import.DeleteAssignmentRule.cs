using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// Deleting a rule is the off switch, and with no edit it is also how a rule is changed. Nothing
    /// depends on a rule — it only ever decides how incoming rows read — so the delete is
    /// unconditional; its tags go with it, and the transactions it once classified stay as they are.
    /// </summary>
    public static class DeleteAssignmentRule
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
                var rule = await _db.ImportAssignmentRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
                if (rule is null)
                {
                    return false;
                }

                _db.ImportAssignmentRules.Remove(rule);
                await _db.SaveChangesAsync(cancellationToken);
                return true;
            }
        }
    }

    public static IServiceCollection AddDeleteAssignmentRule(this IServiceCollection services) => services
        .AddScoped<DeleteAssignmentRule.Handler>();

    public static IEndpointRouteBuilder MapDeleteAssignmentRule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/assignment-rules/{id:int}", async (
            int id, DeleteAssignmentRule.Handler handler, CancellationToken ct) =>
        {
            var found = await handler.HandleAsync(id, ct);
            return found ? Results.NoContent() : Results.NotFound();
        });
        return endpoints;
    }
}
