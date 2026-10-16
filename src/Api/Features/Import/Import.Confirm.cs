using System.Security.Claims;
using Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Transactions.Transactions;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// The import itself: the held file read through the account's saved mapping and the
    /// <see cref="Pipeline"/>, and every row that survives it written as a transaction. A row
    /// arrives uncategorized and as a Want (ADR-0001): uncategorized is the review queue, and a
    /// guessed Want overstates discretionary spending where a guessed Need would quietly hide it.
    /// </summary>
    public static partial class Confirm
    {
        public record class Response(int Imported, int SkippedIgnored, int SkippedDuplicate);

        public class Handler
        {
            /// <summary>
            /// Both household members may import the same export at once. The duplicate check is
            /// only true until the next insert, so confirms run one at a time.
            /// </summary>
            private static readonly SemaphoreSlim Gate = new(1, 1);

            private readonly BudgetDbContext _db;
            private readonly PreviewCache _cache;
            private readonly Pipeline _pipeline;

            public Handler(BudgetDbContext db, PreviewCache cache, Pipeline pipeline)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
                _cache = cache ?? throw new ArgumentNullException(nameof(cache));
                _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            }

            public async Task<Results<Ok<Response>, NotFound, ValidationProblem>> HandleAsync(
                string token, string createdByUserId, CancellationToken cancellationToken)
            {
                await Gate.WaitAsync(cancellationToken);
                try
                {
                    // Read inside the gate, so a second confirm of the same file finds it already
                    // consumed rather than importing it twice.
                    var entry = _cache.Find(token);
                    if (entry is null)
                    {
                        return TypedResults.NotFound();
                    }

                    // The saved mapping, not whatever the client last previewed with: the mapping is
                    // remembered so that the next export from this bank imports the same way.
                    var saved = await _db.CsvImportMappings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.AccountId == entry.AccountId, cancellationToken);
                    if (saved is null)
                    {
                        return Problem("Save the column mapping for this account before importing.");
                    }

                    var mapping = new RowReader.Mapping(
                        saved.Delimiter,
                        saved.HasHeaderRow,
                        saved.DateFormat,
                        saved.DateColumn,
                        saved.DescriptionColumn,
                        saved.AmountColumn,
                        saved.DebitColumn,
                        saved.CreditColumn);

                    var rows = await _pipeline.ReadAsync(entry.AccountId, entry.Text, mapping, cancellationToken);

                    // Refused rather than imported around: counts that leave rows unaccounted for
                    // are not counts the household can check against the file.
                    var unreadable = rows.Count(r => r.Outcome == Pipeline.Outcome.Unreadable);
                    if (unreadable > 0)
                    {
                        return Problem(unreadable == 1
                            ? "1 row could not be read. Fix the mapping, or write a rule that skips it, then import."
                            : $"{unreadable} rows could not be read. Fix the mapping, or write a rule that skips them, then import.");
                    }

                    var imported = rows
                        .Where(r => r.Outcome == Pipeline.Outcome.Import)
                        .Select(r => new Transaction
                        {
                            AccountId = entry.AccountId,
                            CategoryId = null,
                            Date = r.Date!.Value,
                            Amount = r.Amount!.Value,
                            Description = r.Description!,
                            NeedWant = NeedWant.Want,
                            CreatedByUserId = createdByUserId,
                        })
                        .ToList();

                    _db.Transactions.AddRange(imported);
                    await _db.SaveChangesAsync(cancellationToken);

                    // The file has done its job; confirming it again would only re-run the dedupe.
                    _cache.Remove(token);

                    return TypedResults.Ok(new Response(
                        imported.Count,
                        rows.Count(r => r.Outcome == Pipeline.Outcome.Ignored),
                        rows.Count(r => r.Outcome == Pipeline.Outcome.Duplicate)));
                }
                finally
                {
                    Gate.Release();
                }
            }

            private static ValidationProblem Problem(string message) =>
                TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = [message] });
        }
    }

    public static IServiceCollection AddConfirm(this IServiceCollection services) => services
        .AddScoped<Confirm.Handler>();

    public static IEndpointRouteBuilder MapConfirm(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/previews/{token}/confirm", async (
            string token, ClaimsPrincipal user, Confirm.Handler handler, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            return await handler.HandleAsync(token, userId, ct);
        });
        return endpoints;
    }
}
