using static Api.Features.Categories.Categories;

namespace Api.Tests.Features.Categories;

public class CreateValidatorTests
{
    private readonly Create.Validator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new Create.Command("Groceries"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyName_HasError()
    {
        var result = _validator.Validate(new Create.Command(string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.Name));
    }

    [Fact]
    public void Validate_WithNameLongerThan100Characters_HasError()
    {
        var result = _validator.Validate(new Create.Command(new string('a', 101)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Create.Command.Name));
    }
}
