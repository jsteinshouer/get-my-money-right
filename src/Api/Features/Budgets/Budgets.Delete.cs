using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Budgets;

public static partial class Budgets
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
                var budget = await _db.Budgets.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
                if (budget is null)
                {
                    return false;
                }

                _db.Budgets.Remove(budget);
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
