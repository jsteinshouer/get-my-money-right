using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Api.Features.Categories;

public static partial class Categories
{
    public class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string CreatedByUserId { get; set; }
    }

    public static IServiceCollection AddCategoriesFeature(this IServiceCollection services) => services
        .AddCreate()
        .AddUpdate()
        .AddDelete()
        .AddFetchAll()
        .AddFetchOne();

    public static IEndpointRouteBuilder MapCategoriesFeature(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("categories")
            .WithTags("Categories")
            .AddFluentValidationAutoValidation()
            .RequireAuthorization();
        group.MapCreate().MapUpdate().MapDelete().MapFetchAll().MapFetchOne();
        return endpoints;
    }
}
