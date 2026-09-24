using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// The whole ledger of rules, in the order it is read: what applies everywhere first, then each
    /// account's own. There is no paging — a household that needs one has written far too many rules.
    /// </summary>
    public static class FetchAllIgnoreRules
    {
        public record class Response(
            int Id, int? AccountId, string? AccountName, string MatchText, IgnoreMatchType MatchType, bool IsActive);

        public class Handler
        {
            private readonly BudgetDbContext _db;

            public Handler(BudgetDbContext db)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
            }

            public async Task<List<Response>> HandleAsync(CancellationToken cancellationToken) =>
                await (
                    from rule in _db.ImportIgnoreRules.AsNoTracking()
                    join account in _db.Accounts on rule.AccountId equals account.Id into scope
                    from account in scope.DefaultIfEmpty()
                    orderby rule.AccountId == null ? 0 : 1, account.Name, rule.MatchText
                    select new Response(
                        rule.Id,
                        rule.AccountId,
                        account == null ? null : account.Name,
                        rule.MatchText,
                        rule.MatchType,
                        rule.IsActive))
                    .ToListAsync(cancellationToken);
        }
    }

    public static IServiceCollection AddFetchAllIgnoreRules(this IServiceCollection services) => services
        .AddScoped<FetchAllIgnoreRules.Handler>();

    public static IEndpointRouteBuilder MapFetchAllIgnoreRules(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/ignore-rules", (FetchAllIgnoreRules.Handler handler, CancellationToken ct) =>
            handler.HandleAsync(ct));
        return endpoints;
    }
}
