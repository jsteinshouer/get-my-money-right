using System.Security.Claims;
using Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Transactions.Transactions;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// Writes an assignment rule. Like an ignore rule it is created active and there is no update:
    /// a wrong rule is deleted and written again. It changes nothing already stored — a rule speaks
    /// about rows arriving, never about transactions in the ledger (ADR-0002).
    /// </summary>
    public static partial class CreateAssignmentRule
    {
        /// <summary>A null <paramref name="AccountId"/> means every account.</summary>
        public record class Command(
            int? AccountId,
            string MatchText,
            IgnoreMatchType MatchType,
            int? CategoryId,
            NeedWant? NeedWant,
            IReadOnlyList<int>? TagIds);

        public record class Tag(int Id, string Name);

        public record class Response(
            int Id,
            int? AccountId,
            string? AccountName,
            string MatchText,
            IgnoreMatchType MatchType,
            int? CategoryId,
            string? CategoryName,
            NeedWant? NeedWant,
            IReadOnlyList<Tag> Tags,
            bool IsActive);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.MatchText)
                    .NotEmpty()
                    .WithMessage("Type the words the rule should look for.")
                    .MaximumLength(200);

                RuleFor(x => x.MatchType).IsInEnum();

                RuleFor(x => x.NeedWant).IsInEnum();

                // A rule that assigns nothing cannot be read back as a sentence, and would only sit
                // on the rules page doing nothing.
                RuleFor(x => x)
                    .Must(x => x.CategoryId is not null || x.NeedWant is not null || x.TagIds is { Count: > 0 })
                    .OverridePropertyName("Assignments")
                    .WithMessage("Choose a category, Need or Want, or a tag — a rule has to set something.");
            }
        }

        public class Handler
        {
            private readonly BudgetDbContext _db;

            public Handler(BudgetDbContext db)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
            }

            /// <returns>Null when the account, category or a tag it names does not exist.</returns>
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

                string? categoryName = null;
                if (command.CategoryId is int categoryId)
                {
                    categoryName = await _db.Categories
                        .Where(c => c.Id == categoryId)
                        .Select(c => c.Name)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (categoryName is null)
                    {
                        return null;
                    }
                }

                var tagIds = (command.TagIds ?? []).Distinct().ToList();
                var tags = await _db.Tags
                    .Where(t => tagIds.Contains(t.Id))
                    .OrderBy(t => t.Name)
                    .Select(t => new Tag(t.Id, t.Name))
                    .ToListAsync(cancellationToken);
                if (tags.Count != tagIds.Count)
                {
                    return null;
                }

                var rule = new ImportAssignmentRule
                {
                    AccountId = command.AccountId,
                    // Normalised the same way the description it will be matched against is, so a
                    // rule typed with a stray double space still catches the row it was written from.
                    MatchText = Csv.NormaliseDescription(command.MatchText)!,
                    MatchType = command.MatchType,
                    CategoryId = command.CategoryId,
                    NeedWant = command.NeedWant,
                    IsActive = true,
                    CreatedByUserId = createdByUserId,
                };

                // The join rows need the rule's id, so the rule is saved first; one transaction
                // keeps a failure between the two writes from leaving a rule without its tags.
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
                _db.ImportAssignmentRules.Add(rule);
                await _db.SaveChangesAsync(cancellationToken);

                _db.ImportAssignmentRuleTags.AddRange(
                    tagIds.Select(tagId => new ImportAssignmentRuleTag { RuleId = rule.Id, TagId = tagId }));
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new Response(
                    rule.Id,
                    rule.AccountId,
                    accountName,
                    rule.MatchText,
                    rule.MatchType,
                    rule.CategoryId,
                    categoryName,
                    rule.NeedWant,
                    tags,
                    rule.IsActive);
            }
        }
    }

    public static IServiceCollection AddCreateAssignmentRule(this IServiceCollection services) => services
        .AddScoped<CreateAssignmentRule.Handler>();

    public static IEndpointRouteBuilder MapCreateAssignmentRule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/assignment-rules", async (
            CreateAssignmentRule.Command command,
            ClaimsPrincipal user,
            CreateAssignmentRule.Handler handler,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await handler.HandleAsync(command, userId, ct);
            return result is not null
                ? Results.Created($"/api/import/assignment-rules/{result.Id}", result)
                : Results.NotFound();
        });
        return endpoints;
    }
}
