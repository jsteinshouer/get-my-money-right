using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Api.Features.Transactions;

public static partial class Transactions
{
    public enum NeedWant
    {
        Need,
        Want,
    }

    public class Transaction
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int CategoryId { get; set; }
        public DateOnly Date { get; set; }
        public decimal Amount { get; set; }
        public required string Description { get; set; }
        public NeedWant NeedWant { get; set; }
        public required string CreatedByUserId { get; set; }
    }

    public static IServiceCollection AddTransactionsFeature(this IServiceCollection services) => services
        .AddCreate()
        .AddUpdate()
        .AddDelete()
        .AddFetchAll();

    public static IEndpointRouteBuilder MapTransactionsFeature(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("transactions")
            .WithTags("Transactions")
            .AddFluentValidationAutoValidation()
            .RequireAuthorization();
        group.MapCreate().MapUpdate().MapDelete().MapFetchAll();
        return endpoints;
    }
}
