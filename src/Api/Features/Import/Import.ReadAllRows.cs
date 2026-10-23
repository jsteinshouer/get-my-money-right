using Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static Api.Features.Transactions.Transactions;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// Station 3: the whole file read through the settled mapping and the <see cref="Pipeline"/> —
    /// the rows an ignore rule catches and the rows already in the ledger marked and counted, and
    /// every row going in printed with how the assignment rules will file it. Where
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
        /// two reasons would read as two problems. <paramref name="IsDuplicate"/> comes after, so it
        /// is only ever set on a row that was readable and not struck.
        ///
        /// A row going in carries what it will be written as — its category, its Need/Want (the
        /// import's Want where no rule judged it) and its tags — and, the way a struck row names its
        /// ignore rule, <paramref name="ClassifiedBy"/> names every rule that decided something about
        /// it. A row that is leaving carries none of it: a null Need/Want, no tags, no rules.
        /// </summary>
        public record class Row(
            DateOnly? Date,
            string? Description,
            decimal? Amount,
            string? Error,
            string? SkippedReason,
            bool IsDuplicate,
            int? CategoryId,
            string? CategoryName,
            NeedWant? NeedWant,
            IReadOnlyList<Tag> Tags,
            IReadOnlyList<string> ClassifiedBy);

        public record class Tag(int Id, string Name);

        /// <param name="ClassifiedCount">Rows going in that a rule gives a category.</param>
        /// <param name="ForReviewCount">Rows going in with no category: the review queue they will join.</param>
        public record class Response(
            List<Row> Rows,
            int WillImportCount,
            int SkippedCount,
            int DuplicateCount,
            int ErrorCount,
            int ClassifiedCount,
            int ForReviewCount);

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
            private readonly Pipeline _pipeline;

            public Handler(BudgetDbContext db, PreviewCache cache, Pipeline pipeline)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
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

                // Names for the ids the rules assigned, fetched once for the whole file.
                var categoryIds = read.Select(r => r.Assigned?.CategoryId).OfType<int>().Distinct().ToList();
                var tagIds = read.SelectMany(r => r.Assigned?.TagIds ?? []).Distinct().ToList();
                var categoryNames = await _db.Categories
                    .Where(c => categoryIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
                var tagNames = await _db.Tags
                    .Where(t => tagIds.Contains(t.Id))
                    .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

                var rows = read
                    .Select(r =>
                    {
                        var goingIn = r.Outcome == Pipeline.Outcome.Import;
                        var categoryId = r.Assigned?.CategoryId;
                        return new Row(
                            r.Date,
                            r.Description,
                            r.Amount,
                            r.Error,
                            r.CaughtBy is null ? null : RuleMatching.Describe(r.CaughtBy),
                            r.Outcome == Pipeline.Outcome.Duplicate,
                            categoryId,
                            categoryId is int id ? categoryNames.GetValueOrDefault(id) : null,
                            goingIn ? r.Assigned?.NeedWant ?? NeedWant.Want : null,
                            (r.Assigned?.TagIds ?? [])
                                .Select(tagId => new Tag(tagId, tagNames.GetValueOrDefault(tagId, string.Empty)))
                                .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
                                .ToList(),
                            (r.Assigned?.DecidedBy ?? []).Select(RuleMatching.Describe).ToList());
                    })
                    .ToList();

                var goingInRows = read.Where(r => r.Outcome == Pipeline.Outcome.Import).ToList();
                return new Response(
                    rows,
                    goingInRows.Count,
                    read.Count(r => r.Outcome == Pipeline.Outcome.Ignored),
                    read.Count(r => r.Outcome == Pipeline.Outcome.Duplicate),
                    read.Count(r => r.Outcome == Pipeline.Outcome.Unreadable),
                    goingInRows.Count(r => r.Assigned?.CategoryId is not null),
                    goingInRows.Count(r => r.Assigned?.CategoryId is null));
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
