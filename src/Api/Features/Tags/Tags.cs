using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Api.Features.Tags;

public static partial class Tags
{
    public class Tag
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string CreatedByUserId { get; set; }
    }

    /// <summary>
    /// The many-to-many join between a transaction and a tag. Rows are owned by the pairing itself,
    /// so deleting either side takes its assignments with it rather than blocking the delete.
    /// </summary>
    public class TransactionTag
    {
        public int TransactionId { get; set; }
        public int TagId { get; set; }
    }

    public static IServiceCollection AddTagsFeature(this IServiceCollection services) => services
        .AddCreate()
        .AddDelete()
        .AddFetchAll()
        .AddAssignToTransaction()
        .AddRemoveFromTransaction()
        .AddAssignToManyTransactions();

    public static IEndpointRouteBuilder MapTagsFeature(this IEndpointRouteBuilder endpoints)
    {
        var tags = endpoints.MapGroup("tags")
            .WithTags("Tags")
            .AddFluentValidationAutoValidation()
            .RequireAuthorization();
        tags.MapCreate().MapDelete().MapFetchAll().MapAssignToManyTransactions();

        // Assignments hang off the transaction they belong to, not off the tag.
        var transactionTags = endpoints.MapGroup("transactions/{transactionId:int}/tags")
            .WithTags("Tags")
            .AddFluentValidationAutoValidation()
            .RequireAuthorization();
        transactionTags.MapAssignToTransaction().MapRemoveFromTransaction();

        return endpoints;
    }
}
