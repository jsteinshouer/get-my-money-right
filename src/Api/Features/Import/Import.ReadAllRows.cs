using Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// Station 3: the whole file read through the settled mapping, with the rows an ignore rule
    /// catches marked and counted. Where <see cref="ReadPreview"/> is a five-row rehearsal of the
    /// mapping, this is the performance — what the file will actually put in the transaction list.
    /// Nothing is written to Transactions here; importing arrives with the next ticket.
    /// </summary>
    public static partial class ReadAllRows
    {
        public record class Command(
            string Delimiter,
            bool HasHeaderRow,
            string DateFormat,
            string? DateColumn,
            string? DescriptionColumn,
            string? AmountColumn,
            string? DebitColumn,
            string? CreditColumn);

        /// <summary>
        /// A non-null <paramref name="SkippedReason"/> is the strike, and it silences
        /// <paramref name="Error"/>: the pipeline is map → ignore → dedupe, so a row that both
        /// matches a rule and fails to parse is leaving either way, and two reasons would read as
        /// two problems.
        /// </summary>
        public record class Row(
            DateOnly? Date, string? Description, decimal? Amount, string? Error, string? SkippedReason);

        public record class Response(List<Row> Rows, int WillImportCount, int SkippedCount, int ErrorCount);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Delimiter)
                    .Must(Csv.IsSupportedDelimiter)
                    .WithMessage("That column separator is not one the import can read.");

                RuleFor(x => x.DateFormat)
                    .Must(Csv.IsSupportedDateFormat)
                    .WithMessage("That date format is not one the import can read.");
            }
        }

        public class Handler
        {
            private readonly BudgetDbContext _db;
            private readonly PreviewCache _cache;

            public Handler(BudgetDbContext db, PreviewCache cache)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
                _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            }

            public async Task<Response?> HandleAsync(string token, Command command, CancellationToken cancellationToken)
            {
                var entry = _cache.Find(token);
                if (entry is null)
                {
                    return null;
                }

                // Global rules and this account's, read fresh on every request: a rule written or
                // deleted while the preview is open takes effect the next time station 3 reads.
                var rules = await _db.ImportIgnoreRules
                    .AsNoTracking()
                    .Where(r => r.IsActive && (r.AccountId == null || r.AccountId == entry.AccountId))
                    .OrderBy(r => r.Id)
                    .ToListAsync(cancellationToken);

                var mapping = new RowReader.Mapping(
                    command.Delimiter,
                    command.HasHeaderRow,
                    command.DateFormat,
                    command.DateColumn,
                    command.DescriptionColumn,
                    command.AmountColumn,
                    command.DebitColumn,
                    command.CreditColumn);

                var (columns, records) = RowReader.Split(entry.Text, mapping);
                if (columns.Count == 0)
                {
                    return new Response([], 0, 0, 0);
                }

                var rows = new List<Row>(records.Count);
                var skipped = 0;
                var errored = 0;

                foreach (var record in records)
                {
                    var read = RowReader.ReadRow(record, columns, mapping);
                    var caughtBy = IgnoreMatching.FirstMatch(rules, read.Description);

                    if (caughtBy is not null)
                    {
                        skipped++;
                        rows.Add(new Row(read.Date, read.Description, read.Amount, null, IgnoreMatching.Describe(caughtBy)));
                        continue;
                    }

                    if (read.Error is not null)
                    {
                        errored++;
                    }

                    rows.Add(new Row(read.Date, read.Description, read.Amount, read.Error, null));
                }

                return new Response(rows, rows.Count - skipped - errored, skipped, errored);
            }
        }
    }

    public static IServiceCollection AddReadAllRows(this IServiceCollection services) => services
        .AddScoped<ReadAllRows.Handler>();

    public static IEndpointRouteBuilder MapReadAllRows(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/previews/{token}/rows", async (
            string token, ReadAllRows.Command command, ReadAllRows.Handler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(token, command, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });
        return endpoints;
    }
}
