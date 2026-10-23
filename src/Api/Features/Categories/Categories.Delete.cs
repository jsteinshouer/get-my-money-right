using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Categories;

public static partial class Categories
{
    public static class Delete
    {
        public enum Outcome
        {
            Deleted,
            NotFound,
            UsedByRules,
        }

        /// <param name="RuleCount">How many assignment rules stand in the way, when any do.</param>
        public record class Result(Outcome Outcome, int RuleCount = 0);

        public class Handler
        {
            private readonly BudgetDbContext _db;

            public Handler(BudgetDbContext db)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
            }

            public async Task<Result> HandleAsync(int id, CancellationToken cancellationToken)
            {
                var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (category is null)
                {
                    return new Result(Outcome.NotFound);
                }

                // The foreign key would refuse this too, but only the count tells the household what
                // to fix first. Transactions in the category are still refused by the key alone.
                var ruleCount = await _db.ImportAssignmentRules.CountAsync(r => r.CategoryId == id, cancellationToken);
                if (ruleCount > 0)
                {
                    return new Result(Outcome.UsedByRules, ruleCount);
                }

                _db.Categories.Remove(category);
                await _db.SaveChangesAsync(cancellationToken);
                return new Result(Outcome.Deleted);
            }
        }
    }

    public static IServiceCollection AddDelete(this IServiceCollection services) => services
        .AddScoped<Delete.Handler>();

    public static IEndpointRouteBuilder MapDelete(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/{id:int}", async (int id, Delete.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.Outcome switch
            {
                Delete.Outcome.NotFound => Results.NotFound(),
                Delete.Outcome.UsedByRules => Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "The category is used by assignment rules.",
                    detail: result.RuleCount == 1
                        ? "1 assignment rule uses this category."
                        : $"{result.RuleCount} assignment rules use this category.",
                    extensions: new Dictionary<string, object?> { ["ruleCount"] = result.RuleCount }),
                _ => Results.NoContent(),
            };
        });
        return endpoints;
    }
}
