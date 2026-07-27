using Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Categories;

public static partial class Categories
{
    public static partial class Update
    {
        public record class Command(string Name);

        public record class Response(int Id, string Name);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            }
        }

        [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
        public partial class Mapper
        {
            public partial Response Map(Category category);
        }

        public class Handler
        {
            private readonly BudgetDbContext _db;
            private readonly Mapper _mapper;

            public Handler(BudgetDbContext db, Mapper mapper)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
                _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            }

            public async Task<Response?> HandleAsync(int id, Command command, CancellationToken cancellationToken)
            {
                var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (category is null)
                {
                    return null;
                }

                category.Name = command.Name;
                await _db.SaveChangesAsync(cancellationToken);
                return _mapper.Map(category);
            }
        }
    }

    public static IServiceCollection AddUpdate(this IServiceCollection services) => services
        .AddScoped<Update.Handler>()
        .AddSingleton<Update.Mapper>();

    public static IEndpointRouteBuilder MapUpdate(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/{id:int}", async (int id, Update.Command command, Update.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, command, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });
        return endpoints;
    }
}
