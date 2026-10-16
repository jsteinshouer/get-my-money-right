using FluentValidation;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// Station 3: the whole file read through the settled mapping and the <see cref="Pipeline"/> —
    /// the rows an ignore rule catches and the rows already in the ledger marked and counted. Where
    /// <see cref="ReadPreview"/> is a five-row rehearsal of the mapping, this is the performance:
    /// what <see cref="Confirm"/> will write, read the same way. Nothing is written here.
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
        /// A row carries at most one reason for leaving. A non-null <paramref name="SkippedReason"/>
        /// is the strike, and it silences <paramref name="Error"/>: the pipeline is map → ignore →
        /// dedupe, so a row that both matches a rule and fails to parse is leaving either way, and
        /// two reasons would read as two problems. <paramref name="IsDuplicate"/> comes last, so it
        /// is only ever set on a row that was readable and not struck.
        /// </summary>
        public record class Row(
            DateOnly? Date, string? Description, decimal? Amount, string? Error, string? SkippedReason, bool IsDuplicate);

        public record class Response(
            List<Row> Rows, int WillImportCount, int SkippedCount, int DuplicateCount, int ErrorCount);

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
            private readonly PreviewCache _cache;
            private readonly Pipeline _pipeline;

            public Handler(PreviewCache cache, Pipeline pipeline)
            {
                _cache = cache ?? throw new ArgumentNullException(nameof(cache));
                _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            }

            public async Task<Response?> HandleAsync(string token, Command command, CancellationToken cancellationToken)
            {
                var entry = _cache.Find(token);
                if (entry is null)
                {
                    return null;
                }

                var mapping = new RowReader.Mapping(
                    command.Delimiter,
                    command.HasHeaderRow,
                    command.DateFormat,
                    command.DateColumn,
                    command.DescriptionColumn,
                    command.AmountColumn,
                    command.DebitColumn,
                    command.CreditColumn);

                var read = await _pipeline.ReadAsync(entry.AccountId, entry.Text, mapping, cancellationToken);

                var rows = read
                    .Select(r => new Row(
                        r.Date,
                        r.Description,
                        r.Amount,
                        r.Error,
                        r.CaughtBy is null ? null : IgnoreMatching.Describe(r.CaughtBy),
                        r.Outcome == Pipeline.Outcome.Duplicate))
                    .ToList();

                return new Response(
                    rows,
                    read.Count(r => r.Outcome == Pipeline.Outcome.Import),
                    read.Count(r => r.Outcome == Pipeline.Outcome.Ignored),
                    read.Count(r => r.Outcome == Pipeline.Outcome.Duplicate),
                    read.Count(r => r.Outcome == Pipeline.Outcome.Unreadable));
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
