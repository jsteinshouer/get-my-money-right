using static Api.Features.Identity.Identity;

namespace Api.Tests.Features.Identity;

public class LoginValidatorTests
{
    private readonly Login.Validator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new Login.Command("user1@household.local", "SomePassword123!"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "SomePassword123!")]
    [InlineData("not-an-email", "SomePassword123!")]
    public void Validate_WithInvalidEmail_HasError(string email, string password)
    {
        var result = _validator.Validate(new Login.Command(email, password));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Login.Command.Email));
    }

    [Fact]
    public void Validate_WithEmptyPassword_HasError()
    {
        var result = _validator.Validate(new Login.Command("user1@household.local", string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Login.Command.Password));
    }
}
