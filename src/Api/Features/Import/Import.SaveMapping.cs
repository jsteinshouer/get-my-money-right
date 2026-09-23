using System.Security.Claims;
using Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Import;

public static partial class Import
{
    public static partial class SaveMapping
    {
        public record class Command(
            string DateColumn,
            string DescriptionColumn,
            string? AmountColumn,
            string? DebitColumn,
            string? CreditColumn,
            string DateFormat,
            string Delimiter,
            bool HasHeaderRow);

        public record class Response(
            int Id,
            int AccountId,
            string DateColumn,
            string DescriptionColumn,
            string? AmountColumn,
            string? DebitColumn,
            string? CreditColumn,
            string DateFormat,
            string Delimiter,
            bool HasHeaderRow,
            DateTimeOffset UpdatedAt);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.DateColumn).NotEmpty().MaximumLength(200);
                RuleFor(x => x.DescriptionColumn).NotEmpty().MaximumLength(200);
                RuleFor(x => x.AmountColumn).MaximumLength(200);
                RuleFor(x => x.DebitColumn).MaximumLength(200);
                RuleFor(x => x.CreditColumn).MaximumLength(200);

                RuleFor(x => x.DateFormat)
                    .Must(Csv.IsSupportedDateFormat)
                    .WithMessage("That date format is not one the import can read.");

                RuleFor(x => x.Delimiter)
                    .Must(Csv.IsSupportedDelimiter)
                    .WithMessage("That column separator is not one the import can read.");

                // An amount arrives one of two ways and never both: a single signed column, or a
                // debit/credit pair. Anything else leaves the sign of a transaction undecided.
                RuleFor(x => x)
                    .Must(HasExactlyOneAmountConvention)
                    .WithName(nameof(Command.AmountColumn))
                    .WithMessage("Choose either one signed amount column, or a debit column and a credit column.");
            }

            private static bool HasExactlyOneAmountConvention(Command command)
            {
                var hasAmount = !string.IsNullOrWhiteSpace(command.AmountColumn);
                var hasDebit = !string.IsNullOrWhiteSpace(command.DebitColumn);
                var hasCredit = !string.IsNullOrWhiteSpace(command.CreditColumn);
                return hasAmount ? !hasDebit && !hasCredit : hasDebit && hasCredit;
            }
        }

        public class Handler
        {
            private readonly BudgetDbContext _db;

            public Handler(BudgetDbContext db)
            {
                _db = db ?? throw new ArgumentNullException(nameof(db));
            }

            public async Task<Response?> HandleAsync(
                int accountId, Command command, string createdByUserId, CancellationToken cancellationToken)
            {
                var accountExists = await _db.Accounts.AnyAsync(a => a.Id == accountId, cancellationToken);
                if (!accountExists)
                {
                    return null;
                }

                var mapping = await _db.CsvImportMappings
                    .FirstOrDefaultAsync(m => m.AccountId == accountId, cancellationToken);

                if (mapping is null)
                {
                    mapping = new CsvImportMapping
                    {
                        AccountId = accountId,
                        DateColumn = command.DateColumn,
                        DescriptionColumn = command.DescriptionColumn,
                        DateFormat = command.DateFormat,
                        Delimiter = command.Delimiter,
                        CreatedByUserId = createdByUserId,
                    };
                    _db.CsvImportMappings.Add(mapping);
                }

                mapping.DateColumn = command.DateColumn.Trim();
                mapping.DescriptionColumn = command.DescriptionColumn.Trim();
                mapping.AmountColumn = Blank(command.AmountColumn);
                mapping.DebitColumn = Blank(command.DebitColumn);
                mapping.CreditColumn = Blank(command.CreditColumn);
                mapping.DateFormat = command.DateFormat;
                mapping.Delimiter = command.Delimiter;
                mapping.HasHeaderRow = command.HasHeaderRow;
                mapping.UpdatedAt = DateTimeOffset.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);

                return new Response(
                    mapping.Id,
                    mapping.AccountId,
                    mapping.DateColumn,
                    mapping.DescriptionColumn,
                    mapping.AmountColumn,
                    mapping.DebitColumn,
                    mapping.CreditColumn,
                    mapping.DateFormat,
                    mapping.Delimiter,
                    mapping.HasHeaderRow,
                    mapping.UpdatedAt);
            }

            private static string? Blank(string? value) =>
                string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    public static IServiceCollection AddSaveMapping(this IServiceCollection services) => services
        .AddScoped<SaveMapping.Handler>();

    public static IEndpointRouteBuilder MapSaveMapping(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/mappings/{accountId:int}", async (
            int accountId,
            SaveMapping.Command command,
            ClaimsPrincipal user,
            SaveMapping.Handler handler,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await handler.HandleAsync(accountId, command, userId, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });
        return endpoints;
    }
}
