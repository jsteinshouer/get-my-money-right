using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Categories;

public static partial class Categories
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
                var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (category is null)
                {
                    return false;
                }

                _db.Categories.Remove(category);
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
