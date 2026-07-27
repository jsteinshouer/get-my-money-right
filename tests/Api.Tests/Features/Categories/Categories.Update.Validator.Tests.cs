using static Api.Features.Categories.Categories;

namespace Api.Tests.Features.Categories;

public class UpdateValidatorTests
{
    private readonly Update.Validator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new Update.Command("Dining Out"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyName_HasError()
    {
        var result = _validator.Validate(new Update.Command(string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Update.Command.Name));
    }

    [Fact]
    public void Validate_WithNameLongerThan100Characters_HasError()
    {
        var result = _validator.Validate(new Update.Command(new string('a', 101)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Update.Command.Name));
    }
}
