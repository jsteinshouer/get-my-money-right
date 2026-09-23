using System.Text;
using Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Import;

public static partial class Import
{
    public static partial class UploadPreview
    {
        /// <summary>How much of a bank export the household could plausibly be holding.</summary>
        public const long MaxFileBytes = 5 * 1024 * 1024;

        public record class Response(
            string Token,
            int AccountId,
            string FileName,
            List<string> Columns,
            List<List<string>> SampleRows,
            string Delimiter,
            bool HasHeaderRow,
            string DateFormat,
            SavedMapping? Saved,
            List<string> MissingColumns);

        /// <summary>
        /// The mapping this account was last imported with, so the common case — the fifth import from
        /// the same bank — is a confirmation rather than a task.
        /// </summary>
        public record class SavedMapping(
            string DateColumn,
            string DescriptionColumn,
            string? AmountColumn,
            string? DebitColumn,
            string? CreditColumn,
            string DateFormat,
            string Delimiter,
            bool HasHeaderRow,
            DateTimeOffset UpdatedAt);

        public class Handler
        {
            private readonly BudgetDbContext _db;
            private readonly PreviewCache _cache;

            public Handler(BudgetDbContext db, PreviewCache cache)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
                _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            }

            public async Task<Results<Ok<Response>, NotFound, ValidationProblem>> HandleAsync(
                int accountId, IFormFile? file, CancellationToken cancellationToken)
            {
                if (file is null || file.Length == 0)
                {
                    return Problem("file", "That file is empty. Export the statement again and upload the new file.");
                }

                if (file.Length > MaxFileBytes)
                {
                    return Problem("file", $"That file is larger than {MaxFileBytes / (1024 * 1024)} MB. Export a narrower date range.");
                }

                var accountExists = await _db.Accounts.AnyAsync(a => a.Id == accountId, cancellationToken);
                if (!accountExists)
                {
                    return TypedResults.NotFound();
                }

                string text;
                await using (var stream = file.OpenReadStream())
                {
                    // detectEncodingFromByteOrderMarks strips the UTF-8 BOM real exports often carry,
                    // which would otherwise arrive glued to the first column's name.
                    using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    text = await reader.ReadToEndAsync(cancellationToken);
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    return Problem("file", "That file is empty. Export the statement again and upload the new file.");
                }

                var saved = await _db.CsvImportMappings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.AccountId == accountId, cancellationToken);

                // A remembered mapping decides how its own file is read. Re-detecting here would let a
                // guess disagree with what the household already told us, and report every remembered
                // column as missing from a file that has not changed at all.
                var delimiter = Csv.IsSupportedDelimiter(saved?.Delimiter)
                    ? saved!.Delimiter[0]
                    : Csv.DetectDelimiter(text);
                var records = Csv.Split(text, delimiter);
                if (records.Count == 0)
                {
                    return Problem("file", "That file is empty. Export the statement again and upload the new file.");
                }

                if (records[0].Count < 2)
                {
                    return Problem("file", "That file has only one column, so there is nothing to map. Check you exported it as CSV.");
                }

                var hasHeaderRow = saved?.HasHeaderRow ?? Csv.DetectHasHeaderRow(records);
                var columns = hasHeaderRow
                    ? Csv.ColumnNames(records[0])
                    : Csv.PositionalColumnNames(records[0].Count);
                var dataRecords = hasHeaderRow ? records.Skip(1).ToList() : records;

                if (dataRecords.Count == 0)
                {
                    return Problem("file", "That file has column names but no transactions under them.");
                }

                var dateFormat = Csv.IsSupportedDateFormat(saved?.DateFormat)
                    ? saved!.DateFormat
                    : Csv.DetectDateFormat(dataRecords);

                var token = _cache.Add(accountId, file.FileName, text);

                return TypedResults.Ok(new Response(
                    token,
                    accountId,
                    file.FileName,
                    columns,
                    dataRecords.Take(Csv.SampleRowCount).Select(r => r.Select(c => c.Trim()).ToList()).ToList(),
                    delimiter.ToString(),
                    hasHeaderRow,
                    dateFormat,
                    saved is null ? null : new SavedMapping(
                        saved.DateColumn,
                        saved.DescriptionColumn,
                        saved.AmountColumn,
                        saved.DebitColumn,
                        saved.CreditColumn,
                        saved.DateFormat,
                        saved.Delimiter,
                        saved.HasHeaderRow,
                        saved.UpdatedAt),
                    MissingColumns(saved, columns)));
            }

            /// <summary>
            /// Columns the saved mapping names that this file no longer has — what happens when a bank
            /// changes its export. Naming them is the difference between a fixable screen and a
            /// mysteriously blank one.
            /// </summary>
            private static List<string> MissingColumns(CsvImportMapping? saved, List<string> columns)
            {
                if (saved is null)
                {
                    return [];
                }

                var named = new[]
                {
                    saved.DateColumn, saved.DescriptionColumn, saved.AmountColumn, saved.DebitColumn, saved.CreditColumn,
                };

                return named
                    .Where(c => !string.IsNullOrEmpty(c) && !columns.Contains(c, StringComparer.Ordinal))
                    .Select(c => c!)
                    .ToList();
            }

            private static ValidationProblem Problem(string field, string message) =>
                TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
        }
    }

    public static IServiceCollection AddUploadPreview(this IServiceCollection services) => services
        .AddScoped<UploadPreview.Handler>();

    public static IEndpointRouteBuilder MapUploadPreview(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/preview", async (
                [FromForm] int accountId,
                IFormFile? file,
                UploadPreview.Handler handler,
                CancellationToken ct) => await handler.HandleAsync(accountId, file, ct))
            // The app has no antiforgery middleware (LAN-only, cookie auth, same-origin SPA), and a
            // form-accepting endpoint refuses to run without one unless it opts out explicitly.
            .DisableAntiforgery();
        return endpoints;
    }
}
