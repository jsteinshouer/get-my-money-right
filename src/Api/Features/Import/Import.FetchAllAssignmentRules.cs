using Api.Data;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Transactions.Transactions;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// Every assignment rule, in the order the ignore rules are read: what applies everywhere first,
    /// then each account's own. Each carries the names of what it assigns, so it can be read back as
    /// a sentence without a second trip for categories and tags.
    /// </summary>
    public static class FetchAllAssignmentRules
    {
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

        public class Handler
        {
            private readonly BudgetDbContext _db;

            public Handler(BudgetDbContext db)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
            }

            public async Task<List<Response>> HandleAsync(CancellationToken cancellationToken)
            {
                var rules = await (
                    from rule in _db.ImportAssignmentRules.AsNoTracking()
                    join account in _db.Accounts on rule.AccountId equals account.Id into scope
                    from account in scope.DefaultIfEmpty()
                    join category in _db.Categories on rule.CategoryId equals category.Id into assigned
                    from category in assigned.DefaultIfEmpty()
                    orderby rule.AccountId == null ? 0 : 1, account.Name, rule.MatchText
                    select new
                    {
                        Rule = rule,
                        AccountName = account == null ? null : account.Name,
                        CategoryName = category == null ? null : category.Name,
                    })
                    .ToListAsync(cancellationToken);

                var tagsByRule = (await (
                        from ruleTag in _db.ImportAssignmentRuleTags.AsNoTracking()
                        join tag in _db.Tags on ruleTag.TagId equals tag.Id
                        orderby tag.Name
                        select new { ruleTag.RuleId, Tag = new Tag(tag.Id, tag.Name) })
                        .ToListAsync(cancellationToken))
                    .ToLookup(x => x.RuleId, x => x.Tag);

                return rules
                    .Select(r => new Response(
                        r.Rule.Id,
                        r.Rule.AccountId,
                        r.AccountName,
                        r.Rule.MatchText,
                        r.Rule.MatchType,
                        r.Rule.CategoryId,
                        r.CategoryName,
                        r.Rule.NeedWant,
                        tagsByRule[r.Rule.Id].ToList(),
                        r.Rule.IsActive))
                    .ToList();
            }
        }
    }

    public static IServiceCollection AddFetchAllAssignmentRules(this IServiceCollection services) => services
        .AddScoped<FetchAllAssignmentRules.Handler>();

    public static IEndpointRouteBuilder MapFetchAllAssignmentRules(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/assignment-rules", (FetchAllAssignmentRules.Handler handler, CancellationToken ct) =>
            handler.HandleAsync(ct));
        return endpoints;
    }
}
