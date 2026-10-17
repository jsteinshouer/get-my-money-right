using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using static Api.Features.Transactions.Transactions;

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

    /// <summary>
    /// How a rule's text is held up against a row's description. Named for the ignore rule, which
    /// came first; the assignment rule speaks the same vocabulary so both kinds match alike.
    /// </summary>
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
    public class ImportIgnoreRule : IImportRule
    {
        public int Id { get; set; }
        public int? AccountId { get; set; }
        public required string MatchText { get; set; }
        public IgnoreMatchType MatchType { get; set; }
        public bool IsActive { get; set; } = true;
        public required string CreatedByUserId { get; set; }
    }

    /// <summary>
    /// A row the household has said what it *is*: "rows whose description contains KROGER are
    /// Groceries · Need". Sets any mix of <see cref="CategoryId"/>, <see cref="NeedWant"/> and tags —
    /// never none, since a rule that sets nothing cannot be read back as a sentence. Scoped like an
    /// ignore rule: <see cref="AccountId"/> is null for a rule that applies to every account.
    /// </summary>
    public class ImportAssignmentRule : IImportRule
    {
        public int Id { get; set; }
        public int? AccountId { get; set; }
        public required string MatchText { get; set; }
        public IgnoreMatchType MatchType { get; set; }
        public int? CategoryId { get; set; }
        public NeedWant? NeedWant { get; set; }
        public bool IsActive { get; set; } = true;
        public required string CreatedByUserId { get; set; }
    }

    /// <summary>
    /// The many-to-many join between an assignment rule and the tags it applies. Owned by the
    /// pairing: deleting the tag or the rule takes the row with it, as a tag on a transaction does.
    /// </summary>
    public class ImportAssignmentRuleTag
    {
        public int RuleId { get; set; }
        public int TagId { get; set; }
    }

    public static IServiceCollection AddImportFeature(this IServiceCollection services) => services
        .AddSingleton<PreviewCache>()
        .AddScoped<Pipeline>()
        .AddUploadPreview()
        .AddReadPreview()
        .AddReadAllRows()
        .AddConfirm()
        .AddSaveMapping()
        .AddFetchMapping()
        .AddCreateIgnoreRule()
        .AddFetchAllIgnoreRules()
        .AddDeleteIgnoreRule()
        .AddCreateAssignmentRule()
        .AddFetchAllAssignmentRules()
        .AddDeleteAssignmentRule();

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
            .MapConfirm()
            .MapSaveMapping()
            .MapFetchMapping()
            .MapCreateIgnoreRule()
            .MapFetchAllIgnoreRules()
            .MapDeleteIgnoreRule()
            .MapCreateAssignmentRule()
            .MapFetchAllAssignmentRules()
            .MapDeleteAssignmentRule();
        return endpoints;
    }
}
