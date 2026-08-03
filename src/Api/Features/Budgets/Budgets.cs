using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Api.Features.Budgets;

public static partial class Budgets
{
    public class Budget
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
    }

    public static IServiceCollection AddBudgetsFeature(this IServiceCollection services) => services
        .AddCreate()
        .AddUpdate()
        .AddDelete()
        .AddFetchForMonth();

    public static IEndpointRouteBuilder MapBudgetsFeature(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("budgets")
            .WithTags("Budgets")
            .AddFluentValidationAutoValidation()
            .RequireAuthorization();
        group.MapCreate().MapUpdate().MapDelete().MapFetchForMonth();
        return endpoints;
    }
}
