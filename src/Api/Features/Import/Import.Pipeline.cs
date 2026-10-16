using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// The import pipeline up to the point of writing: map → ignore → dedupe. Station 3's preview
    /// and the confirm both read a file through this one class, because a preview that decides a
    /// row's fate differently from the import is a preview nobody can trust.
    /// </summary>
    public class Pipeline
    {
        public enum Outcome
        {
            Import,
            Ignored,
            Duplicate,
            Unreadable,
        }

        /// <summary>
        /// One row's fate. <paramref name="CaughtBy"/> is set only for <see cref="Outcome.Ignored"/>,
        /// <paramref name="Error"/> only for <see cref="Outcome.Unreadable"/>: the pipeline is
        /// terminal at each step, so a row carries exactly one reason for leaving.
        /// </summary>
        public record class Row(
            DateOnly? Date,
            string? Description,
            decimal? Amount,
            Outcome Outcome,
            ImportIgnoreRule? CaughtBy,
            string? Error);

        private readonly BudgetDbContext _db;

        public Pipeline(BudgetDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<List<Row>> ReadAsync(
            int accountId, string text, RowReader.Mapping mapping, CancellationToken cancellationToken)
        {
            var (columns, records) = RowReader.Split(text, mapping);
            if (columns.Count == 0)
            {
                return [];
            }

            // Global rules and this account's, read fresh on every request: a rule written or
            // deleted while the preview is open takes effect the next time the file is read.
            var rules = await _db.ImportIgnoreRules
                .AsNoTracking()
                .Where(r => r.IsActive && (r.AccountId == null || r.AccountId == accountId))
                .OrderBy(r => r.Id)
                .ToListAsync(cancellationToken);

            var rows = new List<Row>(records.Count);
            foreach (var record in records)
            {
                var read = RowReader.ReadRow(record, columns, mapping);
                var caughtBy = IgnoreMatching.FirstMatch(rules, read.Description);

                if (caughtBy is not null)
                {
                    rows.Add(new Row(read.Date, read.Description, read.Amount, Outcome.Ignored, caughtBy, null));
                }
                else if (read.Error is not null || read.Date is null || read.Description is null || read.Amount is null)
                {
                    rows.Add(new Row(
                        read.Date,
                        read.Description,
                        read.Amount,
                        Outcome.Unreadable,
                        null,
                        read.Error ?? "This row is missing its date, description or amount."));
                }
                else
                {
                    rows.Add(new Row(read.Date, read.Description, read.Amount, Outcome.Import, null, null));
                }
            }

            return await MarkDuplicatesAsync(accountId, rows, cancellationToken);
        }

        /// <summary>
        /// A row duplicates a stored transaction on the same account with the same date, amount and
        /// description — the description whitespace-normalised and compared without regard to case,
        /// since a payee typed by hand and the same payee in an export differ in nothing else.
        /// Stored transactions are consumed one-for-one: two identical coffees on one day in the
        /// file are two coffees, and only as many of them are duplicates as the ledger already holds.
        /// That is what makes re-importing an overlapping export safe without losing a real repeat.
        /// </summary>
        private async Task<List<Row>> MarkDuplicatesAsync(
            int accountId, List<Row> rows, CancellationToken cancellationToken)
        {
            var candidates = rows.Where(r => r.Outcome == Outcome.Import).ToList();
            if (candidates.Count == 0)
            {
                return rows;
            }

            var from = candidates.Min(r => r.Date!.Value);
            var to = candidates.Max(r => r.Date!.Value);
            var stored = await _db.Transactions
                .AsNoTracking()
                .Where(t => t.AccountId == accountId && t.Date >= from && t.Date <= to)
                .Select(t => new { t.Date, t.Amount, t.Description })
                .ToListAsync(cancellationToken);

            var unclaimed = stored
                .GroupBy(t => KeyOf(t.Date, t.Amount, t.Description))
                .ToDictionary(g => g.Key, g => g.Count());

            return rows
                .Select(row =>
                {
                    if (row.Outcome != Outcome.Import)
                    {
                        return row;
                    }

                    var key = KeyOf(row.Date!.Value, row.Amount!.Value, row.Description);
                    if (unclaimed.TryGetValue(key, out var remaining) && remaining > 0)
                    {
                        unclaimed[key] = remaining - 1;
                        return row with { Outcome = Outcome.Duplicate };
                    }

                    return row;
                })
                .ToList();
        }

        private static (DateOnly, decimal, string) KeyOf(DateOnly date, decimal amount, string? description) =>
            (date, amount, (Csv.NormaliseDescription(description) ?? string.Empty).ToUpperInvariant());
    }
}
