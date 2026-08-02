using static Api.Features.Budgets.Budgets;

namespace Api.Tests.Features.Budgets;

public class CreateValidatorTests
{
    private readonly Create.Validator _validator = new();

    private static Create.Command ValidCommand() => new(1, 2026, 8, 400.00m);

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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.CategoryId));
    }

    [Fact]
    public void Validate_WithMonthBelowOne_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { Month = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.Month));
    }

    [Fact]
    public void Validate_WithMonthAboveTwelve_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { Month = 13 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.Month));
    }

    [Fact]
    public void Validate_WithYearOutOfRange_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { Year = 1999 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.Year));
    }

    [Fact]
    public void Validate_WithZeroAmount_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { Amount = 0m });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.Amount));
    }

    [Fact]
    public void Validate_WithNegativeAmount_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { Amount = -1m });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.Amount));
    }
}
