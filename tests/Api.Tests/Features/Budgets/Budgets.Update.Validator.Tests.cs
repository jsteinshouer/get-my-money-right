using static Api.Features.Budgets.Budgets;

namespace Api.Tests.Features.Budgets;

public class UpdateValidatorTests
{
    private readonly Update.Validator _validator = new();

    private static Update.Command ValidCommand() => new(1, 2026, 8, 400.00m);

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithZeroCategoryId_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { CategoryId = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Update.Command.CategoryId));
    }

    [Fact]
    public void Validate_WithMonthOutOfRange_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { Month = 13 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Update.Command.Month));
    }

    [Fact]
    public void Validate_WithZeroAmount_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { Amount = 0m });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Update.Command.Amount));
    }
}
