using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Api.Features.Accounts;

public static partial class Accounts
{
    public enum AccountType
    {
        Checking,
        Savings,
        CreditCard,
    }

    public class Account
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public AccountType Type { get; set; }
        public bool IsActive { get; set; } = true;
        public required string CreatedByUserId { get; set; }
    }

    public static IServiceCollection AddAccountsFeature(this IServiceCollection services) => services
        .AddCreate()
        .AddUpdate()
        .AddDeactivate()
        .AddFetchAll()
        .AddFetchOne();

    public static IEndpointRouteBuilder MapAccountsFeature(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("accounts")
            .WithTags("Accounts")
            .AddFluentValidationAutoValidation()
            .RequireAuthorization();
        group.MapCreate().MapUpdate().MapDeactivate().MapFetchAll().MapFetchOne();
        return endpoints;
    }
}
