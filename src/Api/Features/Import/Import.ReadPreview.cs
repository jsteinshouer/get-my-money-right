using FluentValidation;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// Re-reads the sampled rows through the mapping the household member is assembling. This is the
    /// proof the Map Columns step offers: not a validation tick, but the file's own first rows printed
    /// as the app would store them. The same reading is what a confirmed import will insert.
    /// </summary>
    public static partial class ReadPreview
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

        public record class Row(DateOnly? Date, string? Description, decimal? Amount, string? Error);

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

        public record class Response(List<string> Columns, List<List<string>> SampleRows, List<Row> Rows);

        public class Handler
        {
            private readonly PreviewCache _cache;

            public Handler(PreviewCache cache)
            {
                _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            }

            public Response? Handle(string token, Command command)
            {
                var entry = _cache.Find(token);
                if (entry is null)
                {
                    return null;
                }

                // The delimiter and the header flag are the household member's to correct, so the file
                // is re-split every time rather than reusing the upload's guess.
                var delimiter = Csv.IsSupportedDelimiter(command.Delimiter) ? command.Delimiter[0] : ',';
                var records = Csv.Split(entry.Text, delimiter);
                if (records.Count == 0)
                {
                    return new Response([], [], []);
                }

                var columns = command.HasHeaderRow
                    ? Csv.ColumnNames(records[0])
                    : Csv.PositionalColumnNames(records[0].Count);
                var dataRecords = (command.HasHeaderRow ? records.Skip(1) : records)
                    .Take(Csv.SampleRowCount)
                    .Select(r => r.Select(c => c.Trim()).ToList())
                    .ToList();

                return new Response(
                    columns,
                    dataRecords,
                    dataRecords.Select(record => ReadRow(record, columns, command)).ToList());
            }

            private static Row ReadRow(List<string> record, List<string> columns, Command command)
            {
                var problems = new List<string>();

                DateOnly? date = null;
                var rawDate = Cell(record, columns, command.DateColumn);
                if (rawDate is not null)
                {
                    if (Csv.TryParseDate(rawDate, command.DateFormat, out var parsed))
                    {
                        date = parsed;
                    }
                    else
                    {
                        problems.Add($"\"{rawDate}\" is not a date in {command.DateFormat} form.");
                    }
                }

                var description = Cell(record, columns, command.DescriptionColumn);

                decimal? amount = null;
                if (!string.IsNullOrEmpty(command.AmountColumn))
                {
                    var rawAmount = Cell(record, columns, command.AmountColumn);
                    if (rawAmount is null)
                    {
                        problems.Add("This row has no amount.");
                    }
                    else if (Csv.TryParseAmount(rawAmount, out var parsed))
                    {
                        amount = parsed;
                    }
                    else
                    {
                        problems.Add($"\"{rawAmount}\" is not an amount.");
                    }
                }
                else if (!string.IsNullOrEmpty(command.DebitColumn) || !string.IsNullOrEmpty(command.CreditColumn))
                {
                    amount = ReadDebitCredit(record, columns, command, problems);
                }

                return new Row(date, description, amount, problems.Count == 0 ? null : string.Join(" ", problems));
            }

            /// <summary>
            /// Money out is negative and money in is positive, whatever signs the two columns carried —
            /// that normalisation is the whole reason the pair exists as a mapping option.
            /// </summary>
            private static decimal? ReadDebitCredit(
                List<string> record, List<string> columns, Command command, List<string> problems)
            {
                var rawDebit = Cell(record, columns, command.DebitColumn);
                var rawCredit = Cell(record, columns, command.CreditColumn);

                decimal? debit = null;
                if (rawDebit is not null)
                {
                    if (!Csv.TryParseAmount(rawDebit, out var parsed))
                    {
                        problems.Add($"\"{rawDebit}\" is not an amount.");
                        return null;
                    }

                    debit = parsed;
                }

                decimal? credit = null;
                if (rawCredit is not null)
                {
                    if (!Csv.TryParseAmount(rawCredit, out var parsed))
                    {
                        problems.Add($"\"{rawCredit}\" is not an amount.");
                        return null;
                    }

                    credit = parsed;
                }

                // Plenty of banks zero-fill the unused side rather than leaving it blank, so the side
                // that actually carries a figure decides — not merely the side that is non-empty.
                var debitUsed = debit is not null && debit != 0m;
                var creditUsed = credit is not null && credit != 0m;

                if (debitUsed && creditUsed)
                {
                    problems.Add("This row has both a debit and a credit, so its amount is ambiguous.");
                    return null;
                }

                if (debitUsed)
                {
                    return -Math.Abs(debit!.Value);
                }

                if (creditUsed)
                {
                    return Math.Abs(credit!.Value);
                }

                if (debit is null && credit is null)
                {
                    problems.Add("This row has neither a debit nor a credit.");
                    return null;
                }

                // Both sides read as zero, which is what the file says.
                return 0m;
            }

            /// <summary>An unassigned role, an unknown column and a blank cell all read as nothing.</summary>
            private static string? Cell(List<string> record, List<string> columns, string? columnName)
            {
                if (string.IsNullOrEmpty(columnName))
                {
                    return null;
                }

                var index = columns.IndexOf(columnName);
                if (index < 0 || index >= record.Count)
                {
                    return null;
                }

                var value = record[index];
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
    }

    public static IServiceCollection AddReadPreview(this IServiceCollection services) => services
        .AddScoped<ReadPreview.Handler>();

    public static IEndpointRouteBuilder MapReadPreview(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/previews/{token}/reading", (
            string token, ReadPreview.Command command, ReadPreview.Handler handler) =>
        {
            var result = handler.Handle(token, command);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });
        return endpoints;
    }
}
