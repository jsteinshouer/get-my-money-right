using System.Security.Claims;
using Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// Writes a rule, normally straight from the preview row that provoked it. A rule is created
    /// active and there is no update: in this ticket, deleting a rule is how it is switched off.
    /// </summary>
    public static partial class CreateIgnoreRule
    {
        /// <summary>A null <paramref name="AccountId"/> means every account.</summary>
        public record class Command(int? AccountId, string MatchText, IgnoreMatchType MatchType);

        public record class Response(
            int Id, int? AccountId, string? AccountName, string MatchText, IgnoreMatchType MatchType, bool IsActive);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.MatchText)
                    .NotEmpty()
                    .WithMessage("Type the words the rule should look for.")
                    .MaximumLength(200);

                RuleFor(x => x.MatchType).IsInEnum();
            }
        }

        public class Handler
        {
            private readonly BudgetDbContext _db;

            public Handler(BudgetDbContext db)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
            }

            public async Task<Response?> HandleAsync(
                Command command, string createdByUserId, CancellationToken cancellationToken)
            {
                string? accountName = null;
                if (command.AccountId is int accountId)
                {
                    accountName = await _db.Accounts
                        .Where(a => a.Id == accountId)
                        .Select(a => a.Name)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (accountName is null)
                    {
                        return null;
                    }
                }

                var rule = new ImportIgnoreRule
                {
                    AccountId = command.AccountId,
                    // Normalised the same way the description it will be matched against is, so a
                    // rule typed with a stray double space still catches the row it was written from.
                    MatchText = Csv.NormaliseDescription(command.MatchText)!,
                    MatchType = command.MatchType,
                    IsActive = true,
                    CreatedByUserId = createdByUserId,
                };

                _db.ImportIgnoreRules.Add(rule);
                await _db.SaveChangesAsync(cancellationToken);

                return new Response(
                    rule.Id, rule.AccountId, accountName, rule.MatchText, rule.MatchType, rule.IsActive);
            }
        }
    }

    public static IServiceCollection AddCreateIgnoreRule(this IServiceCollection services) => services
        .AddScoped<CreateIgnoreRule.Handler>();

    public static IEndpointRouteBuilder MapCreateIgnoreRule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/ignore-rules", async (
            CreateIgnoreRule.Command command,
            ClaimsPrincipal user,
            CreateIgnoreRule.Handler handler,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await handler.HandleAsync(command, userId, ct);
            return result is not null
                ? Results.Created($"/api/import/ignore-rules/{result.Id}", result)
                : Results.NotFound();
        });
        return endpoints;
    }
}
