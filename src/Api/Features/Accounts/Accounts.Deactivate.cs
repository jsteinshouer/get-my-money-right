using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Accounts;

public static partial class Accounts
{
    public static class Deactivate
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
                var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
                if (account is null)
                {
                    return false;
                }

                account.IsActive = false;
                await _db.SaveChangesAsync(cancellationToken);
                return true;
            }
        }
    }

    public static IServiceCollection AddDeactivate(this IServiceCollection services) => services
        .AddScoped<Deactivate.Handler>();

    public static IEndpointRouteBuilder MapDeactivate(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{id:int}/deactivate", async (int id, Deactivate.Handler handler, CancellationToken ct) =>
        {
            var found = await handler.HandleAsync(id, ct);
            return found ? Results.NoContent() : Results.NotFound();
        });
        return endpoints;
    }
}
