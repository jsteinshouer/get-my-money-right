using System.Security.Claims;
using Api.Data;
using FluentValidation;
using Riok.Mapperly.Abstractions;

namespace Api.Features.Tags;

public static partial class Tags
{
    public static partial class Create
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
            public partial Response Map(Tag tag);
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

            public async Task<Response> HandleAsync(Command command, string createdByUserId, CancellationToken cancellationToken)
            {
                var tag = new Tag
                {
                    Name = command.Name,
                    CreatedByUserId = createdByUserId,
                };
                _db.Tags.Add(tag);
                await _db.SaveChangesAsync(cancellationToken);
                return _mapper.Map(tag);
            }
        }
    }

    public static IServiceCollection AddCreate(this IServiceCollection services) => services
        .AddScoped<Create.Handler>()
        .AddSingleton<Create.Mapper>();

    public static IEndpointRouteBuilder MapCreate(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", async (Create.Command command, ClaimsPrincipal user, Create.Handler handler, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await handler.HandleAsync(command, userId, ct);
            return Results.Created($"/api/tags/{result.Id}", result);
        });
        return endpoints;
    }
}
