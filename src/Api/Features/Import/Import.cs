using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// How one account's bank export is read. One per Account: banks each format their export their
    /// own way, and describing it once is the whole point — the household never maps it twice.
    /// Either <see cref="AmountColumn"/> is set, or the <see cref="DebitColumn"/>/<see cref="CreditColumn"/>
    /// pair is, never both.
    /// </summary>
    public class CsvImportMapping
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public required string DateColumn { get; set; }
        public required string DescriptionColumn { get; set; }
        public string? AmountColumn { get; set; }
        public string? DebitColumn { get; set; }
        public string? CreditColumn { get; set; }
        public required string DateFormat { get; set; }
        public required string Delimiter { get; set; }
        public bool HasHeaderRow { get; set; }
        public required string CreatedByUserId { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    /// <summary>How a rule's text is held up against a row's description.</summary>
    public enum IgnoreMatchType
    {
        Contains,
        StartsWith,
        Equals,
    }

    /// <summary>
    /// A row the household has said it never wants: an autopay confirmation, a card payment, the
    /// transfer to savings. This is also the only way an inter-account transfer is described — the
    /// app has no Transfer entity — so the mechanism carries a modelling decision on its back.
    /// <see cref="AccountId"/> is null for a rule that applies to every account.
    /// </summary>
    public class ImportIgnoreRule
    {
        public int Id { get; set; }
        public int? AccountId { get; set; }
        public required string MatchText { get; set; }
        public IgnoreMatchType MatchType { get; set; }
        public bool IsActive { get; set; } = true;
        public required string CreatedByUserId { get; set; }
    }

    public static IServiceCollection AddImportFeature(this IServiceCollection services) => services
        .AddSingleton<PreviewCache>()
        .AddUploadPreview()
        .AddReadPreview()
        .AddReadAllRows()
        .AddSaveMapping()
        .AddFetchMapping()
        .AddCreateIgnoreRule()
        .AddFetchAllIgnoreRules()
        .AddDeleteIgnoreRule();

    public static IEndpointRouteBuilder MapImportFeature(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("import")
            .WithTags("Import")
            .AddFluentValidationAutoValidation()
            .RequireAuthorization();
        group
            .MapUploadPreview()
            .MapReadPreview()
            .MapReadAllRows()
            .MapSaveMapping()
            .MapFetchMapping()
            .MapCreateIgnoreRule()
            .MapFetchAllIgnoreRules()
            .MapDeleteIgnoreRule();
        return endpoints;
    }
}
