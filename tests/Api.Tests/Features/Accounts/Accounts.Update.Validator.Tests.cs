using static Api.Features.Accounts.Accounts;

namespace Api.Tests.Features.Accounts;

public class UpdateValidatorTests
{
    private readonly Update.Validator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new Update.Command("Savings", AccountType.Savings));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyName_HasError()
    {
        var result = _validator.Validate(new Update.Command(string.Empty, AccountType.Savings));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Update.Command.Name));
    }

    [Fact]
    public void Validate_WithOutOfRangeType_HasError()
    {
        var result = _validator.Validate(new Update.Command("Savings", (AccountType)999));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Update.Command.Type));
    }
}
