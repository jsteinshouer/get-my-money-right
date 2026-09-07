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

    public static IServiceCollection AddImportFeature(this IServiceCollection services) => services
        .AddSingleton<PreviewCache>()
        .AddUploadPreview()
        .AddReadPreview()
        .AddSaveMapping()
        .AddFetchMapping();

    public static IEndpointRouteBuilder MapImportFeature(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("import")
            .WithTags("Import")
            .AddFluentValidationAutoValidation()
            .RequireAuthorization();
        group.MapUploadPreview().MapReadPreview().MapSaveMapping().MapFetchMapping();
        return endpoints;
    }
}
