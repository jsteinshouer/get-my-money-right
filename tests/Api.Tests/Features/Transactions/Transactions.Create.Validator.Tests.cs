using static Api.Features.Transactions.Transactions;

namespace Api.Tests.Features.Transactions;

public class CreateValidatorTests
{
    private readonly Create.Validator _validator = new();

    private static Create.Command ValidCommand() =>
        new(1, 1, new DateOnly(2026, 1, 15), -10.00m, "Groceries", NeedWant.Need);

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithoutNeedWant_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { NeedWant = null });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.NeedWant));
    }

    [Fact]
    public void Validate_WithOutOfRangeNeedWant_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { NeedWant = (NeedWant)999 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.NeedWant));
    }

    [Fact]
    public void Validate_WithZeroAccountId_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { AccountId = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.AccountId));
    }

    [Fact]
    public void Validate_WithZeroCategoryId_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { CategoryId = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.CategoryId));
    }

    [Fact]
    public void Validate_WithDefaultDate_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { Date = default });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.Date));
    }

    [Fact]
    public void Validate_WithEmptyDescription_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { Description = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.Description));
    }

    [Fact]
    public void Validate_WithDescriptionLongerThan200Characters_HasError()
    {
        var result = _validator.Validate(ValidCommand() with { Description = new string('a', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.Description));
    }
}
