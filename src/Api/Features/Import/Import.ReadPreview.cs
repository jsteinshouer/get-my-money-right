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
                    return new Response([], [], []);
                }

                // A rehearsal, not the performance: few enough rows to read at a glance.
                var sampled = records.Take(Csv.SampleRowCount).ToList();

                return new Response(
                    columns,
                    sampled,
                    sampled
                        .Select(record => RowReader.ReadRow(record, columns, mapping))
                        .Select(read => new Row(read.Date, read.Description, read.Amount, read.Error))
                        .ToList());
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
